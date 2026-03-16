using Microsoft.Extensions.Primitives;
using MyProject.Database;
using MyProject.lib;
using MyProject.Models;
using MyProject.ProjectCtrl;
using Newtonsoft.Json;
using NLog;
using System.Net;
using System.Text;

namespace MyProject.Urs {

    // 描述有關 URS 相關的基本環境，這些基本設定大部分來自 DB 的定義，
    // CheckDB:                 是否連線DB成功? Table Name 是否有 tblLogger 、tblRecData ?
    // GetLoggerConfig:         透過 appsettings 的 loggerID 到 DB 中取得 LoggerSeq、LoggerName ...等資訊，放入 LoggerDataModel
    // GetLoggerSystemConfig:   透過 LoggerSeq 中取得 Logger 相關的系統設定，LoggerSystemConfigDataModel
    // InitParameters:          準備系統參數，UrsRoot、UrsDataPath、RecID、RecFileSn
    //                          注意: RecID、RecFileSn 好像沒用了 ...? 要再確認
    // CheckRecordingRootPath:  強制建立 UrsRoot、UrsDataPath 路徑資料夾 
    // GetBoardChannelSetting:  取得 IP 錄音的起始迴路編號: UrsTotalChannel、UrsStartChNo
    // CreateRecordingFolder:   建立當天的錄音檔路徑 => UrsDataPath\yyyymm\yyyymmdd
    // ReadDBChannelConfig:     取得 DB 中每一 Channel 的設定放入 UrsChInfo => List<ChannelConfigDataModel>

    public class UrsProcess {        
        private RecDb? _db;
        private Logger _nlog;

        private object objFileSn = new object();

        // 因為要跟彰銀一致，所以還是設定為三匯, 否則應該是 RawIP 才對
        public ENUM_ChannelType ChannelType = ENUM_ChannelType.SYNIPR; 
        public LoggerDataModel? LoggerConfig { get; private set; }
        public LoggerSystemConfigDataModel? LoggerSystemConfig { get; private set; }
        public string? UrsRoot { get; private set; } = string.Empty; // 錄音的 root 目錄
        public string? UrsDataPath { get; private set; } = string.Empty; // 錄音的 root\data 目錄
        public ulong RecID { get; private set; } = 0; //這一版改為 10億為 base，一號到底，沒有每日 reset
        public ulong RecFileSn { get; private set; } = 0;
        public int UrsTotalChannel { get; private set; } = 0; // 總共有幾個 channel
        public int UrsStartChNo { get; private set; } = 0; // IP錄音開始的 channel 編號(因為類比；數位、IP會混合)

        // 每一個 Channel 的設定 => 考慮到混插，5筆資料 = 迴路編號(ChID)是 5 ~ 9， 因為前面 1 ~ 4 可能是 URA 或 URD        
        public List<ChannelConfigDataModel>? UrsChInfo { get; private set; } = null;

        // 標註 URS 目前的狀態，Init、Running、Failed、NotSupport ...等，讓其他程式可以透過 GlobalVar 來判斷 URS 的狀態
        public ENUM_UrsStatus Status { private set; get; } = ENUM_UrsStatus.None;

        // 錯誤訊息，當 Status = Failed 時，可以帶入錯誤訊息，讓其他程式可以透過 GlobalVar 來取得 URS 失敗的原因
        public string Error { private set; get; } = "";
        
        //=========================================================================================================
        public UrsProcess(ENUM_ChannelType chType, string logName, bool support) {
            ChannelType = chType;
            _nlog = LogManager.GetLogger(logName);
            _nlog.Info($"[URS] ursProcess init ...");
            Status = ENUM_UrsStatus.Init;
            if (!support) {
                _nlog.Info($"[URS] appsettings.URS.Support = false, ursProcess stopped normally.");
                Status = ENUM_UrsStatus.NotSupport;
            }
        }

        public ENUM_UrsStatus Init() {
            if (Status == ENUM_UrsStatus.NotSupport) {
                // 雖然沒有啟用 URS，但這不是錯誤，直接回傳 true 就好，讓程式繼續執行
                return Status; 
            }

            _db = new RecDb();
            if (!CheckDB()) {
                Status = ENUM_UrsStatus.Failed;
                return Status;
            }

            if (!GetLoggerConfig()) {
                Status = ENUM_UrsStatus.Failed;
                return Status;
            }

            if (!GetLoggerSystemConfig(LoggerConfig!.LoggerSeq)) {
                Status = ENUM_UrsStatus.Failed;
                return Status;
            }

            if (!InitParameters()) {
                Status = ENUM_UrsStatus.Failed;
                return Status;
            }

            if (!CheckRecordingRootPath()) {
                Status = ENUM_UrsStatus.Failed;
                return Status;
            }

            if (!GetBoardChannelSetting((char)(int)ENUM_BoardType.SynIpr)) {
                Status = ENUM_UrsStatus.Failed;
                return Status;
            }

            if (!CreateRecordingFolder(0)) {
                Status = ENUM_UrsStatus.Failed;
                return Status;
            }

            if (!ReadDBChannelConfig()) {
                Status = ENUM_UrsStatus.Failed;
                return Status;
            }

            Status = ENUM_UrsStatus.Ok;
            return Status;
        }

        // 檢查 config 裡面的 DB 連線字串、資料庫結構、錄音主機...等設定
        private bool CheckDB() {            
            _nlog.Info($"[URS] 正在檢查資料庫...");            
            var result = _db!.GetTableSchema().GetAwaiter().GetResult();
            if (!result.Success) {
                _nlog.Error($"[URS] \t 資料庫無法連線!");
                _nlog.Error($"[URS] \t {result.UserMessage} {result.TraceMessage}");
                return false;
            }
            if (result.Data == null) {
                _nlog.Error($"[URS] \t 無法取得資料庫資訊!");
                return false;
            }
            if (!result.Data.Contains("tblLogger") || !result.Data.Contains("tblRecData")) {
                _nlog.Error($"[URS] \t DB 不是指定的錄音資料庫!");
                return false;
            }
            _nlog.Info($"[URS] 資料庫連線 OK.");
            return true;
        }

        // 取得資料庫 tblLogger 的第一筆設定
        private bool GetLoggerConfig() {
            _nlog.Info($"[URS] Get LoggerConfig  ...");
            var ret = false;
            var result = _db!.GetLogger(GVar.Config!.URS.LoggerID).GetAwaiter().GetResult();
            if (result.Success && result.Data != null) {
                LoggerConfig = new();
                LoggerConfig!.LoggerSeq = result.Data.LoggerSeq;
                LoggerConfig.LoggerID = result.Data.LoggerID;
                LoggerConfig.LoggerName = result.Data.LoggerName;
                LoggerConfig.UrsFolder = result.Data.UrsFolder;
                LoggerConfig.BoardNum = result.Data.BoardNum;
                LoggerConfig.VoiceCodec = result.Data.VoiceCodec;
                LoggerConfig.MinRecTime = result.Data.MinRecTime;
                LoggerConfig.MaxRecTime = result.Data.MaxRecTime;
                LoggerConfig.MaxSilenceTime = result.Data.MaxSilenceTime;
                LoggerConfig.MaxBusyToneCycle = result.Data.MaxBusyToneCycle;
                _nlog.Info($"[URS] \t GetLoggerConfig 成功:\r\n{JsonConvert.SerializeObject(LoggerConfig, Formatting.Indented)}");
                ret = true;
            }
            else {
                _nlog.Error($"[URS] \t GetLoggerConfig 失敗: {result.UserMessage} {result.TraceMessage}");
                _nlog.Error($"[URS] \t 請檢查 LoggerID 的設定...");
            }
            return ret;
        }

        private bool GetLoggerSystemConfig(long loggerSeq) {
            _nlog.Info($"[URS] Get LoggerSystemConfig  ...");
            var ret = false;
            var result = _db!.GetLoggerSystemConfig(loggerSeq).GetAwaiter().GetResult();
            if (result.Success && result.Data != null) {
                LoggerSystemConfig = result.Data;
                ret = true;
                _nlog.Info($"[URS] \t GetLoggerSystemConfig 成功:\r\n{JsonConvert.SerializeObject(LoggerSystemConfig, Formatting.Indented)}");
            }
            else {
                _nlog.Error($"[URS] \t GetLoggerSystemConfig 失敗: {result.UserMessage} {result.TraceMessage}");
                _nlog.Error($"[URS] \t 資料庫無法取得錄音主機設定(tblSystemConfig), loggerSeq={loggerSeq}");
            }
            return ret;
        }
               

        private ulong SetRecID(ENUM_ChannelType chType) {
            ulong baseID = 10000000000;
            int intChType = (int)chType;
            var result = _db!.GetMaxRecID((int)chType).GetAwaiter().GetResult();
            if (result.Success) {
                if (result.Data == null) { // 資料庫沒有資料，where 條件不符合
                    RecID = (ulong)intChType * baseID;
                }
                else {
                    if (result.Data < baseID) { // 以前的舊資料
                        RecID = ((ulong)intChType * baseID) + result.Data.Value;
                    }
                    else {
                        RecID = result.Data.Value + 100; // 表示有重新開始, 要跳 100 號
                    }
                }                
                _nlog.Info($"[URS] \t chType={chType}, get last RecID={result.Data}, SetRecID={RecID}");
            }
            else { // 找不到，result.Data = null
                RecID = (ulong)intChType * baseID;
                _nlog.Info($"[URS] \t chType={chType}, get last RecID Error: {result.UserMessage} {result.TraceMessage}");
                _nlog.Info($"[URS] \t SetRecID={RecID}");
            }            
            return RecID; 
        }

        private ulong SetRecFileSN(ENUM_ChannelType chType) {
            // 若無當天資料, result.Data = 0
            var result = _db!.GetRecFileCount(DateTime.Now.Date, (int)chType).GetAwaiter().GetResult();
            if (result.Success) {
                RecFileSn = result.Data + 100;
                _nlog.Info($"[URS] \t chType={chType}, get last RecFileSN={result.Data}, SetRecFileSN={RecFileSn}");
            }
            else {
                RecFileSn = 1;
                _nlog.Info($"[URS] \t chType={chType}, get last RecFileSN Error: {result.UserMessage} {result.TraceMessage}");
                _nlog.Info($"[URS] \t SetRecFileSN={RecFileSn}");
            }
            return RecFileSn; // start from 1
        }

        // 設定 Global Var 
        //public bool InitGlobalParameters() {
        private bool InitParameters() {
            _nlog.Info($"[URS] 正在準備系統參數...");
            if (string.IsNullOrEmpty(LoggerConfig!.UrsFolder)) {
                _nlog.Info($"[URS] \t LoggerConfig.UrsFolder null or empty, 請檢查資料庫設定");
                return false;
            }

            UrsRoot = LoggerConfig.UrsFolder;           

            UrsDataPath = Path.Combine(UrsRoot, "data");

            var recID = SetRecID(ChannelType);            
            var recFileSN = SetRecFileSN(ChannelType);
            _nlog.Info($@"Set URS Parameters:
                            LoggerSeq = {LoggerConfig.LoggerSeq},  
                            LoggerID = {LoggerConfig.LoggerID},  
                            LoggerName = {LoggerConfig.LoggerName},  
                            UrsRoot = {UrsRoot},  
                            UrsDataPath = {UrsDataPath},                              
                            RecID = {RecID},
                            RecFileSn = {RecFileSn}");
            _nlog.Info($"[URS] 系統參數準備完成: recID={recID}, recFileSN={recFileSN}");
            return true;            
        }

        // 檢查 \Urs\Data 路徑並開啟
        public bool CheckRecordingRootPath() {
            _nlog.Info($"[URS] try to Check Recording Root Path: {UrsRoot} ...");
            var err = lib_misc.ForceCreateFolder(UrsRoot!);
            if (!string.IsNullOrEmpty(err)) {
                _nlog.Error($"[URS] URS Root路徑無法開啟: {UrsRoot}");
                _nlog.Error($"[URS] 錯誤: {err}");
                return false;
            }
            _nlog.Info($"[URS] 錄音根目錄: UrsRoot 檢查OK: {UrsRoot}");

            _nlog.Info($"[URS] try to Check Recording Data Path: {UrsDataPath} ...");            
            err = lib_misc.ForceCreateFolder(UrsDataPath!);
            if (!string.IsNullOrEmpty(err)) {
                _nlog.Error($"[URS] URS Data 路徑無法開啟: {UrsDataPath}");
                _nlog.Error($"[URS] 錯誤: {err}");
                return false;
            }
            _nlog.Info($"[URS] 錄音資料夾: UrsData 檢查OK: {UrsDataPath}");
            return true;
        }
                
        /// <summary>
        /// 檢查 \Urs\Data\yyyymm\yyyymmdd 路徑並開啟，
        /// </summary>
        /// <param name="day"></param>
        ///  1: 明天，餘此類堆
        ///  0: 當天
        /// -1: 昨天，餘此類堆
        /// <returns></returns>
        public bool CreateRecordingFolder(int day) {            
            var theDate = DateTime.Now.AddDays(day);
            var yyyymm = theDate.ToString("yyyyMM");
            var yyyymmdd = theDate.ToString("yyyyMMdd");
            var path = Path.Combine(UrsDataPath!, yyyymm, yyyymmdd);

            _nlog.Info($"[URS] try to CreateRecordingFolder...(path={path})");
            var err = lib_misc.ForceCreateFolder(path);
            if (err != "") {
                _nlog.Error($"[URS] URS路徑無法開啟: {path}");
                _nlog.Error($"[URS] 錯誤: {err}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 取 DB 中 tblHwBoard 的設定，
        /// </summary>
        /// <param name="boardType">類比、數位、IP ...等</param>
        /// 可參考 ENUM_BoardType
        /// <returns></returns>
        public bool GetBoardChannelSetting(char boardType) {
            _nlog.Info($"[URS] try to GetBoardChannelSetting...(boardType={boardType})");
            var startChNo = GetLogicStartChNo(LoggerConfig!.LoggerSeq, boardType, out int totalCh);
            UrsTotalChannel = totalCh;
            UrsStartChNo = startChNo;
            _nlog.Info($"[URS] 讀取DB.tblHwBoard設定(boardType={boardType}): startChNo={UrsStartChNo}, totalUrsCh={UrsTotalChannel}");
            if (startChNo <= 0) {
                _nlog.Error($"[URS] 無法讀取資料庫中的卡版迴路設定.");
                return false;
            }
            return true;
        }

        public int GetLogicStartChNo(long loggerSeq, char boardType, out int totalCh) {
            var startNo = -1;
            totalCh = 0;

            var result = _db!.GetHwBoard(loggerSeq, boardType).GetAwaiter().GetResult();
            if (result.Data != null) {
                if (result.Data.Count >= 1) { // 因為有排序，只抓第一筆即可
                    startNo = result.Data[0].LogicBeginChNo;
                }
                foreach (var board in result.Data) {
                    totalCh = totalCh + board.HWChannelNum;
                }
            }
            return startNo;
        }
                
        public bool ReadDBChannelConfig() {
            _nlog.Info($"[URS] 讀取資料庫中的迴路設定...(List<ChannelConfigDataModel>)");
            var result = _db!.GetChannelConfigList(ENUM_ChannelType.SYNIPR).GetAwaiter().GetResult();
            if (result.Success) {
                if (result.Data == null) {
                    _nlog.Info($"[URS] \t>資料庫迴路設定沒有資料.");
                    return false;
                }
                else {
                    UrsChInfo = result.Data;
                    _nlog.Info($"[URS] \t>資料庫迴路設定, 共 {UrsChInfo.Count} 迴路設定已讀取:");
                    foreach (var item in UrsChInfo) {
                        _nlog.Info($"[URS] \t\t==> {JsonConvert.SerializeObject(item)}");
                    }
                }
            }
            else {
                _nlog.Error($"[URS] \t>無法讀取資料庫中的迴路設定: {result.UserMessage} {result.TraceMessage}");
                return false;
            }
            return true;
        }
     
        public string GetUrsChAgentID(string extNo) {
            var agentID = "";
            if (UrsChInfo != null) {
                foreach (var chInfo in UrsChInfo) {
                    if (chInfo.ExtNo == extNo) {
                        agentID = chInfo.AgentID;
                        break;
                    }
                }
            }            
            return agentID;
        }

        public string GetUrsChChName(string extNo) {
            var chName = "";
            if (UrsChInfo != null) {
                foreach (var chInfo in UrsChInfo) {
                    if (chInfo.ExtNo == extNo) {
                        chName = chInfo.ChName;
                        break;
                    }
                }
            }            
            return chName;
        }
        
        public ChannelConfigDataModel? GetChannelConfig(string extNo) {
            ChannelConfigDataModel? chConfig = null;
            if (UrsChInfo != null) {
                foreach (var chInfo in UrsChInfo) {
                    if (chInfo.ExtNo == extNo) {
                        chConfig = chInfo;
                        break;
                    }
                }
            }
            return chConfig;
        }

        public List<string> GetExtenList(ENUM_UrsChType chType) {
            if (UrsChInfo == null || UrsChInfo.Count == 0) {
                return new List<string>();
            }
            return UrsChInfo
                .Where(x => x.ChType == (int)chType)    // 1. 條件 (Where)
                .OrderBy(x => x.ChID)                   // 2. 排序 (OrderBy)
                .Select(x => x.ExtNo)                   // 3. 取出欄位 (Select)
                .ToList();
        }

        private (bool, string) PrepareDatFileName(DateTime recStartTime) {            
            var yyyymm = recStartTime.ToString("yyyyMM");
            var yyyymmdd = recStartTime.ToString("yyyyMMdd");
            var path = Path.Combine(UrsDataPath!, yyyymm, yyyymmdd);
            var err = lib_misc.ForceCreateFolder(path); // 順便檢查、開啟Path
            if (!string.IsNullOrEmpty(err)) {
                return (false, err);
            }
            return (true, Path.Combine(path, $"{yyyymmdd}.dat"));
        }

        //public string GetRecFullFileName(ulong recID, DateTime recStartTime, int ursChID, out string errMsg, bool checkUnique = false) {
        //    errMsg = "";
        //    var fullFileName = "";

        //    lock (objFileSn) {
        //        RecFileSn++;
        //        var yyyymm = recStartTime.ToString("yyyyMM");
        //        var yyyymmdd = recStartTime.ToString("yyyyMMdd");
        //        var chNo = ursChID.ToString("D3");
        //        // path = $urs\data\yyyymm\yyyymmdd\001
        //        var path = Path.Combine(UrsDataPath!, yyyymm, yyyymmdd, chNo);

        //        // 預先開啟路徑
        //        var err = lib_misc.ForceCreateFolder(path); // 順便檢查、開啟Path
        //        if (!string.IsNullOrEmpty(err)) {
        //            errMsg = $"URS 錄音檔路徑無法開啟({path}): {err}";                    
        //        }
        //        else {
        //            var machineID = (GVar.Config?.Recording?.MachineID ?? 1) * 10;                    
        //            var retry = 1;
        //            var subSn = 1;
        //            while (retry < 10) {                        
        //                subSn = machineID + retry; 
        //                var fileName = $"{yyyymmdd}{RecFileSn:X4}{subSn:D2}_{chNo}.wav"; // IP錄音大部分是 PCMA、PCMU，故統一用 711

        //                var file_wav = Path.Combine(path, fileName);
        //                var file_711 = Path.ChangeExtension(file_wav, ".711");
        //                // 檢查是否重複，一旦重複就會造成後續錄音檔產製失敗
        //                if (!File.Exists(file_711) && !File.Exists(file_wav)) {
        //                    fullFileName = file_wav; // 最後 return wav file
        //                    _nlog.Info($"[URS] GetRecFullFileName: recID={recID}, RecFileSn={RecFileSn}, subSn={subSn} => {fullFileName}");
        //                    break;
        //                }
        //                else {
        //                    RecFileSn++;
        //                    retry++;
        //                }                                                                                   
        //            }
        //            if (string.IsNullOrEmpty(fullFileName)) {
        //                errMsg = $"無法產生唯一的錄音檔檔名(recID={recID}, path={path}, retry={retry}, RecFileSn={RecFileSn}, subSn={subSn})";
        //                _nlog.Error($"[URS] GetRecFullFileName Error: {errMsg}");                        
        //            }
        //        }                    
        //    }            

        //    return fullFileName;
        //}

        public string GetRecFullFileName(ulong recID, DateTime recStartTime, int ursChID, out string errMsg) {
            errMsg = "";
            var fullFileName = "";

            lock (objFileSn) {
                var yyyymm = recStartTime.ToString("yyyyMM");
                var yyyymmdd = recStartTime.ToString("yyyyMMdd");
                var chNo = ursChID.ToString("D3");
                var path = Path.Combine(UrsDataPath!, yyyymm, yyyymmdd, chNo);

                var err = lib_misc.ForceCreateFolder(path);
                if (!string.IsNullOrEmpty(err)) {
                    errMsg = $"URS 錄音檔路徑無法開啟({path}): {err}";
                }
                else {
                    var machineID = (GVar.Config?.Recording?.MachineID ?? 1) * 10;
                    var retry = 1;
                    string lastAttemptFileName = ""; // 提升作用域，讓 Log 抓得到

                    while (retry <= 10) { // 改為 <= 10，給足 10 次機會
                        RecFileSn++; // 進入迴圈即遞增，確保每次 attempt 都是新序號
                        var subSn = machineID + retry;
                        var fileName = $"{yyyymmdd}{RecFileSn:X4}{subSn:D2}_{chNo}.wav";
                        lastAttemptFileName = fileName;

                        var file_wav = Path.Combine(path, fileName);
                        var file_711 = Path.ChangeExtension(file_wav, ".711");

                        if (!File.Exists(file_711) && !File.Exists(file_wav)) {
                            fullFileName = file_wav;
                            _nlog.Info($"[URS] GetRecFullFileName: recID={recID}, RecFileSn={RecFileSn}, subSn={subSn} => {fullFileName}");
                            break;
                        }
                        else {
                            // 發生碰撞，下一圈會自動 RecFileSn++ 並增加 subSn
                            retry++;
                        }
                    }

                    if (string.IsNullOrEmpty(fullFileName)) {                        
                        errMsg = $"無法產生唯一的錄音檔檔名(recID={recID}, path={path}, retry={retry}, lastFileName={lastAttemptFileName})";
                        _nlog.Error($"[URS] GetRecFullFileName Error: {errMsg}");
                    }
                }
            }

            return fullFileName;
        }

        public (bool Result, string ErrorMsg) WriteRecordingList(RecordingDataModel recDataModel) {            
            var recData = MakeRecDataString(recDataModel);
            //            
            var fileRet = PrepareDatFileName(recDataModel.RecStartTime);
            if (!fileRet.Item1) {
                return (false, $"無法準備 dat 檔案: {fileRet.Item2}");
            }
            var datFileName = fileRet.Item2;

            #region 準備寫入錄音索引，            
            var ret = (true, "");
            try {
                File.AppendAllText(datFileName, recData + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex) {
                ret.Item1 = false;
                ret.Item2 = ex.Message;
            }
            #endregion
            return ret;
        }

        private string MakeRecDataString(RecordingDataModel model) {
            var s = $"{model.RecID,-8}\t" +
                    $"{model.AgentID,-12}\t" +
                    $"{model.BaseFileName,-25}\t" +
                    $"{model.RecLen,-5}\t" +

                    $"{model.RecStartTime.ToString("yyyyMMdd HH:mm:ss"),-17}\t" +
                    $"{model.ChNo,-3}\t" +
                    $"{model.ChName,-20}\t" +
                    $"{model.Ext,-12}\t" +

                    $"{model.CallType}\t" +
                    $"{model.RecType}\t" +
                    $"{model.TriggerType}\t" +
                    $"{model.CallerID,-50}\t" +

                    $"{model.DTMF,-50}\t" +
                    $"{model.SMDR}\t" +
                    $"{model.DNIS,-50}\t" +

                    $"{model.rev1}\t" +
                    $"{model.DID}\t" +
                    $"{model.RingLen,-3}\t" +
                    $"{model.rev4}\t" +
                    $"{model.rev5}\t" +
                    $"{model.rev6}\t" +
                    $"{model.rev7}\t" +

                    $"{model.RecGuid}\t" +
                    $"{model.CTIEventLink}\t";
            return s;
        }

        //public RecordingDataModel MakeRecordingDataModel(RecChannel recCh, string ursFileName) {
        //    // 判斷 urs檔案不在(複製失敗)，就指回去原檔名
        //    var finalFileName = ursFileName;
        //    if (string.IsNullOrEmpty(finalFileName) || !File.Exists(finalFileName)) {
        //        finalFileName = recCh.WavFileName; 
        //    }

        //    var recStartTime = recCh.RecStartTime.HasValue ? recCh.RecStartTime.Value : DateTime.MinValue;
        //    var recStopTime = recCh.RecStopTime.HasValue ? recCh.RecStopTime.Value : DateTime.MinValue;
        //    var recLen = recCh.Duration;

        //    var model = new RecordingDataModel();
        //    model.LoggerSeq = LoggerConfig?.LoggerSeq ??  0;
        //    model.LoggerID = LoggerConfig?.LoggerID ?? "";
        //    model.LoggerName = LoggerConfig?.LoggerName ?? "";
        //    model.RecID = recCh.RecID;
        //    model.AgentID = "";
        //    model.RecFileSizeMB = lib_misc.GetFileSizeMB(finalFileName); // 取得檔案大小，有可能加密或不加密
        //    model.NewRecFolder = Path.GetDirectoryName(finalFileName) ?? "";
        //    model.BaseFileName = Path.GetFileName(finalFileName); // 檔名是 711，但實際檔案仍是 wav file
        //    model.RecLen = (int)recLen;
        //    model.RecStartTime = recStartTime;
        //    model.ChType = 5; // 為了相容，還是設為三匯IP錄音
        //    model.ChNo = recCh.ChNo;
        //    model.ChName = recCh.Extn;
        //    model.Ext = recCh.Extn;

        //    var callType = 0; // urs 那邊的定義 unknown=0, inbound=4, outbound=5
        //    #region Tranlate Inbound/Outbound...
        //    if (recCh.CallDir == ENUM_CallDirection.Inbound)
        //        callType = 4;
        //    else if (recCh.CallDir == ENUM_CallDirection.Outbound)
        //        callType = 5;
        //    #endregion
        //    model.CallType = callType;

        //    model.RecType = 4;  // SIP 固定用 Schedule 方式錄音, USR 定義: Idle = 2, SOD = 3, Schedule = 4, Continuous = 5
        //    model.TriggerType = 6; // 錄音啟動方式，SIP=6, CTI=5, API=4, ... 
        //    model.SMDR = 0;

        //    model.CallerID = recCh.CallerID;
        //    model.DTMF = recCh.CalledID; // outbound時，對方的號碼
        //    model.DNIS = "";

        //    if (callType == 4) { // inbound             
        //        model.DTMF = recCh.RecvDTMF; //inbound 時，放對方所按的按鍵
        //        model.DNIS = ""; //要放自己的號碼，但是沒有 SIP 這個資訊
        //    }
        //    else if (callType == 5) { // outbound
        //        model.DNIS = recCh.CalledID;
        //    }

        //    model.rev1 = "";
        //    model.DID = "";
        //    model.RingLen = 0;
        //    model.rev4 = "";
        //    model.rev5 = "";
        //    model.rev6 = "";
        //    model.rev7 = recCh.CallID; // 加入 SIP CallID
        //    model.RecGuid = recCh.RecGUID;
        //    model.CTIEventLink = "";

        //    #region 先不要轉...，保留原始資料
        //    //// 如果callerID 是 ip，設為 "" 
        //    //if (!string.IsNullOrEmpty(model.CallerID)) {
        //    //    int freq = model.CallerID.Where(x => (x == '.')).Count();
        //    //    if (freq >= 1)
        //    //        model.CallerID = "";
        //    //}
        //    #endregion

        //    return model;
        //}

        //public string CopyRecordingFile(RecChannel recCh, out bool encrypted, out string err) {
        //    encrypted = false;
        //    err = "";

        //    var ursFileName = "";
        //    var wavFile = recCh.WavFileName;
        //    var encFile = Path.ChangeExtension(recCh.WavFileName, "enc");
        //    var targetFile = "";

        //    if (File.Exists(wavFile)) {
        //        targetFile = wavFile;
        //    }
        //    else if (File.Exists(encFile)) {
        //        encrypted = true;
        //        targetFile = encFile;
        //    }
        //    else {
        //        err = $"原始錄音檔(wav|enc) 都不存在, recID={recCh.RecID}, callID={recCh.CallID}\n wavFile={wavFile}\n encFile={encFile}";
        //        return "";
        //    }
            
        //    if (!recCh.RecStartTime.HasValue) {
        //        err = $"錄音開始時間未指定, recID={recCh.RecID}, callID={recCh.CallID}";
        //        return "";
        //    }

        //    ursFileName = GetRecFullFileName(recCh.RecStartTime.Value, recCh.ChNo, out string errMsg);
        //    var ret = lib_misc.CopyFile(targetFile, ursFileName, out err);
        //    return ret == 1 ? ursFileName : "";            
        //}

        public async Task UpdateChannelStatus(ChannelStatusModel model) {
            model.LoggerSeq = LoggerConfig!.LoggerSeq; //補上 LoggerSeq
            if (!model.LineStatus.HasValue)
                return;

            _nlog.Info($"[URS] try to UpdateChannelStatus =>\r\n {JsonConvert.SerializeObject(model, Formatting.Indented)}");
            var db = new RecDb();

            var errHD = await db.UpdateChannelStatus(model);
            if (errHD.Success) {
                _nlog.Info($"[URS] UpdateChannelStatus update ok.");
            }
            else {
                _nlog.Info($"[URS] UpdateChannelStatus update error: {errHD.UserMessage} {errHD.TraceMessage}");
            }
            return;
        }

        public async Task UpdateModuleStatus(int status, string moduleVer) {
            var loggerSeq = LoggerConfig!.LoggerSeq;
            var moduleID = $"URIP";
            var db = new RecDb();
            var errHD = await db.UpdateModuleStatus(moduleID, status, moduleVer, loggerSeq);
            if (errHD.Success) {
                _nlog.Info($"[URS] UpdateModuleStatus update ok.");
            }
            else {
                _nlog.Info($"[URS] UpdateModuleStatus update error: {errHD.UserMessage} {errHD.TraceMessage}");
            }
        }
    }
}
