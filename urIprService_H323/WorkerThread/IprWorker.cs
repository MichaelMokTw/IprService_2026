using MyProject.Database;
using MyProject.lib;
using MyProject.Models;
using MyProject.ProjectCtrl;
using MyProject.Utils;
using NLog;
using richpod.synway;
using shpa3api;
using Synway.Models;
using System.Security.AccessControl;
using System.Threading.Channels;
using urIprService.Models;
using urSynIpr.lib;
public class IprWorker {
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

        _chLog = LogManager.GetLogger($"ChIpr_{hwChID.ToString("D3")}");

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
        _chLog!.Trace($"\r\n\r\n\r\n############ Channel: iprChID={_hwChID}, an {_chType} channel. task is running ... ############");
        _chLog!.Trace($"_IprAnaID={_iprAnaID}, _IprRecID={_iprRecID}, _UrsChID={_ursChID}, _ExtNo={_extNo}, _StationID={_stationID}");        
        
        // init ChannelStatus
        await _recDb.UpdateChannelStatus(_loggerSeq, _ursChID, ENUM_LineStatus.Idle, ENUM_RecordingType.Idle, "", "");

        try {
            // 持續讀取，直到 Channel 被關閉或 Token 取消
            // 當沒有資料時，這裡會非同步地等待，不消耗 CPU
            await foreach (var ssmModel in _channel.Reader.ReadAllAsync(ct)) {
                // 執行實際的消化邏輯
                try {
                    ProcessData(ssmModel);
                }
                catch (Exception ex) {
                    _chLog!.Trace($"ProcessEvent_Cisco() exception: {ex.Message}");
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

    private void ProcessData(SsmEventDataModel ssmEvt) {
        uint slaverID = 0;
        uint DTMF = 0;
        uint ret = 0;
        string errMsg = "";
        var sessionStr = "";
        CallRefDataModel? callRefObj;

        uint callRef = 0;
        var nRef = ssmEvt.nReference;
        var eventName = ssmEvt.eventName;
        var stationID = ssmEvt.stationID;
        var iprCh = GVar.CTI!.IPRChInfo[_hwChID];

        _chLog!.Trace($"=====> [{eventName,-32}], nRef={nRef}, staID={stationID}, hWChID={_hwChID}, ursChID={_ursChID}, extNo={_extNo}");
        _chLog!.Trace($"=====> eventName={ssmEvt.eventName}, RawData:{ssmEvt.RawData}");
        switch (ssmEvt.wEventCode) {
            #region IPR_REC(後面的 port) 收到的 Event
            // 由 REC Channel 丟出此訊息 
            case (ushort)EventCode.E_CHG_ChState:
                var chState = ssmEvt.GetChStateStr();
                _chLog!.Trace($"\t -> IPRChInfo[{_hwChID}].ChState: {chState}");
                iprCh.ChState = (int)(ssmEvt.dwParam & 0x0000ffff);
                break;

            // 由 REC Channel 丟出此訊息 
            case (ushort)EventCode.E_IPR_RCV_DTMF:
                #region E_IPR_RCV_DTMF 說明                    
                // 這個 event 比較特殊，按 0 之後抓到外線，
                // 之後外撥時的按鍵Event會在這裡出現，此時直接寫入 Global.IPRChInfo[_IprAnaChID].DTMF                    
                #endregion
                // 實際資料跟三匯文件講的不同，沒有 slaverID 或 ...，只有最低 byte 的 DTMF 

                // 要在時間設定內才會處理 DTMF，避免收到客戶輸入的 ID 跟密碼
                var iprAnaCh = GVar.CTI!.IPRChInfo[_iprAnaID];
                var timeLen = (int)(DateTime.Now - iprAnaCh.RecStartTime).TotalSeconds;
                if (timeLen <= GVar.Config!.IPR.GetDtmfMaxSec) {
                    DTMF = (ssmEvt.dwParam & 0x000000ff);
                    _chLog!.Trace($"\t -> DTMF={(char)DTMF}");
                    iprAnaCh.DTMF = iprAnaCh.DTMF + (char)DTMF;
                    _chLog!.Trace($"\t -> ext={_extNo}, IPRChInfo[{_iprAnaID}].DTMF={iprAnaCh.DTMF}");
                }
                break;
            // 由 REC Channel 丟出此訊息 
            case (ushort)EventCode.E_PROC_RecordEnd:
                // 最後: 這裡不再處裡關於結束錄音的相關工作，全部放在 StopRecording() 中
                var reason = lib_ctiDecode.DecodeRecordEndReason(ssmEvt.dwParam);
                _chLog!.Trace($"\t -> E_PROC_RecordEnd: nRef={nRef}, reason={reason}, staID={stationID}, ext={_extNo}, iprAnaChID={_iprAnaID}, iprRecChID={_iprRecID}, ursChID={_ursChID}");
                //
                sessionStr = ssmEvt.GetSessionInfoStr();
                callRef = (uint)ssmEvt.SessionInfo.nCallRef;
                _chLog!.Trace($"\t -> E_PROC_RecordEnd: callRef={callRef}, sessionInfo({sessionStr})");
                break;

            // 由 REC Channel 丟出此訊息 
            case (ushort)EventCode.E_IPR_STOP_REC_CB:
                _chLog!.Trace($"\t -> E_IPR_STOP_REC_CB");
                break;

            // 由 REC Channel 丟出此訊息 
            case (ushort)EventCode.E_IPR_START_REC_CB:
                _chLog!.Trace($"\t -> E_IPR_START_REC_CB");
                break;

            // 由 REC Channel 丟出此訊息 
            case (ushort)EventCode.E_IPR_ACTIVE_SESSION_CB:
                _chLog!.Trace($"\t -> E_IPR_ACTIVE_SESSION_CB");
                break;
            #endregion

            #region IPR_ANA(前面的 port) 收到的 Event

            case (ushort)EventCode.E_RCV_IPR_DChannel:
                ProcessDChannelEvent(ssmEvt); // <= 統一處理有關 D-Channel 的 Event
                break;

            case (ushort)EventCode.E_RCV_IPR_DONGLE_ADDED:
                break;
            case (ushort)EventCode.E_RCV_IPR_DONGLE_REMOVED:
                break;
            case (ushort)EventCode.E_RCV_IPA_DONGLE_ADDED:
                break;
            case (ushort)EventCode.E_RCV_IPA_DONGLE_REMOVED:
                break;
            case (ushort)EventCode.E_RCV_IPA_APPLICATION_PENDING:
                break;

            case (ushort)EventCode.E_RCV_IPR_STATION_ADDED:
                _chLog!.Trace($"\t -> station added ok: staID[{stationID}], ext={_extNo}, iprAnaChID={_iprAnaID}, iprRecChID={_iprRecID}");
                break;
            case (ushort)EventCode.E_RCV_IPR_STATION_REMOVED:
                _chLog!.Trace($"\t -> WARNING: REMOVE STATION(staID={stationID}");
                break;

            case (ushort)EventCode.E_RCV_IPR_AUTH_OVERFLOW:
                _chLog!.Trace($"\t -> WARNING: IPR-AUTH OVERFLOW ...");
                break;

            case (ushort)EventCode.E_IPR_SLAVER_INIT_CB:
            case (ushort)EventCode.E_IPR_START_SLAVER_CB:
            case (ushort)EventCode.E_IPR_CLOSE_SLAVER_CB:
                _chLog!.Trace($"\t -> slaverID={ssmEvt.dwParam >> 16}, CB_Rreturn={ssmEvt.dwParam & 0xffff}");
                GVar.CTI!.ScanIprSlaver(out string err);
                var slaverInfo = lib_ctiDecode.GetSlaverInfoStr(GVar.CTI!.HwInfo.IPRSlaverAddr);
                _chLog!.Trace($"\t -> {slaverInfo}");
                break;

            case (ushort)EventCode.E_IPR_PAUSE_REC_CB:
            case (ushort)EventCode.E_IPR_RESTART_REC_CB:
                _chLog!.Trace($"\t -> slaverID={ssmEvt.dwParam >> 16}, CB_Rreturn={ssmEvt.dwParam & 0xffff}");
                break;

            case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_STARTED:
            case (ushort)EventCode.E_RCV_IPR_AUX_MEDIA_SESSION_STARTED:
                #region *** IPR_ANA: 開始錄音
                iprCh.CallState = (int)ENUM_CallState.Active; // 此時算通話中                                                  
                sessionStr = ssmEvt.GetSessionInfoStr();
                callRef = (uint)ssmEvt.SessionInfo.nCallRef;
                _chLog!.Trace($"\t -> START RECORDING: staID={stationID}, ext={_extNo}, iprAnaChID={_iprAnaID}, iprRecChID={_iprRecID}, ursChID={_ursChID}, sessionInfo({sessionStr})");

                callRefObj = callRefMgr.GetCallRef(callRef);
                if (callRefObj != null) {
                    callRefObj.SessionStartTime = DateTime.Now;
                    _chLog!.Trace($"\t -> START RECORDING: callRefObj({callRef}) found.");
                }
                else { // 2021/08/08 Added, 萬一前面沒有 DE_CALL_CONNECTED 出現，就沒有 CallRefObj, 如果沒有 CallRefObj, 等等就無法結束錄音, 所以這裡要補
                    callRefObj = CreateCallRefObj(callRef);
                    _chLog!.Trace($"\t -> START RECORDING: callRefObj({callRef}) not found... create a new one!!!");
                    callRefObj.SessionStartTime = DateTime.Now;
                }

                #region 新的路錄音: 當下為 Idle 時才會開始錄音
                if (iprCh.RecordingState == (int)ENUM_RecordingState.Idle) {
                    StartRecording(ssmEvt);
                    iprCh.RecordingState = (int)ENUM_RecordingState.Actived;
                    GVar.IncRecordingCount();                    
                }
                #endregion
                #region 繼續錄音: 當下若是 pause, 則繼續錄音
                else if (iprCh.RecordingState == (int)ENUM_RecordingState.Pause) {
                    RestartRecording(ssmEvt);
                    iprCh.RecordingState = (int)ENUM_RecordingState.Actived;
                    //SignalR_Notify_RecState();
                }
                #endregion
                else {
                    _chLog!.Trace($"\t -> START RECORDING WARNING: Global.IPRChInfo[{_hwChID}].RecordingState is already recording(Actived)");
                }
                #endregion
                break;

            // IPR_ANA: 控制要停止錄音或暫停錄音。
            case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_STOPED:
            case (ushort)EventCode.E_RCV_IPR_AUX_MEDIA_SESSION_STOPED:
                #region  *** 收到 Session 停止的訊息 => 停止或暫停錄音                    
                sessionStr = ssmEvt.GetSessionInfoStr();
                _chLog!.Trace($"\t -> try STOP/PAUSE RECORDING: staID={stationID}, ext={_extNo}, iprAnaChID={_iprAnaID}, iprRecChID={_iprRecID}, sessionInfo={sessionStr}");

                // 停止錄音: 如果是 callState 是 idle，且錄音在(Active/Pause) => 則停止錄音
                if (iprCh.CallState == (int)ENUM_CallState.Idle && iprCh.RecordingState != (int)ENUM_RecordingState.Idle) {
                    _chLog!.Trace($"\t -> STOP RECORDING: staID={stationID}, ext={_extNo}");
                    StopRecording(ssmEvt);
                }
                else {
                    #region 判斷是否要 remove callRefObj, held 時要保留 
                    callRefObj = callRefMgr.GetCallRef((uint)ssmEvt.SessionInfo.nCallRef);
                    if (callRefObj != null) {
                        if (callRefObj.Status != ENUM_CallRefStatus.CallHeld) {
                            _chLog!.Trace($"\t -> callRef({callRefObj.CallRef}) is not held, removed!");
                            callRefMgr.RemoveCallRef(callRefObj.CallRef);
                        }
                        else {
                            callRefObj.RecStatus = ENUM_RecordingStatus.Pause;
                            _chLog!.Trace($"\t -> callRef({callRefObj.CallRef}) is held, keep it.");
                        }
                    }
                    #endregion

                    #region  2026/03/14 重改程式，不知為何升級要取消此功能? 以後再看看 ...
                    // 2022/09/28 配合升級到三匯驅動 5442，取消錄音站暫停
                    // 暫停錄音
                    //if (iprCh.RecordingState == (int)ENUM_RecordingState.Actived) {
                    //    _chLog!.Trace($"\t -> PAUSE RECORDING: staID={stationID}, ext={_ExtNo}");
                    //    if (lib_synway.PauseRecToFile(_IprRecChID) >= 0) {
                    //        _chLog!.Trace($"\t -> PAUSE RECORDING OK");
                    //    }
                    //    else {
                    //        _chLog!.Trace($"\t -> PAUSE RECORDING ERROR(PauseRecToFile failed): staID={stationID}, ext={_ExtNo}");
                    //    }
                    //    iprCh.RecordingState = (int)ENUM_RecordingState.Pause;
                    //    iprCh.RecPauseTime = DateTime.Now;
                    //}
                    //else {
                    //    _chLog!.Trace($"\t -> STOP/PAUSE RECORDING ERROR: staID={stationID}, ext={_ExtNo}, CallState/RecordingState mismatch.");
                    //}
                    #endregion
                }
                break;

            case (ushort)EventCode.E_IPR_ACTIVE_AND_REC_CB: // 在 rec channel(5 ~ 9) 中收到                    
                                                            // 此處原需呼叫 IPR_SendSession，但因改成 IPR_RecToFileA，故此 Event 已不會出現
                ret = ssmEvt.dwParam & 0xffff;
                slaverID = ssmEvt.dwParam >> 16;
                errMsg = ssmEvt.DecodeSlaverErrorMsg(ret);
                _chLog!.Trace($"\t -> E_IPR_ACTIVE_AND_REC_CB: {errMsg}(ret={ret}), slaverID={slaverID}");
                GVar.CTI!.ScanIprSlaver(out err);
                break;
            case (ushort)EventCode.E_IPR_DEACTIVE_AND_STOPREC_CB:
                // 原來的 code 是判斷 (pEvent->dwParam >> 16) == gIprSlaverAddr[SlaverIndex].nRecSlaverID),
                // 但覺得應該不用, 因為 Slaver 應該只有一個才對(0)，所以直接 Scan Slaver...
                ret = ssmEvt.dwParam & 0xffff;
                slaverID = ssmEvt.dwParam >> 16;
                var slaverMsg = ssmEvt.DecodeSlaverErrorMsg(ret);
                _chLog!.Trace($"\t -> E_IPR_DEACTIVE_AND_STOPREC_CB: {slaverMsg}(ret={ret}), slaverID={slaverID}");
                GVar.CTI!.ScanIprSlaver(out err);
                break;
            case (ushort)EventCode.E_IPR_LINK_REC_SLAVER_CONNECTED:
                _chLog!.Trace($"\t -> WARNING => E_IPR_LINK_REC_SLAVER_CONNECTED");
                GVar.CTI!.ScanIprSlaver(out err);
                break;
            case (ushort)EventCode.E_IPR_LINK_REC_SLAVER_DISCONNECTED:
                _chLog!.Trace($"\t -> *** WARNING *** => E_IPR_LINK_REC_SLAVER_DISCONNECTED");
                GVar.CTI!.ScanIprSlaver(out err);
                break;
            case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_FOWARDING:
            case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_FOWARD_STOPED:
                _chLog!.Trace($"\t -> sessionID={ssmEvt.dwParam}");
                break;
                #endregion
            #endregion

            default:
                break;
        }

        PrintIprDebug();
    }

    private void ProcessDChannelEvent(SsmEventDataModel ssmEvt) {
        var stationID = ssmEvt.stationID;
        var dEventName = ssmEvt.GetDEventCodeName();
        var callInfo = ssmEvt.CallInfo;
        _chLog!.Trace($">>> DChEvent=[{dEventName}], nRef={ssmEvt.nReference}, staID={stationID}, dwSubRsn={ssmEvt.dwSubReason}, " +
                            $"CallInfo(callRef={callInfo.CallRef}, callSrc={callInfo.CallSource}, caller={callInfo.szCallerId}, called={callInfo.szCalledId})");
        
        CallRefDataModel? callRefObj = null;
        var iprCh = GVar.CTI!.IPRChInfo[_hwChID];

        switch (ssmEvt.dwParam) {
            case (ushort)DEventCode.DE_REMOTE_PARTYID: // RemotePartyID
                #region DE_REMOTE_PARTYID 的處理說明
                //進線:
                //  1. EXT=87612981, callSrc=1, caller 不準(沒有@)，called = 自己，RemotePartyID = 對方
                //  2. EXT=87612981, callSrc=1, caller = 對方，called = 自己，RemotePartyID = 自己
                //外撥:
                //  caller = 自己, called = 不準(sip:0@...), RemotePartyID = 對方
                //結論: RemotePartyID 有點不一定，要判斷，看一下 StopRecording() 的寫法
                #endregion

                iprCh.RemotePartyID = ssmEvt.pvBuffer; //lib_ctiDecode.GetSsmEventpvBuffer(ssmEvent); 
                _chLog!.Trace($"\t -> ext={_extNo}, set IPRChInfo[{_hwChID}].RemotePartyID={iprCh.RemotePartyID}");                

                // 2021/07/26 added:
                // DE_REMOTE_PARTYID 發生時, DE_CALL_CONNECTED 還沒發生，而且此時的 CallRef 不能用
                // 所以，只能記錄在 ChCtrl.LastRemotePartyID, 之後在 DE_CALL_CONNECTED 發生時再取得
                //--------------------------------------------------------------------------------------
                ChCtrl.LastRemotePartyID = ssmEvt.pvBuffer;
                ChCtrl.RemotePartyIDSetTime = DateTime.Now;
                //--------------------------------------------------------------------------------------
                break;

            case (ushort)DEventCode.DE_DGT_PRS:
                //發現 Cisco 抓這個會不完整...所以這裡只顯示log，不放進 DTMF                    
                string c = (ssmEvt.dwSubReason & 0x000000ff).ToString();
                _chLog!.Trace($"\t -> get dtmf={c}");
                break;

            // SIP 沒有 DE_MSG_CHG ，不需要從 LCD 抓資訊                
            case (ushort)DEventCode.DE_MSG_CHG:
                break;

            case (ushort)DEventCode.DE_AUDIO_CHG:
                // 判斷 dwSubReason 無法解決"沒有 DE_RELEASE_BTN_PRS 的問題"
                // 因為當 Agent Login/Logout 並沒有此 Event 出現...
                break;

            //  當 DE_CALL_CONNECTED 出現時，直接在此處取 CallDir/DTMF/CallerID
            case (ushort)DEventCode.DE_CALL_CONNECTED:
            case (ushort)DEventCode.DE_OFFHOOK: // 這個Event Cisco 沒有，但暫時保留
                iprCh.CallState = (int)ENUM_CallState.Active;

                callRefObj = CreateCallRefObj(callInfo);
                _chLog!.Trace($"\t -> create a new callRefObj({callRefObj.CallRef}), callDir={callRefObj.CallDir}");
                break;

            // 響鈴
            case (ushort)DEventCode.DE_CALL_ALERTING: // for SIP
            case (ushort)DEventCode.DE_RING_ON:       // for AVAYA H.3232/Alcatel
                iprCh.RingFlag = 1; // 這是曾經響鈴，故如果 RingOff或onhook, 不需要變 0, 直到錄音結束
                iprCh.CallState = (int)ENUM_CallState.Ring;
                break;

            // 掛話筒(以下5種都是一樣的處理)
            case (ushort)DEventCode.DE_CALL_REJECTED:
            case (ushort)DEventCode.DE_CALL_ABANDONED:
            case (ushort)DEventCode.DE_CALL_RELEASED:
            case (ushort)DEventCode.DE_RELEASE_BTN_PRS:
            case (ushort)DEventCode.DE_ONHOOK:
                //GlobalVar.CTI!.IPRChInfo[_hwChID].CallState = (int)ENUM_CallState.Idle;                    
                StopRecording(ssmEvt);
                #region 原來是不管如何都設為 Idle，但要改為如下說明:
                // ------------------------------------------------------------------------------------------------------------
                // 因為 2021/09/25 發現，Cisco 的外線在 ConsultationCall, ConfCall, Held 時，都會先出現 Release，
                // 雖然這個 Release 不會造成停止錄音，但是會把 CallState 設為 Idle，當真正 Held 觸發
                // 而出現 E_RCV_IPR_MEDIA_SESSION_STOPED 時, 就會造成停止錄音，這樣就會錄成 2 通...，所以
                // 解法: 真的沒有任何的 call 時，才設為 Idle,
                #endregion
                if (callRefMgr.IsCallRefEmpty) {
                    iprCh.CallState = (int)ENUM_CallState.Idle;
                }
                break;

            // 要把 CallRefObj.Status 設為 Held
            case (ushort)DEventCode.DE_CALL_HELD:
                callRefObj = callRefMgr.GetCallRef(callInfo.CallRef);
                if (callRefObj != null) {
                    callRefObj.Status = ENUM_CallRefStatus.CallHeld;
                    callRefObj.LastHeldTime = DateTime.Now;
                }
                break;

            // 要把 CallRefObj.Status 設為 Connected
            case (ushort)DEventCode.DE_CALL_RETRIEVED:
                callRefObj = callRefMgr.GetCallRef(callInfo.CallRef);
                if (callRefObj != null) {
                    callRefObj.Status = ENUM_CallRefStatus.CallConnected;
                    callRefObj.LastHeldTime = null;
                }
                break;
            case (ushort)DEventCode.DE_RING_OFF:
                // 這一段有點奇怪，還要斟酌...
                if (iprCh.CallState == (int)ENUM_CallState.Ring) {
                    _chLog!.Trace($"\t -> staID={stationID}, ext={_extNo} last callState is CALL_STATE_RING, set callState=CALL_STATE_IDLE");
                    iprCh.CallState = (int)ENUM_CallState.Idle;
                    StopRecording(ssmEvt);
                }
                break;
            case (ushort)DEventCode.DE_SIP_RAW_MSG:
                break;
            default:
                break;
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
        var recFullFileName = GVar.URS!.GetRecFullFileName(recStartTime, _ursChID, out errMsg); // full path WAV filename
        if (!string.IsNullOrEmpty(errMsg)) {
            _chLog!.Trace($"\t -> START RECORDING ERROR: GetRecFullFileName failed({errMsg}");
            return false;
        }

        var iprCh = GVar.CTI!.IPRChInfo[_hwChID];

        ulong recID = Snowflake.NextId();
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
        _chLog!.Trace($"\t -> START RECORDING: recID={recID}, callRef={mktSessionInfo.nCallRef}, sessionId={mktSessionInfo.dwSessionId}, recFileName={recFullFileName}, guid={recGuid}");

        // 將 RecGuid 寫入 tblChannelStatus
        var recType = ENUM_RecordingType.Schedule;
        var lineStatus = ChCtrl.CallDir == ENUM_CallDir.Inbound ? ENUM_LineStatus.Inbound : ENUM_LineStatus.Outbound;
        var errHD = await _recDb.UpdateChannelStatus(_loggerSeq, _ursChID, lineStatus, recType, ChCtrl.CallerID, ChCtrl.DTMF, recGuid, recStartTime);
        if (errHD.Success)
            _chLog!.Trace($"\t -> START RECORDING: update channel status ok.(guid={recGuid})");
        else
            _chLog!.Trace($"\t -> START RECORDING: update channel status error.(err={errHD.UserMessage})");

        // 這裡本來要從 SharedMemory 抄 CTIEventLink => iprCh.RecControl.CTIEventLink = ???
        // 2020/09/25：CTIEventLink 先不做...  改變作法 => 還未決定...

        var retCode = 0;
        
        retCode = lib_synway.RecToFileA(_iprRecID, recFullFileName, (int)ENUM_ShCodec.CODEC_711, RTPCallBack);

        if (retCode == 0) { // 錄音成功                            
            _chLog!.Trace($"\t -> RecToFileA OK: recID={recID}, fwdPriPort={mktSessionInfo.nFowardingPPort}, fwdSndPort={mktSessionInfo.nFowardingSPort}");
            // 注意: IPRSendSession 的 nRef 是 IPR_ANA 的迴路ID
            ret = StartToSendSession(nRef, "127.0.0.1", GVar.CTI!.IPRChInfo[_iprRecID].PriRcvPort,
                                           "127.0.0.1", GVar.CTI!.IPRChInfo[_iprRecID].SecRcvPort);
        }
        else { // 錄音失敗
            ret = false;
            _chLog!.Trace($"\t -> RecToFileA ERROR: recID={recID}");
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

        _chLog!.Trace($"\t -> RE-START RECORDING: staID={stationID}, ext={_extNo}, iprRecChID={_iprRecID}");

        // *** 因為 Restart 錄音的時候，要重新指定 CallRef ***
        var newCallRef = ssmEvt.SessionInfo.nCallRef;
        _chLog!.Trace($"\t -> RE-START RECORDING: *** oldCallRef={iprCh.CallRef}, newCallRef={newCallRef} ***");
        iprCh.CallRef = newCallRef;

        //2021/07/28 added
        var callRefObj = callRefMgr.GetCallRef((uint)newCallRef);
        if (callRefObj != null)
            callRefObj.RecStatus = ENUM_RecordingStatus.Start;

        // Restart Recording ...
        if (lib_synway.RestartRecToFile(_iprRecID) >= 0) {
            _chLog!.Trace($"\t -> RestartRecToFile OK");
            var iprRecCh = GVar.CTI!.IPRChInfo[_iprRecID];
            ret = StartToSendSession(nRef, "127.0.0.1", iprRecCh.PriRcvPort, "127.0.0.1", iprRecCh.SecRcvPort);
        }
        else {
            ret = false;
            _chLog!.Trace($"\t -> RestartRecToFile Error");
        }
        return ret;
    }

    private bool StartToSendSession(int iprAnaChID, string priSlaverAddr, int priSlaverPort, string secSlaverAddr, int secSlaverPort) {
        var ret = true;
        var pri = $"{priSlaverAddr}:{priSlaverPort}";
        var sec = $"{secSlaverAddr}:{secSlaverPort}";
        if (lib_synway.IPRSendSession(iprAnaChID, priSlaverAddr, priSlaverPort, secSlaverAddr, secSlaverPort) >= 0) {
            _chLog!.Trace($"\t -> SEND SESSION OK: iprChID={iprAnaChID}, priAddrPort={pri}, sndAddrPort={sec}");
        }
        else {            
            _chLog!.Trace($"\t -> SEND SESSION ERROR: iprChID={iprAnaChID}, priAddrPort={pri}, sndAddrPort={sec}");
            ret = false;
        }
        return ret;
    }

    public void StopRecording(SsmEventDataModel ssmEvt) {
        var mktCallInfo = ssmEvt.CallInfo;
        var iprCh = GVar.CTI!.IPRChInfo[_hwChID];
        var stationID = iprCh.StationID;
        _chLog!.Trace($"\t -> try StopRecording ...staID={stationID}, ext={_extNo}, hwChID={_hwChID}, callRef={mktCallInfo.CallRef}, iprRecChID={_iprRecID}");

        // 1. 檢查目前這一個 event 的 callRef 是否事先存在，不存在則不能停止錄音，必須繼續錄音
        var callRefObj = callRefMgr.GetCallRef(mktCallInfo.CallRef);
        if (callRefObj == null) {
            _chLog!.Trace($"\t\t > CallRef({mktCallInfo.CallRef}) not found, ****** StopRecording() ignored! ******");
            return;
        }
        _chLog!.Trace($"\t\t > Current CallRef({mktCallInfo.CallRef}): CallDir={callRefObj.CallDir}, SessionStartTime={GetTimeStr(callRefObj.SessionStartTime)}, RecStatus={callRefObj.RecStatus}, CallerID={callRefObj.CallerID}, DTMF={callRefObj.DTMF}, status={callRefObj.Status}");
        _chLog!.Trace($"\t\t > ChCtrl: CallDir={ChCtrl.CallDir}, CallerID={ChCtrl.CallerID}, DTMF={ChCtrl.DTMF}");

        // 無論如何，要先把這個 callRef 移除
        callRefMgr.RemoveCallRef(mktCallInfo.CallRef);

        // 移除有問題的 CallRefObj
        var maxHoldTimeMin = 30; // <= TODO: 移到 appsettings
        callRefMgr.CleanupInvalidRefs(15, maxHoldTimeMin);
        //RemoveInvalidCallRefObj();

        // 2. 檢查是否還有其他的 callRef 存在(例如之前的held)，如果有，仍然不能停止錄音，必須繼續錄音
        if (callRefMgr.CallRefCount >= 1) { // 不包含自己，                
            _chLog!.Trace($"\t\t > Warning: total {callRefMgr.CallRefCount} CallRef is recording, *** should not stop recording ***");
            return;
        }

        DoStopRecording();

        //SignalR_Notify_RecState();
    }

    private void DoStopRecording() {
        var iprCh = GVar.CTI!.IPRChInfo[_hwChID];

        if (iprCh.RecordingState == (int)ENUM_RecordingState.Idle) {
            _chLog!.Trace($"\t\t > StopRecording ERROR: Global.IPRChInfo[{_hwChID}].RecordingState is RECORDING_IDLE");
            return;
        }
        _chLog!.Trace($"\t\t > DoStopRecording starting ...");
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
            _chLog!.Trace($"\t\t\t > StopRecording: inbound call, Get callerID={iprCh.CallerID}");
        else
            _chLog!.Trace($"\t\t\t > StopRecording: outbound call, Get DTMF={iprCh.DTMF}");
        //--------------------------------------------------------------------------------------
        #endregion

        // IPR_StopRecToFile                            
        if (lib_synway.StopRecToFile(_iprRecID, out string errMsg) == 0)
            _chLog!.Trace($"\t\t\t -> StopRecording: StopRecToFile(iprRecChID={_iprRecID}) ok.");
        else {
            _chLog!.Trace($"\t\t\t -> StopRecording: StopRecToFile(iprRecChID={_iprRecID}) error: {errMsg}");
        }

        // Update ChannelStatus
        var lineStatus = ENUM_LineStatus.Idle;
        var recType = ENUM_RecordingType.Idle;
        var errHD = _recDb.UpdateChannelStatus(_loggerSeq, _ursChID, lineStatus, recType, "", "").GetAwaiter().GetResult();
        if (errHD.Success)
            _chLog!.Trace($"\t\t\t -> StopRecording: update channel status ok");
        else
            _chLog!.Trace($"\t\t\t -> StopRecording: update channel status error.(err={errHD.UserMessage})");

        // 完成結束錄音            
        try {
            CompleteRecording(_hwChID);
        }
        catch (Exception ex) {
            _chLog!.Trace($"\t\t\t -> CompleteRecording() raise an exception: {ex.Message}");
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

        _chLog!.Trace($"\t -> CompleteRecording... recID={recID}, iprAnaChID={iprAnaID}, recStartTime={GVar.CTI!.IPRChInfo[iprAnaID].RecStartTime.ToString("yyyy/MM/dd HH:mm:ss")}");
        _chLog!.Trace($"\t\t > Prepare RecordingDataMode... recID={recID}");
        var model = MakeRecordingDataModel(iprAnaID);
        //SignalR_Notify_StopRecording(model);
        _chLog!.Trace($"\t\t > Enqueue RecordingDataMode... recID={model.RecID}");
        //Global.RecDataQueue.Enqueue(model);
        return true;
    }

    // 複製 Global.IPRChInfo[iprAnaChID] 裡面的欄位值 => RecordingDataModel
    private RecordingDataModel MakeRecordingDataModel(int iprAnaChID) {
        _chLog!.Trace($"\t > MakeRecordingDataModel...(ursChID={_ursChID}), ext={_extNo}");

        // 注意，是 iprAnaChID 的 IPR
        var iprAnaCh = GVar.CTI!.IPRChInfo[iprAnaChID];

        var recStartTime = iprAnaCh.RecStartTime;
        var recStopTime = DateTime.Now;

        var recLen = recStopTime > recStartTime ? (int)(recStopTime - iprAnaCh.RecStartTime).TotalSeconds : 0;        

        _chLog!.Trace($"\t\t > RecLen={recLen}, RecStartTime={GetDateTimeStr(recStartTime)}, RecStopTime={GetDateTimeStr(recStopTime)}");
        var model = new RecordingDataModel();
        model.LoggerSeq = GVar.URS!.LoggerConfig!.LoggerSeq;
        model.LoggerID = GVar.URS!.LoggerConfig!.LoggerID;
        model.LoggerName = GVar.URS!.LoggerConfig!.LoggerName!;
        model.RecID = (long)iprAnaCh.RecID;
        model.AgentID = GVar.URS!.GetUrsChAgentID(_extNo);

        // 注意: 此時錄音檔還沒被 rename, 實際仍是 wav file
        var wavFileName = iprAnaCh.RecFullFileName;
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

    private string GetTimeStr(DateTime? dt) {
        var ret = "";
        if (!dt.HasValue) {
            ret = "*";
        }
        else {
            ret = dt.Value.ToString("HH:mm:ss");
        }
        return ret;
    }

    private string GetDateTimeStr(DateTime? dt) {
        var ret = "";
        if (!dt.HasValue) {
            ret = "*";
        }
        else {
            ret = dt.Value.ToString("yyyy/MM/dd HH:mm:ss");
        }
        return ret;
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
        if (_chLog == null || !(GVar.Config?.URS?.PrintIprDebug ?? false) ) 
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
            // Enum.GetName 的舊版標準語法，或直接用 .ToString() 最快
            _chLog.Trace($"""
            	 *** IPRChInfo[{_hwChID}].Data => 
            RecID={iprCh.RecID}, 
            ChState={lib_ctiDecode.DecodeChStateStr(iprCh.ChState)}, 
            CallState={iprCh.CallState}, 
            RecordingState={iprCh.RecordingState}, 
            CallRef={iprCh.CallRef}, 
            StationID={iprCh.StationID}, 
            RecStartTime={GetDateTimeStr(iprCh.RecStartTime)}, 
            RecPauseTime={GetDateTimeStr(iprCh.RecPauseTime)}, 
            RemotePartyID={iprCh.RemotePartyID}, 
            DTMF={iprCh.DTMF}, 
            RingFlag={iprCh.RingFlag}, 
            RecFullFileName={iprCh.RecFullFileName}, 
            RecGuid={iprCh.RecGuid}
            """);

            // 2. ChCtrl 資訊
            _chLog.Trace($"\t *** ChCtrl.Data => lastRemotePartyID={ChCtrl.LastRemotePartyID}, setTime={GetTimeStr(ChCtrl.RemotePartyIDSetTime)}, CallDir={ChCtrl.CallDir}, CallerID={ChCtrl.CallerID}, DTMF={ChCtrl.DTMF}");

            // 3. .NET 8 替代 .Index() 的寫法
            // 使用 Select 同時取出 索引 與 物件
            var callRefSnapshot = callRefMgr.CallRefList.Select((cr, idx) => new { Index = idx + 1, Data = cr });

            foreach (var item in callRefSnapshot) {
                var cr = item.Data;
                _chLog.Trace($"\t @@@ {item.Index}. CallRef({cr.CallRef}): CallDir={cr.CallDir}, CreatedTime={GetTimeStr(cr.CreatedTime)}, SessionStartTime={GetTimeStr(cr.SessionStartTime)}, RecStatus={cr.RecStatus}, CallerID={cr.CallerID}, DTMF={cr.DTMF}, status={cr.Status}, LastHeldTime={GetTimeStr(cr.LastHeldTime)}");
            }

            _chLog.Trace(string.Empty);
        }
        catch (Exception ex) {
            _chLog.Error(ex, "PrintDebugInfo exception");
        }
    }

}