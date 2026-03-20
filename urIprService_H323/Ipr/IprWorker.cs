using MyProject.Database;
using MyProject.lib;
using MyProject.Models;
using MyProject.ProjectCtrl;
using MyProject.Utils;
using NLog;
using richpod.synway;
using shpa3api;
using Synway.Models;
using System.Threading.Channels;
using urIprService.Models;
using urSynIpr.lib;

namespace IprService.Ipr {
    public partial class IprWorker {
        private readonly Logger? _chLog = null;
        private readonly Channel<SsmEventDataModel> _channel; // 假設資料是 byte[]
        private readonly int _hwChID;
        private readonly string _className;
        private readonly ENUM_IprChType _chType;

        private readonly long _loggerSeq;
        private readonly int _ursChID;  // URS的迴路編號(start from 1, 當 ChType = IPR_REC 時，UrsChNo = -1)
        private readonly string _extNo; // 分機號碼，當 ChType = IPR_REC 時，ExtNo = ""
        private readonly int _stationID;// IPR_ANA 的 stationID，當 ChType = IPR_REC 時，StationID = -1)
        private readonly int _iprRecID; // 若是 IPR_ANA 迴路時，_IprRecChID = _hwChID+ IprOffset;
        private readonly int _iprAnaID;
        private readonly RecDb _recDb = new RecDb();

        private CallRefManager callRefMgr = new CallRefManager();
        // 2021/07/25 用來控制每一個Channel 的 RemotePartyID
        private ChannelControlDataModel ChCtrl = new ChannelControlDataModel();

        public LPRECTOMEM RTPCallBack = new LPRECTOMEM(RtpCallbackFunc);

        private CancellationTokenSource? _cts;


        public IprWorker(int hwChID, ENUM_IprChType chType) {
            _className = this.GetType().Name;
            _hwChID = hwChID;
            _chType = chType;
            _loggerSeq = GVar.URS!.LoggerConfig!.LoggerSeq;

            if (_chType == ENUM_IprChType.IPR_ANA) {
                _iprAnaID = hwChID;
                _iprRecID = hwChID + GVar.CTI!.IPR_ANA_REC_OFFSET;
                _ursChID = GVar.CTI!.IPRChInfo[hwChID].MapToUrsChID;   // 在 GetAndSetFreeIprAnaChannelID() 中會設定
                _extNo = GVar.CTI!.IPRChInfo[hwChID].MapToExt;         // 在 GetAndSetFreeIprAnaChannelID() 中會設定
                _stationID = GVar.CTI!.IPRChInfo[hwChID].StationID;    // 在 GetAndSetFreeIprAnaChannelID() 中會設定                                
            }
            // 對 IPR_REC 迴路而言
            else {
                _iprAnaID = hwChID - GVar.CTI!.IPR_ANA_REC_OFFSET;
                _iprRecID = hwChID;
                _ursChID = GVar.CTI!.IPRChInfo[_iprAnaID].MapToUrsChID;  // 對應到 IPR_ANA.MapToUrsChID，負責錄音的 UrsChID
                _extNo = GVar.CTI!.IPRChInfo[_iprAnaID].MapToExt;        // 對應到 IPR_ANA.MapToExt
                _stationID = GVar.CTI!.IPRChInfo[_iprAnaID].StationID;   // 對應到 IPR_ANA.MapToExt
            }

            _chLog = LogManager.GetLogger($"chIpr_{hwChID.ToString("D3")}");

            // BoundedChannel 可以防止內存爆掉（背壓機制）
            _channel = Channel.CreateUnbounded<SsmEventDataModel>(new UnboundedChannelOptions {
                SingleReader = true, // 效能優化：明確告知只有一個讀取者
                AllowSynchronousContinuations = false
            });
        }

        public void Start() {
            _cts = new CancellationTokenSource();
            // 使用 LongRunning 提示 ThreadPool 建立獨立執行緒，避免阻塞 Pool
            Task.Factory.StartNew(() => DoWorkAsync(_cts.Token),
                TaskCreationOptions.LongRunning);
        }

        // 外部持續放入資料的方法
        public bool EnqueueData(SsmEventDataModel ssmModel) {
            return _channel.Writer.TryWrite(ssmModel);
        }

        private async Task DoWorkAsync(CancellationToken ct) {
            var proto = GVar.Config?.Recording?.RecProto ?? ENUM_RecProto.Sip;

            _chLog!.Info($"{new string('#', 85)}");
            _chLog!.Info($"############ [{proto}] Recording: iprChID={_hwChID}, an {_chType} channel. Task is running ...        #####");
            _chLog!.Info($"############ _IprAnaID={_iprAnaID}, _IprRecID={_iprRecID}, _UrsChID={_ursChID}, _ExtNo={_extNo}, _StationID={_stationID} #####");
            _chLog!.Info($"{new string('#', 85)}");

            // init ChannelStatus
            await _recDb.UpdateChannelStatus(_loggerSeq, _ursChID, ENUM_LineStatus.Idle, ENUM_RecordingType.Idle, "", "");

            try {
                // 持續讀取，直到 Channel 被關閉或 Token 取消
                // 當沒有資料時，這裡會非同步地等待，不消耗 CPU
                await foreach (var ssmModel in _channel.Reader.ReadAllAsync(ct)) {
                    // 執行實際的消化邏輯
                    try {
                        if (proto == ENUM_RecProto.Sip)
                            ProcessData_SIP(ssmModel);
                        else if (proto == ENUM_RecProto.H323)
                            ProcessData_H323(ssmModel);
                    }
                    catch (Exception ex) {
                        _chLog!.Info($"ProcessEvent_Cisco() exception: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException) {
                /* 正常退出 */
            }
            catch (Exception ex) {
                _chLog!.Error(ex, $"Worker IPR[{_hwChID}] error");
            }
            finally {
                _chLog!.Info($"Worker for IPR[{_hwChID}] stopped.");
            }
        }        

        private CallRefDataModel CreateCallRefObj(CallInfoDataModel callInfo) {
            var callRefObj = callRefMgr.AddCallRef(callInfo.CallRef);
            // Inbound
            if (callInfo.CallSource == 1) {
                callRefObj.CallDir = ENUM_CallDir.Inbound;
                callRefObj.DTMF = "";
                callRefObj.CallerID = GetSipExtension(callInfo.szCallerId, "sip:", "sip:", "@");
            }
            // Outbound
            #region Outbound 處理說明                    
            // 1. Outbound時，mktCallInfo.szCalledId 不准, 要抓之前出現的 DE_REMOTE_PARTYID 裡面的 sip 字串(在一定的時間內)
            // 2. 但如果按 0 抓外線後再撥碼，則 DTMF 會在 E_IPR_RCV_DTMF 時觸發
            //    此時, DE_REMOTE_PARTYID 不會觸發，所以這裡 callRefObj.DTMF 會空值,
            //    至於抓 E_IPR_RCV_DTMF 的部分，StopRecording() 會再判斷一次
            #endregion
            else {
                callRefObj.CallDir = ENUM_CallDir.Outbound;
                // 15 秒內取得的 RemotePartyID 才能拿來當成 outbound 的 DTMF
                if (ChCtrl.GetRemotePartyIDSetSec() <= 15)
                    callRefObj.DTMF = GetSipExtension(ChCtrl.LastRemotePartyID, "sip:", "sip:", "@");
                else
                    callRefObj.DTMF = "";

                callRefObj.CallerID = "";
            }

            // 第一次加入的 CallRefObj, 要用這一次的 Inbound/Outbound 設定
            if (callRefMgr.CallRefCount == 1) {
                ChCtrl.CallDir = callRefObj.CallDir;
                ChCtrl.CallerID = callRefObj.CallerID;
                ChCtrl.DTMF = callRefObj.DTMF;
            }

            return callRefObj;
        }

        private CallRefDataModel CreateCallRefObj(uint callRef) {
            var callRefObj = callRefMgr.AddCallRef(callRef);
            // 因為DE_CALL_CONNECTED 沒有出現, 不知道 Inbound/Outbound, 但大部分應該是 Outbound
            callRefObj.CallDir = ENUM_CallDir.Outbound;
            callRefObj.DTMF = "***";
            callRefObj.CallerID = "***";

            // 第一次加入的 CallRefObj, 要用這一次的 Inbound/Outbound 設定
            if (callRefMgr.CallRefCount == 1) {
                ChCtrl.CallDir = callRefObj.CallDir;
                ChCtrl.CallerID = callRefObj.CallerID;
                ChCtrl.DTMF = callRefObj.DTMF;
            }

            return callRefObj;
        }

        public void Stop() => _cts?.Cancel();

        /// <summary>
        /// 開始錄音：
        /// 1. 取序號(跟舊版不一樣、一開始就取)
        /// 2. 取錄音檔名，設定 RECID、RecFileName、RecStartTime、CallRef(重要) ...等
        /// 3. RecToFileA()
        /// </summary>
        /// <param name="mktEvent"></param>
        /// <returns></returns>
        private async Task<bool> StartRecording(SsmEventDataModel ssmEvt) {
            var errMsg = "";
            var ret = false;
            var nRef = ssmEvt.nReference;
            var recStartTime = DateTime.Now;
            var mktSessionInfo = ssmEvt.SessionInfo;
            ulong recID = Snowflake.NextId();

            var recFullFileName = GVar.URS!.GetRecFullFileName(recID, recStartTime, _ursChID, out errMsg); // full path WAV filename
            if (!string.IsNullOrEmpty(errMsg)) {
                _chLog!.Info($"\t -> START RECORDING ERROR: GetRecFullFileName failed({errMsg}");
                return false;
            }

            var iprCh = GVar.CTI!.IPRChInfo[_hwChID];

            iprCh.RecID = recID;
            iprCh.RecFullFileName = recFullFileName;
            iprCh.RecStartTime = recStartTime;
            iprCh.CallRef = mktSessionInfo.nCallRef;

            string recGuid = Guid.NewGuid().ToString();
            iprCh.RecGuid = recGuid;
            iprCh.CTIEventLink = ""; // <== 要找時間來整合

            //2021/07/28 added
            var callRefObj = callRefMgr.GetCallRef((uint)mktSessionInfo.nCallRef);
            if (callRefObj != null)
                callRefObj.RecStatus = ENUM_RecordingStatus.Start;

            // CTI 的部分            
            _chLog!.Info($"\t -> START RECORDING: recID={recID}, callRef={mktSessionInfo.nCallRef}, sessionId={mktSessionInfo.dwSessionId}, recFileName={recFullFileName}, guid={recGuid}");

            // 將 RecGuid 寫入 tblChannelStatus
            var recType = ENUM_RecordingType.Schedule;
            var lineStatus = ChCtrl.CallDir == ENUM_CallDir.Inbound ? ENUM_LineStatus.Inbound : ENUM_LineStatus.Outbound;
            var errHD = await _recDb.UpdateChannelStatus(_loggerSeq, _ursChID, lineStatus, recType, ChCtrl.CallerID, ChCtrl.DTMF, recGuid, recStartTime);
            if (errHD.Success)
                _chLog!.Info($"\t -> START RECORDING: update channel status ok.(guid={recGuid})");
            else
                _chLog!.Info($"\t -> START RECORDING: update channel status error.(err={errHD.UserMessage})");

            // 這裡本來要從 SharedMemory 抄 CTIEventLink => iprCh.RecControl.CTIEventLink = ???
            // 2020/09/25：CTIEventLink 先不做...  改變作法 => 還未決定...

            var retCode = 0;

            retCode = lib_synway.RecToFileA(_iprRecID, recFullFileName, (int)ENUM_ShCodec.CODEC_711, RTPCallBack);

            if (retCode == 0) { // 錄音成功                            
                _chLog!.Info($"\t -> RecToFileA OK: recID={recID}, fwdPriPort={mktSessionInfo.nFowardingPPort}, fwdSndPort={mktSessionInfo.nFowardingSPort}");
                // 注意: IPRSendSession 的 nRef 是 IPR_ANA 的迴路ID
                ret = StartToSendSession(nRef, "127.0.0.1", GVar.CTI!.IPRChInfo[_iprRecID].PriRcvPort,
                                               "127.0.0.1", GVar.CTI!.IPRChInfo[_iprRecID].SecRcvPort);
            }
            else { // 錄音失敗
                ret = false;
                _chLog!.Info($"\t -> RecToFileA ERROR: recID={recID}");
            }
            return ret;
        }

        /// <summary>
        /// 繼續錄音(因為有錄音暫停):
        ///     1. RestartRecToFile
        ///     2. StartToSendSession
        /// </summary>
        /// <param name="mktEvent"></param>
        /// <returns></returns>
        private bool RestartRecording(SsmEventDataModel ssmEvt) {
            var ret = true;
            var nRef = ssmEvt.nReference;
            var stationID = ssmEvt.stationID;

            var iprCh = GVar.CTI!.IPRChInfo[_hwChID];

            _chLog!.Info($"\t -> RE-START RECORDING: staID={stationID}, ext={_extNo}, iprRecChID={_iprRecID}");

            // *** 因為 Restart 錄音的時候，要重新指定 CallRef ***
            var newCallRef = ssmEvt.SessionInfo.nCallRef;
            _chLog!.Info($"\t -> RE-START RECORDING: *** oldCallRef={iprCh.CallRef}, newCallRef={newCallRef} ***");
            iprCh.CallRef = newCallRef;

            //2021/07/28 added
            var callRefObj = callRefMgr.GetCallRef((uint)newCallRef);
            if (callRefObj != null)
                callRefObj.RecStatus = ENUM_RecordingStatus.Start;

            // Restart Recording ...
            if (lib_synway.RestartRecToFile(_iprRecID) >= 0) {
                _chLog!.Info($"\t -> RestartRecToFile OK");
                var iprRecCh = GVar.CTI!.IPRChInfo[_iprRecID];
                ret = StartToSendSession(nRef, "127.0.0.1", iprRecCh.PriRcvPort, "127.0.0.1", iprRecCh.SecRcvPort);
            }
            else {
                ret = false;
                _chLog!.Info($"\t -> RestartRecToFile Error");
            }
            return ret;
        }

        private bool StartToSendSession(int iprAnaChID, string priSlaverAddr, int priSlaverPort, string secSlaverAddr, int secSlaverPort) {
            var ret = true;
            var pri = $"{priSlaverAddr}:{priSlaverPort}";
            var sec = $"{secSlaverAddr}:{secSlaverPort}";
            if (lib_synway.IPRSendSession(iprAnaChID, priSlaverAddr, priSlaverPort, secSlaverAddr, secSlaverPort) >= 0) {
                _chLog!.Info($"\t -> SEND SESSION OK: iprChID={iprAnaChID}, priAddrPort={pri}, sndAddrPort={sec}");
            }
            else {
                _chLog!.Info($"\t -> SEND SESSION ERROR: iprChID={iprAnaChID}, priAddrPort={pri}, sndAddrPort={sec}");
                ret = false;
            }
            return ret;
        }

        public void StopRecording(uint callRef) {            
            var iprCh = GVar.CTI!.IPRChInfo[_hwChID];
            var stationID = iprCh.StationID;
            _chLog!.Info($"\t -> try StopRecording ...staID={stationID}, ext={_extNo}, hwChID={_hwChID}, callRef={callRef}, iprRecChID={_iprRecID}");

            // 1. 檢查目前這一個 event 的 callRef 是否事先存在，不存在則不能停止錄音，必須繼續錄音
            var callRefObj = callRefMgr.GetCallRef(callRef);
            if (callRefObj == null) {
                _chLog!.Info($"\t\t > CallRef({callRef}) not found, ****** StopRecording() ignored! ******");
                return;
            }
            _chLog!.Info($"\t\t > Current CallRef({callRef}): CallDir={callRefObj.CallDir}, SessionStartTime={callRefObj.SessionStartTime.ToTimeStr()}, RecStatus={callRefObj.RecStatus}, CallerID={callRefObj.CallerID}, DTMF={callRefObj.DTMF}, status={callRefObj.Status}");
            _chLog!.Info($"\t\t > ChCtrl: CallDir={ChCtrl.CallDir}, CallerID={ChCtrl.CallerID}, DTMF={ChCtrl.DTMF}");

            // 無論如何，要先把這個 callRef 移除
            callRefMgr.RemoveCallRef(callRef);

            // 移除有問題的 CallRefObj
            var maxHoldTimeMin = 30; // <= TODO: 移到 appsettings
            callRefMgr.CleanupInvalidRefs(15, maxHoldTimeMin);
            //RemoveInvalidCallRefObj();

            // 2. 檢查是否還有其他的 callRef 存在(例如之前的held)，如果有，仍然不能停止錄音，必須繼續錄音
            if (callRefMgr.CallRefCount >= 1) { // 不包含自己，                
                _chLog!.Info($"\t\t > Warning: total {callRefMgr.CallRefCount} CallRef is recording, *** should not stop recording ***");
                return;
            }

            DoStopRecording();

            //SignalR_Notify_RecState();
        }

        private void DoStopRecording() {
            var iprCh = GVar.CTI!.IPRChInfo[_hwChID];

            if (iprCh.RecordingState == (int)ENUM_RecordingState.Idle) {
                _chLog!.Info($"\t\t > StopRecording ERROR: Global.IPRChInfo[{_hwChID}].RecordingState is RECORDING_IDLE");
                return;
            }
            _chLog!.Info($"\t\t > DoStopRecording starting ...");
            #region 處理 Inbound(CallerID) &  Outbound(DTMF)                                    
            // 2021/08/08 這裡的 Inbound/Outbound 不能抓 callRefObj, 應該要抓 chCtrl
            // 因為要抓第一次的 callRefObj 那時候的資訊，而第一次的資訊會記錄在 chCtrl 之中
            iprCh.CallSource = (int)ChCtrl.CallDir;
            iprCh.CallerID = ChCtrl.CallerID;

            // 如果 ChCtrl.DTMF = 空白，表示有可能是按 0 抓外線再撥號，此時會觸發 E_IPR_RCV_DTMF,     
            // 觸發 E_IPR_RCV_DTMF 時，DTMF 會填入 iprCh.DTMF，所以不用另外指定            
            if (!string.IsNullOrEmpty(ChCtrl.DTMF)) {
                iprCh.DTMF = ChCtrl.DTMF;
            }

            if (ChCtrl.CallDir == ENUM_CallDir.Inbound)
                _chLog!.Info($"\t\t\t > StopRecording: inbound call, Get callerID={iprCh.CallerID}");
            else
                _chLog!.Info($"\t\t\t > StopRecording: outbound call, Get DTMF={iprCh.DTMF}");
            //--------------------------------------------------------------------------------------
            #endregion

            // IPR_StopRecToFile                            
            if (lib_synway.StopRecToFile(_iprRecID, out string errMsg) == 0)
                _chLog!.Info($"\t\t\t -> StopRecording: StopRecToFile(iprRecChID={_iprRecID}) ok.");
            else {
                _chLog!.Info($"\t\t\t -> StopRecording: StopRecToFile(iprRecChID={_iprRecID}) error: {errMsg}");
            }

            // Update ChannelStatus
            var lineStatus = ENUM_LineStatus.Idle;
            var recType = ENUM_RecordingType.Idle;
            var errHD = _recDb.UpdateChannelStatus(_loggerSeq, _ursChID, lineStatus, recType, "", "").GetAwaiter().GetResult();
            if (errHD.Success)
                _chLog!.Info($"\t\t\t -> StopRecording: update channel status ok");
            else
                _chLog!.Info($"\t\t\t -> StopRecording: update channel status error.(err={errHD.UserMessage})");

            // 完成結束錄音            
            try {
                CompleteRecording(_hwChID);
            }
            catch (Exception ex) {
                _chLog!.Info($"\t\t\t -> CompleteRecording() raise an exception: {ex.Message}");
            }

            // 要先 Reset 的欄位
            iprCh.RecordedCount++;
            GVar.AddRecordedCount();
            iprCh.RecordingState = (int)ENUM_RecordingState.Idle;
            GVar.DecRecordingCount();

            iprCh.RingFlag = 0;
            iprCh.DTMF = "";
            ChCtrl.Reset();
            callRefMgr.ClearAll();
        }

        private bool CompleteRecording(int iprAnaID) {

            // 這裡用 _hwChID 來取 IPR，理論上應用 iprAnaID，
            // 不過 caller 是傳 _hwChID 進來的，所以結果一樣，為了一致性，更正為 iprAnaID
            var recID = GVar.CTI!.IPRChInfo[iprAnaID].RecID;

            _chLog!.Info($"\t -> CompleteRecording... recID={recID}, iprAnaChID={iprAnaID}, recStartTime={GVar.CTI!.IPRChInfo[iprAnaID].RecStartTime.ToString("yyyy/MM/dd HH:mm:ss")}");
            _chLog!.Info($"\t\t > Prepare RecordingDataMode... recID={recID}");
            var model = MakeRecordingDataModel(iprAnaID);
            //SignalR_Notify_StopRecording(model);

            _chLog!.Info($"\t\t > Enqueue RecordingDataMode... recID={model.RecID}");
            GVar.AddUrsTaskQueue(model);
            return true;
        }

        // 複製 Global.IPRChInfo[iprAnaChID] 裡面的欄位值 => RecordingDataModel
        private RecordingDataModel MakeRecordingDataModel(int iprAnaChID) {
            _chLog!.Info($"\t > MakeRecordingDataModel...(ursChID={_ursChID}), ext={_extNo}");

            // 注意，是 iprAnaChID 的 IPR
            var iprAnaCh = GVar.CTI!.IPRChInfo[iprAnaChID];

            var recStartTime = iprAnaCh.RecStartTime;
            var recStopTime = DateTime.Now;
            var recLen = recStopTime > recStartTime ? (int)(recStopTime - iprAnaCh.RecStartTime).TotalSeconds : 0;

            _chLog!.Info($"\t\t > RecLen={recLen}, RecStartTime={recStartTime.ToStdStr()}, RecStopTime={recStopTime.ToStdStr()}");
            var model = new RecordingDataModel();
            model.LoggerSeq = GVar.URS!.LoggerConfig!.LoggerSeq;
            model.LoggerID = GVar.URS!.LoggerConfig!.LoggerID;
            model.LoggerName = GVar.URS!.LoggerConfig!.LoggerName!;
            model.RecID = (long)iprAnaCh.RecID;
            model.AgentID = GVar.URS!.GetUrsChAgentID(_extNo);

            // 注意: 此時錄音檔還沒被 rename, 實際仍是 wav file
            var wavFileName = iprAnaCh.RecFullFileName; // *.wav
            model.RecFileSizeMB = lib_misc.GetFileSizeMB(wavFileName); // 取得檔案大小，在 ConvertService 中，若加密，會再度更新 RecFileSizeMB            

            var fileName = Path.ChangeExtension(wavFileName, ".711"); // change *.711
            model.NewRecFolder = Path.GetDirectoryName(fileName) ?? "";
            model.BaseFileName = Path.GetFileName(fileName); // 檔名是 711，但實際檔案仍是 wav file
            model.RecLen = recLen;
            model.RecStartTime = recStartTime;
            model.ChType = (int)GVar.URS!.ChannelType;
            model.ChNo = _ursChID;
            model.ChName = GVar.URS!.GetUrsChChName(_extNo);
            model.Ext = _extNo;

            // Tranlate Inbound/Outbound...
            var callSrc = (ENUM_CallSource)iprAnaCh.CallSource;
            if (callSrc == ENUM_CallSource.Inbound)
                model.CallType = (int)ENUM_UrsCallType.Inbound;
            else if (callSrc == ENUM_CallSource.Outbound)
                model.CallType = (int)ENUM_UrsCallType.Outbound;
            else
                model.CallType = (int)ENUM_UrsCallType.Unknown;

            model.RecType = (int)ENUM_RecordingType.Schedule; // SIP 固定用 Schedule 方式錄音
            model.TriggerType = (int)ENUM_RecTriggerType.SIP;
            model.CallerID = iprAnaCh.CallerID;
            model.DTMF = iprAnaCh.DTMF;
            model.SMDR = 0;
            model.DNIS = "";
            model.rev1 = "";
            model.DID = "";
            model.RingLen = 0;
            model.rev4 = "";
            model.rev5 = "";
            model.rev6 = "";
            model.rev7 = "";
            model.RecGuid = iprAnaCh.RecGuid;
            model.CTIEventLink = iprAnaCh.CTIEventLink;

            // 如果callerID 是 ip，設為 "" 
            if (!string.IsNullOrEmpty(model.CallerID)) {
                int freq = model.CallerID.Where(x => (x == '.')).Count();
                if (freq >= 1)
                    model.CallerID = "";
            }

            return model;
        }

        public static string GetSipExtension(string srcStr, string leadStr = "To:", string startStr = "sip:", string endStr = "@") {
            if (string.IsNullOrWhiteSpace(srcStr)) return string.Empty;

            // 1. 找到標籤（例如 "To:"），忽略大小寫
            int leadIdx = srcStr.IndexOf(leadStr, StringComparison.OrdinalIgnoreCase);
            if (leadIdx == -1) return string.Empty;

            // 2. 從標籤後的位置開始找 "sip:"
            // 這樣可以確保我們抓到的是該 Header 內的 sip 位址，而不是其他地方的
            int searchOffset = leadIdx + leadStr.Length;
            int startIdx = srcStr.IndexOf(startStr, searchOffset, StringComparison.OrdinalIgnoreCase);
            if (startIdx == -1) return string.Empty;

            // 真正的分機起點在 "sip:" 字串結束後
            int extensionStart = startIdx + startStr.Length;

            // 3. 尋找結尾符號 "@"
            int endIdx = srcStr.IndexOf(endStr, extensionStart, StringComparison.OrdinalIgnoreCase);

            // 4. 擷取字串
            if (endIdx != -1 && endIdx > extensionStart) {
                string result = srcStr.Substring(extensionStart, endIdx - extensionStart);

                // 額外處理：如果分機內包含引號或其他雜訊，予以清除
                return result.Trim(' ', '"', '<', '>');
            }

            return string.Empty;
        }

        // *** 錄音迴路 Callback 回來的，所以 ch 是後面的 IPR_REC 迴路編號 ***
        public static void RtpCallbackFunc(int ch, IntPtr lpData, uint dwDataLen) {
            //
            // ===> 之後再來優化 <==================================================
            //
            //if (!CheckIfSendRTP(ch)) {
            //    if (Config.SendRTPLog)
            //        nLog.Trace($"CheckIfSendRTP= false, ch={ch}, dwDataLen={dwDataLen}");
            //    return;
            //}
            ////
            //if (Config.UseRtpGateway) {
            //    SendRtpGateway(ch, lpData, dwDataLen);
            //    return;
            //}

            //#region 以下為本機傳送 RTP
            //if (Global.Config.SendRTPLog)
            //    nLog.Trace($"Re-direct rtp packet,  ch={ch}, dwDataLen = {dwDataLen,6}");

            //byte[] payload = new byte[dwDataLen];
            //try {
            //    Marshal.Copy(lpData, payload, 0, (int)dwDataLen);
            //}
            //catch (Exception ex) {
            //    nLog.Trace($"Marshal copy packet exception(payload): {ex.Message}");
            //    return;
            //}

            //var iprAnaChID = ch - Global.IPR_ANA_REC_OFFSET; // ***注意: ch 實際上是錄音迴路            
            //var ursChID = Global.IPRChInfo[iprAnaChID].MapToUrsChID;  // 對應到 IPR_ANA.MapToUrsChID，負責錄音的 UrsChID
            //var endPoint = Global.IPRChInfo[ch].LiveMonitor.RtpEndPoint;
            //if (Global.Config.SendRTPLog)
            //    nLog.Trace($"try to send RTP data...(iprRecCh={ch}, ursCh={ursChID}, iprAndCh={iprAnaChID}, endPoint={endPoint}, dwDataLen = {dwDataLen,6})");

            //try {
            //    SendRTP(endPoint, payload, ursChID, ch, ENUM_UrsVoiceCodec.CODEC_711, dwDataLen, Global.Config.SendRTPLog);
            //}
            //catch (Exception ex) {
            //    nLog.Trace($"send rtp data exception: {ex.Message}");
            //}
            //#endregion

            return;
        }

        public void PrintIprDebug() {
            if (_chLog == null || !(GVar.Config?.URS?.PrintIprDebug ?? false))
                return;

            try {
                // 檢查 CTI 物件與索引邊界
                var cti = GVar.CTI;
                if (cti?.IPRChInfo == null || _hwChID < 0 || _hwChID >= cti.IPRChInfo.Count) {
                    _chLog.Trace($"\t *** PrintDebugInfo: Invalid HW Channel ID {_hwChID}");
                    return;
                }

                var iprCh = cti.IPRChInfo[_hwChID];

                // 1. 使用字串內插 (支援 .NET Core 全系列)

                _chLog.Trace($"*** IPRChInfo[{_hwChID}].Data => RecID={iprCh.RecID}, ChState={lib_ctiDecode.DecodeChStateStr(iprCh.ChState)}, CallState={iprCh.CallState}, RecordingState={iprCh.RecordingState}, CallRef={iprCh.CallRef}, StationID={iprCh.StationID}, RecStartTime={iprCh.RecStartTime.ToStdStr()}, RecPauseTime={iprCh.RecPauseTime.ToStdStr()}, RemotePartyID={iprCh.RemotePartyID}, DTMF={iprCh.DTMF}, RingFlag={iprCh.RingFlag}, RecFullFileName={iprCh.RecFullFileName}, RecGuid={iprCh.RecGuid}");

                // 2. ChCtrl 資訊
                _chLog.Trace($"*** ChCtrl.Data => lastRemotePartyID={ChCtrl.LastRemotePartyID}, setTime={ChCtrl.RemotePartyIDSetTime.ToTimeStr()}, CallDir={ChCtrl.CallDir}, CallerID={ChCtrl.CallerID}, DTMF={ChCtrl.DTMF}");

                // 3. .NET 8 替代 .Index() 的寫法
                // 使用 Select 同時取出 索引 與 物件
                _chLog.Trace($"*** callRefMgr => ");
                var callRefSnapshot = callRefMgr.CallRefList.Select((cr, idx) => new { Index = idx + 1, Data = cr });
                foreach (var item in callRefSnapshot) {
                    var cr = item.Data;
                    _chLog.Trace($"\t @@@ {item.Index}. CallRef({cr.CallRef}): CallDir={cr.CallDir}, CreatedTime={cr.CreatedTime.ToTimeStr()}, SessionStartTime={cr.SessionStartTime.ToTimeStr()}, RecStatus={cr.RecStatus}, CallerID={cr.CallerID}, DTMF={cr.DTMF}, status={cr.Status}, LastHeldTime={cr.LastHeldTime.ToTimeStr()}");
                }

                _chLog.Trace(string.Empty);
            }
            catch (Exception ex) {
                _chLog.Error(ex, "PrintIprDebug exception");
            }
        }

    }
}