using MyProject.ProjectCtrl;
using NLog;
using richpod.synway;
using shpa3api;
using Synway.Models;
using System.Text;
using urIprService.Models;

namespace MyProject.Synway {
    public class SynwayCti {
        private string _className => GetType().Name;
        private Logger _nlog = LogManager.GetLogger("SynwayCti");
        private Logger _iprLog = LogManager.GetLogger($"IprEvent");        
        private EVENT_SET_INFO_CALLBACKA _eventSetCallbackA = new EVENT_SET_INFO_CALLBACKA();

        private HwInfoDataModel _hwInfo = new HwInfoDataModel();
        public HwInfoDataModel HwInfo => _hwInfo;

        private HwBoardDataModel _board = new HwBoardDataModel();

        // 每一個 IPR H/W Channel 的屬性(狀態...)
        public List<IPRChannelInfoDataModel> IPRChInfo = new List<IPRChannelInfoDataModel>();
        public int IPR_ANA_REC_OFFSET { private set; get; } = 0;

        public string Error { private set; get; } = "";
        public int StationMapped { private set; get; } = 0;

        public SynwayCti() {
            _nlog.Info($"");
            _nlog.Info($"{new string('=', 100)}");
            _nlog.Info($"[{_className}] init ...");
            _nlog.Info($"[{_className}] Set CTI EVENT_SET_INFO_CALLBACKA=EVENT_CALLBACKA ...");

            _eventSetCallbackA.dwWorkMode = (uint)EventMode.EVENT_CALLBACKA;
            _eventSetCallbackA.lpHandlerParam = EventCallBackProc;
        }

        public int EventCallBackProc(ref SSM_EVENT ssmEvent) {
            var ssmModel = new SsmEventDataModel(ssmEvent);
            DispatchEvent(ssmModel, out int ch);
            var s = $"SsmEvent(0x{ssmModel.wEventCode:X4})=[{ssmModel.DecodeEventName(),-35}], ch={ch,-3}, {ssmModel.GetEventParamStr()}";
            _iprLog.Trace(s);
            return 1;
        }

        // 用來取得 ipr channel 的對應 channel id
        // 用 StationID 來分類(StationID是自己定義) 
        //      StationId >= 1000 : 來自 IPR_ANA 的 Event，也是各個分機的 Event
        //      StationId == 0    : 來自 IPR_REC 的 Event(錄音)
        public int GetIprChID(SsmEventDataModel ssmModel) {
            int ch = -1;
            var stationID = (int)ssmModel.stationID;
            var nRef = ssmModel.nReference;
            if (stationID == 0) { // IPRChannel 後面的錄音迴路(IPR_REC)                
                ch = nRef;
            }
            else { // >1000 的 IPR_ANA                 
                ch = GetIPRAnaChID(stationID);
            }
            return ch;
        }

        //TODO: 要速度優化
        public int GetIPRAnaChID(int stationID) {
            int iprChID = -1;
            foreach (var iprCh in IPRChInfo) {
                if (iprCh.ChType == ENUM_IprChType.IPR_ANA && iprCh.StationID == stationID) {
                    iprChID = iprCh.ChID;
                    break;
                }
            }
            return iprChID;
        }

        // 實際上，iprAnaChID 就是 Global.IPRChInfo[] 的 ChID，也是 HWChID
        public int GetIprAnaChID_ByUrsChID(int ursChID) {
            int iprAnaChID = -1;
            foreach (var ipr in IPRChInfo) {
                if (ipr.MapToUrsChID == ursChID) {
                    iprAnaChID = ipr.ChID;
                    break;
                }
            }
            return iprAnaChID;
        }

        private int DispatchEvent(SsmEventDataModel ssmModel, out int ch) {
            ch = -1;
            if (ssmModel == null)
                return -10;
            try {
                ch = GetIprChID(ssmModel);
                var stationID = (int)ssmModel.stationID;
                var nRef = ssmModel.nReference;
                //-------------------------------------------------------------------------------------
                if ((ch < 0) || (ch >= GVar.ServerMaxChannel)) {
                    _iprLog.Error($"DispatchEvent Error: ch{ch} is a invalid range(0 ~ GlobalVar.ServerMaxChannel), staID={stationID}, nRef={nRef}");
                    return -1;
                }
                
                try {
                    GVar.IprManager.PushData(ch, ssmModel);                    
                }
                catch (Exception ex) {
                    _iprLog.Error($"DispatchEvent IprWorker[{ch}].PushData() exception:{ex.Message}(staID={stationID}, nRef={nRef})");
                    return -9;
                }
                return 1;
                
            }
            catch (Exception ex) {
                _iprLog.Error($"DispatchEvent Exception: {ex.Message} ");
                return -9;
            }
        }

        public bool InitShCTI(int cpuCores) {
            // 1. 先關閉 CTI
            _nlog.Info("try to init CTI ...");
            _nlog.Info("\t> close CTI ...");
            lib_synway.CloseCti();

            // 2. Open CTI
            _nlog.Info("\t> open CTI ...");
            var configFile = GetCtiConfigFileName();
            var indexFile = GetCtiIndexFileName();

            if (lib_synway.SsmStartCtiEx(configFile, indexFile, ref _eventSetCallbackA, out string errMsg) != 0) {
                _nlog.Error($"\t> CTI open failed: {errMsg}");
                return false;
            }
            else {
                _nlog.Info($"\t> CTI open ok");
            }

            _nlog.Info("\t> set InterEventType = 1 ...");
            lib_synway.SetInterEventType(1); // 此處不須判斷失敗，後面會判斷處理.

            // 3. 取硬體/板卡資訊 => Global.HwInfo
            _nlog.Info("\t> try to get CTI H/W info ...");
            GetCtiHwInfo();

            // 4. 開始判斷，有問題要 return false 表示cti初始化失敗，系統無法啟動
            _nlog.Info("\t> check H/W info ...");
            if (!CheckHardWare(out errMsg)) {
                return false;
            }

            // 5. 初始化 _iprChInfo ( 0 ~ MaxChannel)
            InitIprChannelInfo();

            // 6. 舊版是 ip/mac 定義在 urIpr 程式這邊，因此必須多一個分機與ip mapping 的程序
            //    新版直接讀 DB 時，就有IP了(以後可能要加 MAC)，所以此步驟省略

            // 7. 取 GVar.URS?.UrsChInfo 每一迴路 IP，Add 到 IPR Station 列表中進行監控錄音
            SetStationMapping();
            Task.Delay(2000).Wait();

            // 掃描 SynIPR Record Slaver，有時候沒安裝好 Slaver 會找不到
            if (!ScanIprSlaver(out errMsg)) {
                return false;
            }

            // 啟動背景服務: SynIPR Record Slaver
            StartRecSlaver(cpuCores);
            Task.Delay(2000).Wait();

            // 針對 IPR_REC 的迴路 => IPRActiveSession
            ActiveIPRSession();

            _nlog.Info($"{new string('=', 50)} CTI init ok {new string('=', 50)}");
            return true;
        }


        public void GetCtiHwInfo() {
            Task.Delay(3000).Wait();
            _hwInfo.MaxCfgBoard = lib_synway.GetMaxCfgBoard();
            _hwInfo.MaxUsableBoard = lib_synway.GetMaxUsableBoard();

            var ver = new SSM_VERSION();
            lib_synway.GetDllVersion(ref ver);
            _hwInfo.DllVersion = ver;

            _hwInfo.MaxChannel = lib_synway.GetMaxCh();
            _hwInfo.InterEventType = lib_synway.GetInterEventType();
            _nlog.Info($"\t\t> GetCtiHwInfo: dllVer={lib_synway.GetDllVerStr(ver)}, " +
                                        $@"maxCfgBroad={_hwInfo.MaxCfgBoard}, " +
                                        $@"maxUsableBoard={_hwInfo.MaxUsableBoard}, " +
                                        $@"maxChannel={_hwInfo.MaxChannel}, " +
                                        $@"interEventType={lib_synway.GetInterEventType()}");

            IPR_ANA_REC_OFFSET = _hwInfo.MaxChannel / 2;

            for (var i = 0; i < _hwInfo.MaxCfgBoard; i++) {
                Task.Delay(100).Wait();

                _board.AccreditId = lib_synway.GetBoardAccreditId(i);
                _board.AccreditIdEx = lib_synway.GetAccreditIdEx(i);
                _board.ModelID = lib_synway.GetBoardModel(i);
                _board.SerialNo = lib_synway.GetPciSerialNo(i);
                _board.IprSlaverCount = 0;
                
                // 錄音的 board, 目前先假設只有一個 Analyzer Board
                if (_board.ModelID == (int)ENUM_IprBoardType.IPRecorder) {
                    if (_hwInfo.IPRRecBoardID == -1) {
                        _hwInfo.IPRRecBoardID = i;
                    }
                    _hwInfo.IprRecBoardCount++;
                    var count = lib_synway.GetIPRRecSlaverCount(i);
                    if (count >= 0)
                        _board.IprSlaverCount = count;
                    _nlog.Info($"\t\t> board_REC[{i}]: IPRRecSlaverCount={count}");
                }
                //分析的 board, 目前先假設只有一個 Analyzer Board
                else if (_board.ModelID == (int)ENUM_IprBoardType.IPAnalyzer) {
                    if (_hwInfo.IPRAnaBoardID == -1) {
                        _hwInfo.IPRAnaBoardID = i;
                    }
                    _board.MonitorType = lib_synway.GetIPRMonitorType(i);
                    _hwInfo.IprAnaBoardCount++;
                    _nlog.Info($"\t\t> board_ANA[{i}]: MonitorType={_board.MonitorType}");
                }
                _hwInfo.TotalSlaverCount = _hwInfo.TotalSlaverCount + _board.IprSlaverCount;
                _hwInfo.BoardList.Add(_board);
            }
            return;
        }

        private bool CheckHardWare(out string errMsg) {
            errMsg = "";
            // 針對 IPR 錄音
            _nlog.Info("\t\t> check IPR_REC board ...");
            if (_hwInfo.IprRecBoardCount <= 0) {
                errMsg = "No IPR_REC board found.";
                _nlog.Error($"\t\t> {errMsg}");
                return false;
            }
            _nlog.Info($"\t\t==> IPR_REC board={_hwInfo.IprRecBoardCount}");

            _nlog.Info("\t\t> check IPR_ANA board ...");
            if (_hwInfo.IprAnaBoardCount <= 0) {
                errMsg = "No IPR_ANA board found.";
                _nlog.Error($"\t\t> {errMsg}");
                return false;
            }
            _nlog.Info($"\t\t==> IPR_ANA board={_hwInfo.IprAnaBoardCount}");

            // 常常會出錯，先不檢查
            _nlog.Info("\t\t> check IPR_REC slaver ...");
            if (_hwInfo.TotalSlaverCount <= 0) {
                errMsg = "No IPR_REC slaver found.";
                _nlog.Error($"\t\t> {errMsg}");
                return false;
            }
            _nlog.Info($"\t\t==> IPR_REC slaver={_hwInfo.TotalSlaverCount}");

            _nlog.Info("\t\t> check InterEventType=1 ...");
            if (_hwInfo.InterEventType != 1) {
                errMsg = $"InterEventType={_hwInfo.InterEventType}, must = 1";
                _nlog.Error($"\t\t> {errMsg}");
                return false;
            }
            _nlog.Info("\t\t> check MonitorType=1 ...");
            for (var i = 0; i < _hwInfo.BoardList.Count; i++) {
                var board = _hwInfo.BoardList[i];
                if (board.ModelID == (int)ENUM_IprBoardType.IPAnalyzer && board.MonitorType != 1) {
                    errMsg = $"IPR_ANA board monitorType != 1(id={i}), modelId={board.ModelID}, sn={board.SerialNo}";
                    _nlog.Error($"\t\t> {errMsg}");
                    return false;
                }

            }
            return true;
        }

        public string GetCtiConfigFileName() {
            //var fileName = "";
            var ursRoot = GVar.URS!.UrsRoot!;
            //switch (Global.AppType) {
            //    case ENUM_AppType.Analog:
            //        fileName = Path.Combine(ursRoot, "conf", "board_a.ini");
            //        break;
            //    case ENUM_AppType.ASMDR:
            //        fileName = Path.Combine(ursRoot, "conf", "board_a.ini");
            //        break;
            //    case ENUM_AppType.Digital:
            //        fileName = Path.Combine(ursRoot, "conf", "board_d.ini");
            //        break;
            //    case ENUM_AppType.Trunk:
            //        fileName = Path.Combine(ursRoot, "conf", "board_t.ini");
            //        break;
            //    case ENUM_AppType.RawIP:    //自己的 sip 錄音，不需要
            //        fileName = "";
            //        break;
            //    case ENUM_AppType.SYNIPR:   //三匯IP錄音
            //        fileName = Path.Combine(ursRoot, "conf", "board_ipr.ini");
            //        break;
            //    default:
            //        break;
            //}
            //return fileName;
            return Path.Combine(ursRoot, "conf", "board_ipr.ini");
        }

        public string GetCtiIndexFileName() {
            var ursRoot = GVar.URS!.UrsRoot!;
            return Path.Combine(ursRoot, "conf", "ShIndex.ini"); ;
        }

        // 配置 IPRChInfo ( 0 ~ MaxChannel)
        public void InitIprChannelInfo() {
            // 初始化每一個 channel info
            _nlog.Info($"IPRChInfo init...");
            for (var i = 0; i < _hwInfo.MaxChannel; i++) {
                var iprChInfo = new IPRChannelInfoDataModel();
                iprChInfo.ChID = i;
                var rawType = SsmApi.SsmGetChType(i);
                // 判斷是否為 IPR_REC(25) 或 IPR_ANA(26)
                if (Enum.IsDefined(typeof(ENUM_IprChType), rawType)) {
                    iprChInfo.ChType = (ENUM_IprChType)rawType;

                    if (iprChInfo.ChType == ENUM_IprChType.IPR_ANA)
                        _hwInfo.IprAnaChannelCount++;
                    else if (iprChInfo.ChType == ENUM_IprChType.IPR_REC)
                        _hwInfo.IprRecChannelCount++;
                    IPRChInfo.Add(iprChInfo);
                    _nlog.Info($"\t> IPRChInfo[{i}] detect ChType => {iprChInfo.ChType}");
                }
                else {
                    _nlog.Error($"\t> IPRChInfo[{i}] detect ChType Undefined({rawType}), not IPR(25) or IPA(26)");
                }                
            }
            _nlog.Info($"\t> ipr={_hwInfo.IprRecChannelCount}, ipa={_hwInfo.IprAnaChannelCount}, total {IPRChInfo.Count} IPRChInfo channels initialized");            
            return;
        }

        public bool SetStationMapping() {            

            _nlog.Info($"開始進行分機 IP 設定新增 Station ...(channelAuth={GVar.LicenseMaxChannel})");
            var ursChCount = GVar.URS?.UrsChInfo?.Count ?? 0;

            for (var i = 0; i < ursChCount; i++) {
                var ursCh = GVar.URS?.UrsChInfo?[i];
                if (ursCh == null || ursCh.ChActive != (int)ENUM_ChActiveType.Activate) { // 迴路沒有啟用
                    _nlog.Info($"\t> ursChInfo[{i}]: ursChNo={ursCh?.ChID}, ext={ursCh?.ExtNo} => channel is deactivated");
                    continue;
                }
                if (StationMapped >= GVar.LicenseMaxChannel) {
                    break;
                }

                var stationID = ursCh.ChID + 1000;
                // 尋找 _iprChInfo 中的 ChType=IPR_ANA && StationID=-1 並賦值
                // IPR_ANA 的 stationID = 1000~，藉此與 IPR_REC 區分
                var iprChID = GetAndSetFreeIprAnaChannelID(stationID, ursCh.ExtNo, ursCh.ChID);
                if (iprChID < 0) { // 找不到=-1
                    _nlog.Error($"\t> ursChInfo[{i}]: ursChNo={ursCh.ChID}, ext={ursCh.ExtNo} => 在 _iprChInfo 中找不到 free 的 ipr_ana channel");
                    continue;
                }

                var stationPort = 0; // 固定為 0: 表示在由 ursCh.IPAddr 指定的IP 地址上，任意端口都可监控到；
                _nlog.Info($"\t> ursChInfo[{i}]: ursChNo={ursCh.ChID}, ext={ursCh.ExtNo}, ip={ursCh.IPAddr}:{stationPort}, staID={stationID} is starting mapping ...");

                #region 現在先支援 IP 就好，之後再加 MAC
                //if (Global.Config.StationMap == ENUM_StationMapping.Mac) {
                //    var ret = lib_synway.IPRAddStationToMap(_hwInfo.IPRAnaBoardID, Global.IPRChInfo[iprChID].StationID, ursCh.Mac, -1);
                //    if (ret == 0) {
                //        Global.StationMapped++;
                //        _nlog.Info($"\t>ursChInfo[{i}]: ursChID={ursCh.ChID}, ext={ursCh.ExtNo} mapping to iprChNo[{iprChID}] @mac({ursCh.Mac}), stationID={stationID} ok.");
                //    }
                //    else
                //        _nlog.Info($"\t>ursChInfo[{i}]: ursChID={ursCh.ChID}, ext={ursCh.ExtNo} mapping to iprChNo[{iprChID}] @mac({ursCh.Mac}), stationID={stationID} failed.");

                //}
                //else if (Global.Config.StationMap == ENUM_StationMapping.IpAddr) {
                #endregion
                
                var ret = lib_synway.IPRAddStationToMap(_hwInfo.IPRAnaBoardID, stationID, ursCh.IPAddr, stationPort);
                if (ret == 0) {
                    StationMapped++;
                    _nlog.Info($"\t> ursChInfo[{i}]: ursChNo={ursCh.ChID}, ext={ursCh.ExtNo} mapping ok.");
                }
                else
                    _nlog.Error($"\t> ursChInfo[{i}]: ursChNo={ursCh.ChID}, ext={ursCh.ExtNo} mapping failed.");
                //}
            }
            _nlog.Info($"共 {StationMapped} 分機已對應(StationMap={ENUM_StationMapping.IpAddr.ToString()})");
            return true;
        }

        private int GetAndSetFreeIprAnaChannelID(int stationID, string ext, int ursChID) {
            // 1. 尋找符合條件的第一個物件及其索引
            var chInfo = IPRChInfo
                    .Select((info, index) => new { info, index }) // 同時取得物件與索引
                    .FirstOrDefault(x => x.info.ChType == ENUM_IprChType.IPR_ANA && x.info.StationID < 0); // StationID 初始狀態 = -1  

            if (chInfo == null) 
                return -1;

            // 2. 進行賦值 (副作用操作)
            var ch = chInfo.info;
            ch.StationID = stationID;
            ch.MapToExt = ext;
            ch.MapToUrsChID = ursChID;

            return chInfo.index;
        }


        public bool ScanIprSlaver(out string errMsg) {
            // TODO: Add your control notification handler code here            
            errMsg = "";
            IPR_SLAVERADDR iprSlaverAddr = new IPR_SLAVERADDR();
            int actualSlaverCount = 0;
            _nlog.Info($"ScanIprSlaver(IPR boardID={_hwInfo.IPRRecBoardID}) ...");
            var slaverCount = lib_synway.GetIPRRecSlaverCount(_hwInfo.IPRRecBoardID); // SsmIPRGetRecSlaverCount 中帶入的參數是 Recorder Board ID = 0xfd
            if (slaverCount > 0) {
                var ret = lib_synway.IPRGetRecSlaverList(_hwInfo.IPRRecBoardID, slaverCount, ref actualSlaverCount, ref iprSlaverAddr);
                if (ret >= 0) {
                    _hwInfo.IPRSlaverAddr = iprSlaverAddr;
                    _nlog.Info($"\t> ScanIprSlaver ok.(SlaverAddr={lib_synway.GetIPAddress(_hwInfo.IPRSlaverAddr.ipAddr)})");
                    return true;
                }
                else {
                    errMsg = $"IPRGetRecSlaverList error: {lib_synway.GetLastErrMsg()}";
                    _nlog.Error($"\t> ScanIprSlaver failed, {errMsg}");
                }
            }
            else {
                errMsg = $"no IPR slaver found.";
                _nlog.Error($"\t> ScanIprSlaver failed: {errMsg}");
            }
            //
            return false;
        }

        public bool StartRecSlaver(int cpuCores) {
            int totalResource = _hwInfo.IprRecChannelCount;
            int thraedPairs = cpuCores;
            _nlog.Info($"StartRecSlaver..., thraedPairs={thraedPairs}");
            _nlog.Info($"\t> StartRecSlaver...(recBoardID={_hwInfo.IPRRecBoardID}, " +
                                            $"recSlaverAddr={lib_synway.GetIPAddress(_hwInfo.IPRSlaverAddr.ipAddr)}), " +
                                            $"recSlaverID={_hwInfo.IPRSlaverAddr.nRecSlaverID}");
            var ret = lib_synway.IPRStartRecSlaver(_hwInfo.IPRRecBoardID, _hwInfo.IPRSlaverAddr.nRecSlaverID, ref totalResource, ref thraedPairs);
            if (ret < 0) {                
                _nlog.Error($"\t> StartRecSlaver failed.");
                return false;
            }
            else {
                _nlog.Info($"\t> StartRecSlaver ok(totalResource={totalResource}, thraedPairs={thraedPairs})");
            }
            return true;
        }

        public bool ActiveIPRSession() {
            int okCount = 0;
            int errorCount = 0;
            _nlog.Info($"try ActiveIPRSession...(recSlaverID={_hwInfo.IPRSlaverAddr.nRecSlaverID})...");
            for (var i = 0; i < IPRChInfo.Count; i++) {
                var iprCh = IPRChInfo[i];
                if (iprCh.ChType != ENUM_IprChType.IPR_REC)
                    continue;
                
                var priRcvPort = 0;
                var secRcvPort = 0;
                StringBuilder priAddr = new StringBuilder("127.0.0.1");
                StringBuilder secAddr = new StringBuilder("127.0.0.1");
                // priCodec & secCodec 隨便帶，因為 IPR 會自動偵測(文件有寫: IPRecorder supports self-adaptive)
                _nlog.Info($"\t> try to IPRActiveSession..., iprChID={i})");
                
                /* 啟用 IPR 的通道
                 *   1. nChId 通道ID
                     2. nRecSlaverId 录音Slaver ID
                     3. dwSessionId 与通道nChId 绑定的Session 的SessionId
                     4. szPriAddr primary 端RTP 的IP 地址
                     5. nPriPort primary 端RTP 的端口
                     6. pnPriRcvPort 用来接收primary 端RTP 的端口
                     7. nPriCodec
                        primary 端的编解码类型，支持的负载值如下表所示：
                        编解码类型 负载值
                        -Law   0
                        A-Law   8
                        G723    4
                        G729    18
                        GSM     3
                        G722    9
                     8. szSecAddr secondary 端RTP 的IP 地址
                     9. nSecPort secondary 端RTP 的端口
                    10. pnSecRcvPort 用来接收secondary 端RTP 的端口
                    11. nSecCodec secondary 端的编解码类型
                    注意事项：
                        1. 本函数只支持SynIPRecorder；
                        2. 该函数返回成功只是表示它已经往Slaver 发送ActiveSession 的命令，至于该命令在Slaver 端是否成功? 处理需要通过事件E_IPR_ACTIVE_SESSION_CB 上携带的dwParam 的值来确定；
                        3. 参数dwSessionId 与IPAnalyzer 通道产生的SessionId 不需要相同，dwSessionId 是用于区分IPRecorder 通道的录音Session；
                        4. 参数szPriAddr 与szSecAddr 未使用，使用“127.0.0.1”即可；
                        5. 参数nPriPort 与nSecPort 未使用，填入有效端口范围内（1~65535）的任意端口值均可；
                        6. 参数nPriCodec 与nSecCodec 需填入IPRecorder 支持的负载格式，但是由于IPRecorder 支持自适应负载格式，所以nPriCodec 和nSecCodec 不一定要与实际通话使用的负载格式相同。
                 */
                var ret = lib_synway.IPRActiveSession(i, _hwInfo.IPRSlaverAddr.nRecSlaverID, (uint)i, priAddr, 8000, ref priRcvPort, 18, secAddr, 8000, ref secRcvPort, 18);
                if (ret >= 0) {
                    IPRChInfo[i].PriRcvPort = priRcvPort;
                    IPRChInfo[i].SecRcvPort = secRcvPort;
                    _nlog.Info($"\t\t >>> IPRActiveSession ok, iprChID={i}, mapExt={iprCh.MapToExt}, stationID={iprCh.StationID}, mapUrsChID={iprCh.MapToUrsChID}, priRcvPort={priRcvPort}, secRcvPort={secRcvPort})");
                    okCount++;
                }
                else {
                    errorCount++;
                    _nlog.Error($"\t\t >>> IPRActiveSession(iprChID={i})  failed: {lib_synway.GetLastErrMsg()}");
                }                
            }
            if (errorCount <= 0)
                _nlog.Info($"\t> ActiveIPRSession ok({okCount}) and no errors");
            else
                _nlog.Error($"\t> ActiveIPRSession ok({okCount}) but {errorCount} errors");
            return (errorCount == 0);
        }

        public bool DeactiveIPRSession() {
            for (var i = 0; i < IPRChInfo.Count; i++) {
                var iprCh = IPRChInfo[i];
                if (iprCh.ChType == ENUM_IprChType.IPR_REC) {
                    lib_synway.IPRDeActiveSession(i);
                }
            }
            return true;
        }

        public void CloseSynway() {
            try {
                DeactiveIPRSession();
                lib_synway.CloseCti();
            }
            catch (Exception ex) {
                _nlog.Error($"close synway cti exception: {ex.Message}");
            }            
        }

    }
}
