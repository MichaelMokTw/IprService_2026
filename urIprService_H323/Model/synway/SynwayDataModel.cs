using Newtonsoft.Json;
using shpa3api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using urSynIpr.lib;

namespace Synway.Models {

    public class CallInfoDataModel {
        public uint CallRef { set; get; } = 0;           //call reference
        public uint CallSource { set; get; } = 0;        //call direction(incoming or outgoing)
        public uint Cause { set; get; } = 0;             //cause        
        public string szCallerId { set; get; } = string.Empty;      //caller number or name        
        public string szCalledId { set; get; } = string.Empty;     //called number or name
        public string szReferredBy { set; get; } = string.Empty;    //from which referred        
        public string szReferTo { set; get; } = string.Empty;       //refer to which
        public string RawData { set; get; } = string.Empty;        // Json string for this data        
    }

    public class SessionInfoDataModel {
        public int nCallRef { set; get; }			//call reference
        public int nStationId { set; get; }			//one of the station of the session
        public int nStationId2 { set; get; }		//another station of the session
        public uint dwSessionId { set; get; }       //session Id
        public IPR_Addr PrimaryAddr { set; get; }	//ip address and port of primary
        public int nPrimaryCodec { set; get; }      //codec of primary
        public IPR_Addr SecondaryAddr { set; get; }	//ip address and port of secondary
        public int nSecondaryCodec { set; get; }	//codec of secondary        
        public string szpFowardingIp { set; get; }	//forwarding ip for forward primary, null if no forwarding        
        public string szsFowardingIp { set; get; }	//forwarding ip for forward secondary, null if no forwarding
        public int nFowardingPPort { set; get; }	//forwarding port for forward primary, -1 if no forwarding
        public int nFowardingSPort { set; get; }	//forwarding port for forard secondary, -1 if no forwarding
        public string RawData { set; get; }         // Json string for this data
        public SessionInfoDataModel() {
            nCallRef = 0;
            nStationId = 0;
            nStationId2 = 0;
            dwSessionId = 0;
            PrimaryAddr = new IPR_Addr();
            nPrimaryCodec = 0;
            SecondaryAddr = new IPR_Addr();
            nSecondaryCodec = 0;
            szpFowardingIp = "";
            szsFowardingIp = "";
            nFowardingPPort = 0;
            nFowardingSPort = 0;
        }
    };

    public class SsmEventDataModel {
        public ushort wEventCode { set; get; }
        public int nReference { set; get; }
        public uint dwParam { set; get; }
        public uint dwUser { set; get; }
        public uint dwSubReason { set; get; }
        public uint dwXtraInfo { set; get; }

        public string pvBuffer { set; get; } // 原來是 public IntPtr pvBuffer, 這裡故意改 string

        public uint dwBufferLength { set; get; }
        public uint dwDataLength { set; get; }
        public uint dwEventFlag { set; get; }
        #region Falgs of the following:        
        //bit 0,    =1 - App created the event
        //          =0 - SHP_A3.DLL created the event
        //bit 1,    Reserved
        //bit 2,    =1 - data has been truncated
        //          =0 - data has not been truncated
        #endregion
        public uint dwReserved1 { set; get; }
        public long llReserved1 { set; get; }
        public string eventName { set; get; }
        public uint stationID { set; get; }
        public uint protoType { set; get; }
        
        public CallInfoDataModel CallInfo { set; get; }
        public SessionInfoDataModel SessionInfo { set; get; }
        public string RawData { set; get; }         // Json string for this data

        public SsmEventDataModel(SSM_EVENT ssmEvent) {
            wEventCode = ssmEvent.wEventCode;
            nReference = ssmEvent.nReference;
            dwParam = ssmEvent.dwParam;
            dwUser = ssmEvent.dwUser;
            dwSubReason = ssmEvent.dwSubReason;
            dwXtraInfo = ssmEvent.dwXtraInfo;
            pvBuffer = GetPvBuffer(ssmEvent);
            dwBufferLength = ssmEvent.dwBufferLength;
            dwDataLength = ssmEvent.dwDataLength;
            dwEventFlag = ssmEvent.dwEventFlag;
            dwReserved1 = ssmEvent.dwReserved1;
            llReserved1 = ssmEvent.llReserved1;

            eventName = DecodeEventName();
            stationID = dwXtraInfo & 0xffff;
            protoType = dwXtraInfo >> 16;

            RawData = $"eventCode={wEventCode}, nRef={nReference}, dwParam={dwParam}";

            CallInfo = GetCallInfo(ssmEvent);
            SessionInfo = GetSessionInfo(ssmEvent);
        }

        public string DecodeEventName() {
            if (Enum.IsDefined(typeof(EventCode), (int)wEventCode)) {
                return Enum.GetName(typeof(EventCode), (int)wEventCode);
            }
            else {
                return $@"???(0x{wEventCode:X4})";
            }
        }

        public string GetDEventCodeName() {
            if (Enum.IsDefined(typeof(DEventCode), (int)dwParam)) {
                return Enum.GetName(typeof(DEventCode), (int)dwParam);
            }
            else {
                return $@"???(0x{dwParam:X4})";
            }
        }

        public string GetIPAddress(IPR_Addr iprAddr) {
            var ipAddr = "";
            var funcName = "GetIPAddress";
            try {
                ipAddr = $"{iprAddr.s_ip.addr.S_un_b.s_b1}.{iprAddr.s_ip.addr.S_un_b.s_b2}.{iprAddr.s_ip.addr.S_un_b.s_b3}.{iprAddr.s_ip.addr.S_un_b.s_b4}:{iprAddr.s_ip.usPort}";
            }
            catch (Exception ex) {
                ipAddr = $"{funcName} Exception: {ex.Message}";                
            }
            return ipAddr;
        }

        public string DecodeSlaverErrorMsg(uint errorCode) {
            var ret = "";
            switch (errorCode) {
                case 0: ret = "OK"; break;
                case 1: ret = "未知"; break;
                case 2: ret = "超時"; break;
                case 3: ret = "報文數據異常"; break;
                case 4: ret = "該SessionID已處於活動中"; break;
                case 5: ret = "資源列表中找不到該SessioID"; break;
                case 6: ret = "創建新的Session失敗"; break;
                case 7: ret = "文件創建失敗"; break;
                case 8: ret = "找不到活動的SessionId"; break;
                case 9: ret = "該SessionID已處於錄音中"; break;
                case 10: ret = "該SessionID已處於錄音停止狀態"; break;
                case 11: ret = "該SessionID未處於錄音狀態"; break;
                case 12: ret = "該SessionID未處於錄音到文件的狀態"; break;
                case 13: ret = "該SessionID未處於錄音暫停狀態"; break;
                case 14: ret = "要求錄音的數據長度不足"; break;
                case 15: ret = "初始WAV文件頭失敗"; break;
                case 16: ret = "Slaver已被分配資源並開啟"; break;
                case 17: ret = "申請資源過程失敗"; break;
                case 18: ret = "該Slaver從未開啟"; break;
                default: ret = "Unknow Error"; break;
            }
            return ret;
        }

        public string DecodeIprProtocolStr() {
            var ret = "";
            switch (protoType) {
                case 0: ret = "PTL_SIP"; break;
                case 1: ret = "PTL_CISCO_SKINNY"; break;
                case 2: ret = "PTL_AVAYA_H323"; break;
                case 3: ret = "PTL_SHORTEL_MGCP"; break;
                case 4: ret = "PTL_H323"; break;
                case 5: ret = "PTL_PANASONIC_MGCP"; break;
                case 6: ret = "PTL_TOSHIBA_MEGACO"; break;
                case 7: ret = "PTL_SIEMENS_H323"; break;
                case 8: ret = "PTL_ALCATEL"; break;
                case 9: ret = "PTL_MITEL"; break;
                default: ret = "UNKNOW_PROTOCOL"; break;
            }
            return ret;
        }

        public string GetSessionInfoStr() {
            var priIpAddr = GetIPAddress(SessionInfo.PrimaryAddr);
            var sndIpAddr = GetIPAddress(SessionInfo.SecondaryAddr);
            return $"sessionID={SessionInfo.dwSessionId}, callRef={SessionInfo.nCallRef}, " +
                    $"priIp={priIpAddr}, sndIp={sndIpAddr}, staId1={SessionInfo.nStationId}, staId2={SessionInfo.nStationId2}";
        }

        //// 此處假設只有一個 Slaver 
        //public static string GetSlaverInfoStr(IPR_SLAVERADDR slaverAddr) {
        //    var ipAddr = GetIPAddress(slaverAddr.ipAddr);
        //    return $"Slaver: ID={slaverAddr.nRecSlaverID}, ip={ipAddr}, thdPairs={slaverAddr.nThreadPairs}, " +
        //                $"totalResource={slaverAddr.nTotalResources}, usedResource={slaverAddr.nUsedResources}";
        //}


        public string DecodeRecordEndReason(uint dwParam) {
            var ret = "";
            switch (dwParam) {
                case 1: ret = "stopRecByApp"; break;            // 1: Terminated by the application program
                case 2: ret = "stopRecByDTMF"; break;           // 2: Terminates upon detecting DTMF digits
                case 3: ret = "stopRecByPeerHangup"; break;     // 3: Terminates upon detecting the remote client’s hangup behavior
                case 4: ret = "stopRecByhMaxTime"; break;       // 4: Terminates when the recorded data reach a specified length or the recording operation lasts for a specified time.
                case 5: ret = "stopRecByRecPaused"; break;      // 5: The task of file recording paused
                case 6: ret = "stopRecByWriteDataError"; break; // 6: Writing recorded data to files failed
                case 7: ret = "stopRecByRTPTimeout"; break;     // 7: RTP timeout
                case 8:
                    ret = "stopRecByNewPayloadUnsupported"; // 8: The RTP payload format is changed in the session, and the new payload format is unsupported
                    break;
                default: ret = $"???({dwParam})"; break;
            }
            return ret;
        }

        public string DecodeChState(int chState) {
            if (Enum.IsDefined(typeof(ChState), (int)chState)) {
                return Enum.GetName(typeof(ChState), (int)chState);
            }
            else {
                return $"???({chState})";
            }
        }

        public string GetChStateStr() {
            uint hi = 0;    // 之前的迴路狀態
            uint low = 0;   // 目前的迴路狀態
            hi = dwParam & 0xffff0000;
            hi = hi >> 16;
            var str1 = DecodeChState((int)hi);
            //
            low = dwParam & 0x0000ffff;
            var str2 = DecodeChState((int)low);
            //            
            return $"{str1}=>{str2}";
        }

        public string GetEventParamStr() {
            string ret = "";
            string str = "";
            uint slaverID = 0;
            uint retCode = 0;
            
            try {
                switch (wEventCode) {
                    case (ushort)EventCode.E_CHG_ChState:
                        var chState = GetChStateStr();
                        ret = $"nRef={nReference}, staID={stationID, -4}, {chState}";
                        break;
                    case (ushort)EventCode.E_IPR_LINK_REC_SLAVER_CONNECTED:
                        slaverID = dwParam >> 16;
                        ret = $"nRef={nReference}, staID={stationID, -4}, SlaverID = {slaverID}";
                        break;
                    case (ushort)EventCode.E_IPR_SLAVER_INIT_CB:
                        slaverID = dwParam >> 16;
                        retCode = dwParam & 0xffff;
                        str = DecodeSlaverErrorMsg(retCode);
                        ret = $"nRef={nReference}, staID={stationID, -4}, SlaverID = {slaverID}, retCode={retCode}({str})";
                        break;
                    case (ushort)EventCode.E_RCV_IPR_DChannel:                        
                        //var protoName = DecodeIprProtocolStr();
                        var dchName = GetDEventCodeName(); 
                        //
                        ret = $"nRef={nReference}, staID={stationID, -4}, DCh={dchName,-24}";
                        //
                        if (dwParam == (uint)DEventCode.DE_CISCO_SCCP_CALL_INFO) {
                            // 以後碰到 CISCO 再補
                        }
                        else if ((dwParam >= (uint)DEventCode.DE_CALL_IN_PROGRESS) && (dwParam <= (uint)DEventCode.DE_CALL_REJECTED)) {                            
                            ret = ret + $"callRef={CallInfo.CallRef}, callSrc={CallInfo.CallSource}, cause={CallInfo.Cause}, " +
                                        $"callerID={CallInfo.szCallerId}, calledID={CallInfo.szCalledId}, " +
                                        $"transFrom={CallInfo.szReferredBy}, transTo={CallInfo.szReferTo}";
                        }
                        else if (dwParam == (uint)DEventCode.DE_SIP_RAW_MSG) {
                            ret = ret + $"callRef={CallInfo.CallRef}, callSrc={CallInfo.CallSource}, cause={CallInfo.Cause}, " +
                                        $"callerID={CallInfo.szCallerId}, calledID={CallInfo.szCalledId}, " +
                                        $"transFrom={CallInfo.szReferredBy}, transTo={CallInfo.szReferTo}";
                        }
                        else if (dwParam == (uint)DEventCode.DE_REMOTE_PARTYID) {
                            ret = ret + $"RemotePartyID={pvBuffer}";
                        }
                        else if (dwParam == (uint)DEventCode.DE_MSG_CHG) {
                            ret = ret + $", LCD={pvBuffer}";
                        }
                        else if (dwParam == (uint)DEventCode.DE_AUDIO_CHG) {
                            ret = ret + $", dwSubReason={dwSubReason}";
                            if ((dwSubReason & 0x0001) == 0x0001)
                                ret = ret + $", HD-mic(1)";
                            else
                                ret = ret + $", HD-mic(0)";
                            if ((dwSubReason & 0x0002) == 0x0002)
                                ret = ret + $", HD-spk(1)";
                            else
                                ret = ret + $", HD-spk(0)";
                            if ((dwSubReason & 0x0004) == 0x0004)
                                ret = ret + $", SP-mic(1)";
                            else
                                ret = ret + $", SP-mic(0)";
                            if ((dwSubReason & 0x0008) == 0x0008)
                                ret = ret + $", SP-spk(1)";
                            else
                                ret = ret + $", SP-spk(0)";
                        }

                        else if (dwParam == (uint)DEventCode.DE_DGT_PRS) {
                            var hi = (char)dwSubReason & 0x00ff;
                            ret = ret + $"dtmf={hi}";
                        }
                        break;
                    case (ushort)EventCode.E_RCV_IPR_STATION_ADDED:                        
                        ret = ret + $"nRef={nReference}, staID={stationID, -4}";
                        break;
                    case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_STARTED:
                    case (ushort)EventCode.E_RCV_IPR_AUX_MEDIA_SESSION_STARTED:
                    case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_STOPED:
                    case (ushort)EventCode.E_RCV_IPR_AUX_MEDIA_SESSION_STOPED:                        
                        var str1 = GetIPAddress(SessionInfo.PrimaryAddr);
                        var str2 = GetIPAddress(SessionInfo.SecondaryAddr);
                        ret = ret + $"nRef={nReference}, staID={stationID, -4}, callRef={SessionInfo.nCallRef}, sessionID={SessionInfo.dwSessionId}, " +
                                    $"stationID1={SessionInfo.nStationId}, stationID2={SessionInfo.nStationId2}, " +
                                    $"primary(ip={str1}, codec={SessionInfo.nPrimaryCodec}), " +
                                    $"secondary(ip={str2}, codec={SessionInfo.nSecondaryCodec})";
                        break;
                    case (ushort)EventCode.E_IPR_ACTIVE_SESSION_CB:
                    case (ushort)EventCode.E_IPR_DEACTIVE_SESSION_CB:
                    case (ushort)EventCode.E_IPR_START_REC_CB:
                    case (ushort)EventCode.E_IPR_STOP_REC_CB:
                    case (ushort)EventCode.E_IPR_PAUSE_REC_CB:
                    case (ushort)EventCode.E_IPR_RESTART_REC_CB:
                    case (ushort)EventCode.E_IPR_START_SLAVER_CB:
                    case (ushort)EventCode.E_IPR_CLOSE_SLAVER_CB:
                    case (ushort)EventCode.E_IPR_ACTIVE_AND_REC_CB:
                    case (ushort)EventCode.E_IPR_DEACTIVE_AND_STOPREC_CB:
                        slaverID = dwParam >> 16;
                        retCode = dwParam & 0xffff;
                        str = DecodeSlaverErrorMsg(retCode);
                        ret = ret + $"nRef={nReference}, staID={stationID, -4}, SlaverID={slaverID}, retCode={retCode}({str})";
                        break;
                    case (ushort)EventCode.E_PROC_RecordEnd:
                        ret = $"nRef={nReference}, staID={stationID, -4}, {DecodeRecordEndReason(dwParam)}";                        
                        break;
                    case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_FOWARDING:
                    case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_FOWARD_STOPED:
                        ret = ret + $"nRef={nReference}, staID={stationID, -4}";
                        break;
                    case (ushort)EventCode.E_RCV_IPR_DONGLE_ADDED:
                        ret = ret + $"nRef={nReference}, staID={stationID, -4}";
                        break;
                    case (ushort)EventCode.E_IPR_RCV_DTMF:
                        var dtmf = (char)dwParam; // 直接就是 dtmf 的十進位，跟三匯文件不一樣
                        ret = ret + $"nRef={nReference}, staID={stationID, -4}, dwParam={dwParam}, dtmf={dtmf}";
                        break;


                    default:
                        ret = ret + $"???(eventCode=0x{wEventCode:X4})";
                        break;
                } // switch
            }
            catch (Exception ex) {
                ret = $"SsmEventDataModel.DecodeEventParam exception: {ex.Message}";                
            }
            return ret;
        }

        private string GetPvBuffer(SSM_EVENT ssmEvent) {
            var s = "";
            if (ssmEvent.pvBuffer != IntPtr.Zero) {
                s = Marshal.PtrToStringAnsi(ssmEvent.pvBuffer);
            }
            return s;
        }

        private IPR_SessionInfo GetIPRSessionInfo(SSM_EVENT ssmEvent) {
            IPR_SessionInfo info = new IPR_SessionInfo();
            if (ssmEvent.pvBuffer != IntPtr.Zero) {
                info = (IPR_SessionInfo)Marshal.PtrToStructure(ssmEvent.pvBuffer, typeof(IPR_SessionInfo));
            }
            return info;
        }

        private string CharToString(char[] chars) {
            var str = "";
            if (chars != null) {
                var utf8Bytes = Encoding.UTF8.GetBytes(chars);
                str = Encoding.UTF8.GetString(utf8Bytes);
            }
            return str.Trim('\0');
        }

        private SessionInfoDataModel GetSessionInfo(SSM_EVENT ssmEvent) {
            var model = new SessionInfoDataModel();

            var iprSessionInfo = GetIPRSessionInfo(ssmEvent);

            model.nCallRef = iprSessionInfo.nCallRef;
            model.nStationId = iprSessionInfo.nStationId;
            model.nStationId2 = iprSessionInfo.nStationId2;
            model.dwSessionId = iprSessionInfo.dwSessionId;
            model.PrimaryAddr = iprSessionInfo.PrimaryAddr;
            model.nPrimaryCodec = iprSessionInfo.nPrimaryCodec;
            model.SecondaryAddr = iprSessionInfo.SecondaryAddr;
            model.nSecondaryCodec = iprSessionInfo.nSecondaryCodec;
            model.szpFowardingIp = CharToString(iprSessionInfo.szpFowardingIp);
            model.szsFowardingIp = CharToString(iprSessionInfo.szsFowardingIp);
            model.nFowardingPPort = iprSessionInfo.nFowardingPPort;
            model.nFowardingSPort = iprSessionInfo.nFowardingSPort;

            model.RawData = JsonConvert.SerializeObject( model);

            return model;
        }

        private IPR_CALL_INFO GetIPRCallInfo(SSM_EVENT ssmEvent) {
            IPR_CALL_INFO iprCallInfo = new IPR_CALL_INFO();
            if (ssmEvent.pvBuffer != IntPtr.Zero) {
                iprCallInfo = (IPR_CALL_INFO)Marshal.PtrToStructure(ssmEvent.pvBuffer, typeof(IPR_CALL_INFO));
            }
            return iprCallInfo;
        }

        private CallInfoDataModel GetCallInfo(SSM_EVENT ssmEvent) {
            var model = new CallInfoDataModel();

            var iprCallInfo = GetIPRCallInfo(ssmEvent); // iprCallInfo is struct

            model.CallRef = iprCallInfo.CallRef;
            model.CallSource = iprCallInfo.CallSource;
            model.Cause = iprCallInfo.CallSource;

            model.szCallerId = CharToString(iprCallInfo.szCallerId);
            model.szCalledId = CharToString(iprCallInfo.szCalledId);
            model.szReferredBy = CharToString(iprCallInfo.szReferredBy);
            model.szReferTo = CharToString(iprCallInfo.szReferTo);

            model.RawData = JsonConvert.SerializeObject(model);

            return model;
        }

    }


}
