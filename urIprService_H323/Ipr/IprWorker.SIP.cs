using MyProject.ProjectCtrl;
using shpa3api;
using Synway.Models;
using urIprService.Models;
using urSynIpr.lib;

namespace IprService.Ipr {
    public partial class IprWorker {        

        public void ProcessData_SIP(SsmEventDataModel ssmEvt) {
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

            _chLog!.Info($"[{_chType}][{eventName,-32}], nRef={nRef}, staID={stationID}, hwChID={_hwChID}, ursChID={_ursChID}, extNo={_extNo}, RawData=[{ssmEvt.RawData}]");
            switch (ssmEvt.wEventCode) {
                #region IPR_REC(後面的 port) 收到的 Event
                // 由 REC Channel 丟出此訊息 
                case (ushort)EventCode.E_CHG_ChState:
                    var chState = ssmEvt.GetChStateStr();
                    _chLog!.Info($"\t -> IPRChInfo[{_hwChID}].ChState: {chState}");
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
                        _chLog!.Info($"\t -> DTMF={(char)DTMF}");
                        iprAnaCh.DTMF = iprAnaCh.DTMF + (char)DTMF;
                        _chLog!.Info($"\t -> ext={_extNo}, IPRChInfo[{_iprAnaID}].DTMF={iprAnaCh.DTMF}");
                    }
                    break;
                // 由 REC Channel 丟出此訊息 
                case (ushort)EventCode.E_PROC_RecordEnd:
                    // 最後: 這裡不再處裡關於結束錄音的相關工作，全部放在 StopRecording() 中
                    var reason = lib_ctiDecode.DecodeRecordEndReason(ssmEvt.dwParam);
                    _chLog!.Info($"\t -> E_PROC_RecordEnd: nRef={nRef}, reason={reason}, staID={stationID}, ext={_extNo}, iprAnaChID={_iprAnaID}, iprRecChID={_iprRecID}, ursChID={_ursChID}");
                    //
                    sessionStr = ssmEvt.GetSessionInfoStr();
                    callRef = (uint)ssmEvt.SessionInfo.nCallRef;
                    _chLog!.Info($"\t -> E_PROC_RecordEnd: callRef={callRef}, sessionInfo({sessionStr})");
                    break;

                // 由 REC Channel 丟出此訊息 
                case (ushort)EventCode.E_IPR_STOP_REC_CB:
                    _chLog!.Info($"\t -> E_IPR_STOP_REC_CB");
                    break;

                // 由 REC Channel 丟出此訊息 
                case (ushort)EventCode.E_IPR_START_REC_CB:
                    _chLog!.Info($"\t -> E_IPR_START_REC_CB");
                    break;

                // 由 REC Channel 丟出此訊息 
                case (ushort)EventCode.E_IPR_ACTIVE_SESSION_CB:
                    _chLog!.Info($"\t -> E_IPR_ACTIVE_SESSION_CB");
                    break;
                #endregion

                #region IPR_ANA(前面的 port) 收到的 Event

                case (ushort)EventCode.E_RCV_IPR_DChannel:
                    ProcessDChannelEvent_Sip(ssmEvt); // <= 統一處理有關 D-Channel 的 Event
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
                    _chLog!.Info($"\t -> station added ok: staID[{stationID}], ext={_extNo}, iprAnaChID={_iprAnaID}, iprRecChID={_iprRecID}");
                    break;
                case (ushort)EventCode.E_RCV_IPR_STATION_REMOVED:
                    _chLog!.Info($"\t -> WARNING: REMOVE STATION(staID={stationID}");
                    break;

                case (ushort)EventCode.E_RCV_IPR_AUTH_OVERFLOW:
                    _chLog!.Info($"\t -> WARNING: IPR-AUTH OVERFLOW ...");
                    break;

                case (ushort)EventCode.E_IPR_SLAVER_INIT_CB:
                case (ushort)EventCode.E_IPR_START_SLAVER_CB:
                case (ushort)EventCode.E_IPR_CLOSE_SLAVER_CB:
                    _chLog!.Info($"\t -> slaverID={ssmEvt.dwParam >> 16}, CB_Rreturn={ssmEvt.dwParam & 0xffff}");
                    GVar.CTI!.ScanIprSlaver(out string err);
                    var slaverInfo = lib_ctiDecode.GetSlaverInfoStr(GVar.CTI!.HwInfo.IPRSlaverAddr);
                    _chLog!.Info($"\t -> {slaverInfo}");
                    break;

                case (ushort)EventCode.E_IPR_PAUSE_REC_CB:
                case (ushort)EventCode.E_IPR_RESTART_REC_CB:
                    _chLog!.Info($"\t -> slaverID={ssmEvt.dwParam >> 16}, CB_Rreturn={ssmEvt.dwParam & 0xffff}");
                    break;

                case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_STARTED:
                case (ushort)EventCode.E_RCV_IPR_AUX_MEDIA_SESSION_STARTED:
                    #region *** IPR_ANA: 開始錄音
                    iprCh.CallState = (int)ENUM_CallState.Active; // 此時算通話中                                                  
                    sessionStr = ssmEvt.GetSessionInfoStr();
                    callRef = (uint)ssmEvt.SessionInfo.nCallRef;
                    _chLog!.Info($"\t -> START RECORDING: staID={stationID}, ext={_extNo}, iprAnaChID={_iprAnaID}, iprRecChID={_iprRecID}, ursChID={_ursChID}, sessionInfo({sessionStr})");

                    callRefObj = callRefMgr.GetCallRef(callRef);
                    if (callRefObj != null) {
                        callRefObj.SessionStartTime = DateTime.Now;
                        _chLog!.Info($"\t -> START RECORDING: callRefObj({callRef}) found.");
                    }
                    else { // 2021/08/08 Added, 萬一前面沒有 DE_CALL_CONNECTED 出現，就沒有 CallRefObj, 如果沒有 CallRefObj, 等等就無法結束錄音, 所以這裡要補
                        callRefObj = CreateCallRefObj(callRef);
                        _chLog!.Info($"\t -> START RECORDING: callRefObj({callRef}) not found... create a new one!!!");
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
                        _chLog!.Info($"\t -> START RECORDING WARNING: Global.IPRChInfo[{_hwChID}].RecordingState is already recording(Actived)");
                    }
                    #endregion
                    break;

                // IPR_ANA: 控制要停止錄音或暫停錄音。
                case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_STOPED:
                case (ushort)EventCode.E_RCV_IPR_AUX_MEDIA_SESSION_STOPED:
                    #region  *** 收到 Session 停止的訊息 => 停止或暫停錄音                    
                    sessionStr = ssmEvt.GetSessionInfoStr();
                    _chLog!.Info($"\t -> try STOP/PAUSE RECORDING: staID={stationID}, ext={_extNo}, iprAnaChID={_iprAnaID}, iprRecChID={_iprRecID}, sessionInfo={sessionStr}");

                    // 停止錄音: 如果是 callState 是 idle，且錄音在(Active/Pause) => 則停止錄音
                    if (iprCh.CallState == (int)ENUM_CallState.Idle && iprCh.RecordingState != (int)ENUM_RecordingState.Idle) {
                        _chLog!.Info($"\t -> STOP RECORDING: staID={stationID}, ext={_extNo}");
                        StopRecording(ssmEvt.CallInfo.CallRef);
                    }
                    else {
                        #region 判斷是否要 remove callRefObj, held 時要保留 
                        callRefObj = callRefMgr.GetCallRef((uint)ssmEvt.SessionInfo.nCallRef);
                        if (callRefObj != null) {
                            if (callRefObj.Status != ENUM_CallRefStatus.CallHeld) {
                                _chLog!.Info($"\t -> callRef({callRefObj.CallRef}) is not held, removed!");
                                callRefMgr.RemoveCallRef(callRefObj.CallRef);
                            }
                            else {
                                callRefObj.RecStatus = ENUM_RecordingStatus.Pause;
                                _chLog!.Info($"\t -> callRef({callRefObj.CallRef}) is held, keep it.");
                            }
                        }
                        #endregion

                        #region  2026/03/14 重改程式，不知為何升級要取消此功能? 以後再看看 ...
                        // 2022/09/28 配合升級到三匯驅動 5442，取消錄音站暫停
                        // 暫停錄音
                        //if (iprCh.RecordingState == (int)ENUM_RecordingState.Actived) {
                        //    _chLog!.Info($"\t -> PAUSE RECORDING: staID={stationID}, ext={_ExtNo}");
                        //    if (lib_synway.PauseRecToFile(_IprRecChID) >= 0) {
                        //        _chLog!.Info($"\t -> PAUSE RECORDING OK");
                        //    }
                        //    else {
                        //        _chLog!.Info($"\t -> PAUSE RECORDING ERROR(PauseRecToFile failed): staID={stationID}, ext={_ExtNo}");
                        //    }
                        //    iprCh.RecordingState = (int)ENUM_RecordingState.Pause;
                        //    iprCh.RecPauseTime = DateTime.Now;
                        //}
                        //else {
                        //    _chLog!.Info($"\t -> STOP/PAUSE RECORDING ERROR: staID={stationID}, ext={_ExtNo}, CallState/RecordingState mismatch.");
                        //}
                        #endregion
                    }
                    break;

                case (ushort)EventCode.E_IPR_ACTIVE_AND_REC_CB: // 在 rec channel(5 ~ 9) 中收到                    
                                                                // 此處原需呼叫 IPR_SendSession，但因改成 IPR_RecToFileA，故此 Event 已不會出現
                    ret = ssmEvt.dwParam & 0xffff;
                    slaverID = ssmEvt.dwParam >> 16;
                    errMsg = ssmEvt.DecodeSlaverErrorMsg(ret);
                    _chLog!.Info($"\t -> E_IPR_ACTIVE_AND_REC_CB: {errMsg}(ret={ret}), slaverID={slaverID}");
                    GVar.CTI!.ScanIprSlaver(out err);
                    break;
                case (ushort)EventCode.E_IPR_DEACTIVE_AND_STOPREC_CB:
                    // 原來的 code 是判斷 (pEvent->dwParam >> 16) == gIprSlaverAddr[SlaverIndex].nRecSlaverID),
                    // 但覺得應該不用, 因為 Slaver 應該只有一個才對(0)，所以直接 Scan Slaver...
                    ret = ssmEvt.dwParam & 0xffff;
                    slaverID = ssmEvt.dwParam >> 16;
                    var slaverMsg = ssmEvt.DecodeSlaverErrorMsg(ret);
                    _chLog!.Info($"\t -> E_IPR_DEACTIVE_AND_STOPREC_CB: {slaverMsg}(ret={ret}), slaverID={slaverID}");
                    GVar.CTI!.ScanIprSlaver(out err);
                    break;
                case (ushort)EventCode.E_IPR_LINK_REC_SLAVER_CONNECTED:
                    _chLog!.Info($"\t -> WARNING => E_IPR_LINK_REC_SLAVER_CONNECTED");
                    GVar.CTI!.ScanIprSlaver(out err);
                    break;
                case (ushort)EventCode.E_IPR_LINK_REC_SLAVER_DISCONNECTED:
                    _chLog!.Info($"\t -> *** WARNING *** => E_IPR_LINK_REC_SLAVER_DISCONNECTED");
                    GVar.CTI!.ScanIprSlaver(out err);
                    break;
                case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_FOWARDING:
                case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_FOWARD_STOPED:
                    _chLog!.Info($"\t -> sessionID={ssmEvt.dwParam}");
                    break;
                    #endregion
                #endregion

                default:
                    break;
            }

            PrintIprDebug();
        }

        private void ProcessDChannelEvent_Sip(SsmEventDataModel ssmEvt) {
            var stationID = ssmEvt.stationID;
            var dEventName = ssmEvt.GetDEventCodeName();
            var callInfo = ssmEvt.CallInfo;
            _chLog!.Info($">>> DChEvent=[{dEventName}], nRef={ssmEvt.nReference}, staID={stationID}, dwSubRsn={ssmEvt.dwSubReason}, " +
                                $"CallInfo(callRef={callInfo.CallRef}, callSrc={callInfo.CallSource}, caller={callInfo.szCallerId}, called={callInfo.szCalledId}, pvBuf={ssmEvt.pvBuffer}");

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
                    _chLog!.Info($"\t -> ext={_extNo}, set IPRChInfo[{_hwChID}].RemotePartyID={iprCh.RemotePartyID}");

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
                    _chLog!.Info($"\t -> get dtmf={c}");
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
                    _chLog!.Info($"\t -> create a new callRefObj({callRefObj.CallRef}), callDir={callRefObj.CallDir}");
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
                    StopRecording(ssmEvt.CallInfo.CallRef);
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
                        _chLog!.Info($"\t -> staID={stationID}, ext={_extNo} last callState is CALL_STATE_RING, set callState=CALL_STATE_IDLE");
                        iprCh.CallState = (int)ENUM_CallState.Idle;
                        StopRecording(ssmEvt.CallInfo.CallRef);
                    }
                    break;
                case (ushort)DEventCode.DE_SIP_RAW_MSG:
                    break;
                default:
                    break;
            }
        }

    }
}
