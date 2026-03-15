using NLog;
using richpod.synway;
using shpa3api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace urSynIpr.lib {

    public struct MKT_CALL_INFO {
        public uint CallRef;            //call reference
        public uint CallSource;         //call direction(incoming or outgoing)
        public uint Cause;              //cause        
        public string szCallerId;         //caller number or name        
        public string szCalledId;         //called number or name
        public string szReferredBy;       //from which referred        
        public string szReferTo;          //refer to which
        public MKT_CALL_INFO(int fake = 0) {
            CallRef = 0;            
            CallSource = 0;      
            Cause = 0;
            szCallerId = "";
            szCalledId = "";
            szReferredBy = "";
            szReferTo = "";          
        }
    }

    public struct MKT_SessionInfo {
        public int nCallRef;			//call reference
        public int nStationId;			//one of the station of the session
        public int nStationId2;		    //another station of the session
        public uint dwSessionId;        //session Id
        public IPR_Addr PrimaryAddr;	//ip address and port of primary
        public int nPrimaryCodec;       //codec of primary
        public IPR_Addr SecondaryAddr;	//ip address and port of secondary
        public int nSecondaryCodec;	    //codec of secondary        
        public string szpFowardingIp;	//forwarding ip for forward primary, null if no forwarding        
        public string szsFowardingIp;	//forwarding ip for forward secondary, null if no forwarding
        public int nFowardingPPort;	    //forwarding port for forward primary, -1 if no forwarding
        public int nFowardingSPort;	    //forwarding port for forard secondary, -1 if no forwarding
        public MKT_SessionInfo(int fake=0) {
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

    public struct MKT_EVENT {
        public SSM_EVENT SsmEvent;
        public string PvBuffer;
        public MKT_CALL_INFO MktCallInfo;
        public MKT_SessionInfo MktSessionInfo;
        public MKT_EVENT(SSM_EVENT ssmEvent) {
            //SsmEvent = ssmEvent; <= 錯誤寫法
            SsmEvent = new SSM_EVENT();
            SsmEvent.wEventCode = ssmEvent.wEventCode;
            SsmEvent.nReference = ssmEvent.nReference;
            SsmEvent.dwParam = ssmEvent.dwParam;
            SsmEvent.dwUser = ssmEvent.dwUser;
            SsmEvent.dwSubReason = ssmEvent.dwSubReason;
            SsmEvent.dwXtraInfo = ssmEvent.dwXtraInfo;
            SsmEvent.pvBuffer = ssmEvent.pvBuffer; // 這個使 Pointer，事後不能用他，有可能值會變
            SsmEvent.dwBufferLength = ssmEvent.dwBufferLength;
            SsmEvent.dwDataLength = ssmEvent.dwDataLength;
            SsmEvent.dwEventFlag = ssmEvent.dwEventFlag;
            SsmEvent.dwReserved1 = ssmEvent.dwReserved1;
            SsmEvent.llReserved1 = ssmEvent.llReserved1;
            //

            PvBuffer = lib_ctiDecode.GetSsmEventpvBuffer(ssmEvent);
            MktCallInfo = lib_ctiDecode.GetMKTCallInfo(ssmEvent);
            MktSessionInfo = lib_ctiDecode.GetMKTSessionInfo(ssmEvent);
        }
    }    

    public static class lib_ctiDecode {

        private static Logger _IprEventLog = LogManager.GetLogger($"IprEvent");
        
        public static string GetSsmEventpvBuffer(SSM_EVENT ssmEvent) {
            var s = "";
            if (ssmEvent.pvBuffer != IntPtr.Zero) {
                s = Marshal.PtrToStringAnsi(ssmEvent.pvBuffer);                
            }
            return s;
        }

        public static string UnicodeToUTF8(string from) {
            var bytes = Encoding.UTF8.GetBytes(from);
            return new string(bytes.Select(b => (char)b).ToArray());
        }

        private static IPR_CALL_INFO GetIPRCallInfo(SSM_EVENT ssmEvent) {
            IPR_CALL_INFO iprCallInfo = new IPR_CALL_INFO();
            if (ssmEvent.pvBuffer != IntPtr.Zero) {
                iprCallInfo = (IPR_CALL_INFO)Marshal.PtrToStructure(ssmEvent.pvBuffer, typeof(IPR_CALL_INFO));
            }
            return iprCallInfo;
        }

        public static MKT_CALL_INFO GetMKTCallInfo(SSM_EVENT ssmEvent) {
            var iprCallInfo = GetIPRCallInfo(ssmEvent);
            
            var mktCallInfo = new MKT_CALL_INFO(0); // 呼叫初始化(Constructer)
            mktCallInfo.CallRef = iprCallInfo.CallRef;
            mktCallInfo.CallSource = iprCallInfo.CallSource;
            mktCallInfo.Cause = iprCallInfo.CallSource;
            
            mktCallInfo.szCallerId = CharToString(iprCallInfo.szCallerId);
            mktCallInfo.szCalledId = CharToString(iprCallInfo.szCalledId);
            mktCallInfo.szReferredBy = CharToString(iprCallInfo.szReferredBy);
            mktCallInfo.szReferTo = CharToString(iprCallInfo.szReferTo);
            return mktCallInfo;
        }

        private static IPR_SessionInfo GetIPRSessionInfo(SSM_EVENT ssmEvent) {
            IPR_SessionInfo info = new IPR_SessionInfo();
            if (ssmEvent.pvBuffer != IntPtr.Zero) {
                info = (IPR_SessionInfo)Marshal.PtrToStructure(ssmEvent.pvBuffer, typeof(IPR_SessionInfo));
            }
            return info;
        }

        public static MKT_SessionInfo GetMKTSessionInfo(SSM_EVENT ssmEvent) {
            var iprSessionInfo = GetIPRSessionInfo(ssmEvent);

            var mktSessionInfo = new MKT_SessionInfo(0); // 呼叫初始化(Constructer)
            mktSessionInfo.nCallRef = iprSessionInfo.nCallRef;
            mktSessionInfo.nStationId = iprSessionInfo.nStationId;
            mktSessionInfo.nStationId2 = iprSessionInfo.nStationId2;
            mktSessionInfo.dwSessionId = iprSessionInfo.dwSessionId;
            mktSessionInfo.PrimaryAddr = iprSessionInfo.PrimaryAddr;
            mktSessionInfo.nPrimaryCodec = iprSessionInfo.nPrimaryCodec;
            mktSessionInfo.SecondaryAddr = iprSessionInfo.SecondaryAddr;
            mktSessionInfo.nSecondaryCodec = iprSessionInfo.nSecondaryCodec;
            mktSessionInfo.szpFowardingIp = CharToString(iprSessionInfo.szpFowardingIp);
            mktSessionInfo.szsFowardingIp = CharToString(iprSessionInfo.szsFowardingIp);
            mktSessionInfo.nFowardingPPort = iprSessionInfo.nFowardingPPort;
            mktSessionInfo.nFowardingSPort = iprSessionInfo.nFowardingSPort;
            return mktSessionInfo;
        }

        public static string CharToString(char[] chars) {
            var str = "";
            if (chars != null) {
                var utf8Bytes = Encoding.UTF8.GetBytes(chars);
                str = Encoding.UTF8.GetString(utf8Bytes);
            }                        
            return str.Trim('\0');
        }
        
        public static string GetEventName(int eventCode) {
            if (Enum.IsDefined(typeof(EventCode), eventCode)) {
                return Enum.GetName(typeof(EventCode), eventCode);
            }
            else {
                return $@"???(0x{eventCode:X4})";
            }
        }

        public static string DecodeSsmEventParamStr(SSM_EVENT ssmEvent) {
            string ret = "";
            string str = "";
            uint slaverID = 0;
            uint retCode = 0;
            uint stationID = 0;
            try {
                switch (ssmEvent.wEventCode) {
                    case (ushort)EventCode.E_CHG_ChState:
                        var chState = DecodeChState(ssmEvent);
                        ret = $"nRef={ssmEvent.nReference}, {chState}";
                        break;
                    case (ushort)EventCode.E_IPR_LINK_REC_SLAVER_CONNECTED:
                        slaverID = ssmEvent.dwParam >> 16;
                        ret = $"SlaverID = {slaverID}";
                        break;
                    case (ushort)EventCode.E_IPR_SLAVER_INIT_CB:
                        slaverID = ssmEvent.dwParam >> 16;
                        retCode = ssmEvent.dwParam & 0xffff;
                        str = DecodeSlaverErrorMsg(retCode);
                        ret = $"SlaverID = {slaverID}, retCode={retCode}({str})";
                        break;
                    case (ushort)EventCode.E_RCV_IPR_DChannel:
                        var protoType = ssmEvent.dwXtraInfo >> 16;
                        stationID = ssmEvent.dwXtraInfo & 0xffff;
                        var protoName = DecodeIprProtocolStr(protoType);
                        var dchName = DecodeDEventStr((int)ssmEvent.dwParam);
                        //
                        ret = $"{protoName}, nRef={ssmEvent.nReference}, staID={stationID}, DCh={dchName,-24}";
                        //
                        if (ssmEvent.dwParam == (uint)DEventCode.DE_CISCO_SCCP_CALL_INFO) {
                            // 以後碰到 CISCO 再補
                        }
                        else if ((ssmEvent.dwParam >= (uint)DEventCode.DE_CALL_IN_PROGRESS) && (ssmEvent.dwParam <= (uint)DEventCode.DE_CALL_REJECTED)) {
                            var iprCallInfo = GetIPRCallInfo(ssmEvent); 
                            ret = ret + $"callRef={iprCallInfo.CallRef}, callSrc={iprCallInfo.CallSource}, cause={iprCallInfo.Cause}, " +
                                        $"callerID={CharToString(iprCallInfo.szCallerId)}, calledID={CharToString(iprCallInfo.szCalledId)}, " +
                                        $"transFrom={CharToString(iprCallInfo.szReferredBy)}, transTo={CharToString(iprCallInfo.szReferTo)}";
                        }
                        else if (ssmEvent.dwParam == (uint)DEventCode.DE_SIP_RAW_MSG) {
                            var iprCallInfo = GetIPRCallInfo(ssmEvent); 
                            ret = ret + $"callRef={iprCallInfo.CallRef}, callSrc={iprCallInfo.CallSource}, cause={iprCallInfo.Cause}, " +
                                        $"callerID={CharToString(iprCallInfo.szCallerId)}, calledID={CharToString(iprCallInfo.szCalledId)}, " +
                                        $"transFrom={CharToString(iprCallInfo.szReferredBy)}, transTo={CharToString(iprCallInfo.szReferTo)}";
                        }
                        else if (ssmEvent.dwParam == (uint)DEventCode.DE_REMOTE_PARTYID) {                            
                            ret = ret + $"RemotePartyID={GetSsmEventpvBuffer(ssmEvent)}";
                        }
                        else if (ssmEvent.dwParam == (uint)DEventCode.DE_MSG_CHG) {
                            ret = ret + $", LCD={Marshal.PtrToStringAnsi(ssmEvent.pvBuffer)}";
                        }
                        else if (ssmEvent.dwParam == (uint)DEventCode.DE_AUDIO_CHG) {
                            ret = ret + $", dwSubReason={ssmEvent.dwSubReason}";
                            if ((ssmEvent.dwSubReason & 0x0001) == 0x0001)
                                ret = ret + $", HD-mic(1)";
                            else
                                ret = ret + $", HD-mic(0)";
                            if ((ssmEvent.dwSubReason & 0x0002) == 0x0002)
                                ret = ret + $", HD-spk(1)";
                            else
                                ret = ret + $", HD-spk(0)";
                            if ((ssmEvent.dwSubReason & 0x0004) == 0x0004)
                                ret = ret + $", SP-mic(1)";
                            else
                                ret = ret + $", SP-mic(0)";
                            if ((ssmEvent.dwSubReason & 0x0008) == 0x0008)
                                ret = ret + $", SP-spk(1)";
                            else
                                ret = ret + $", SP-spk(0)";
                        }

                        else if (ssmEvent.dwParam == (uint)DEventCode.DE_DGT_PRS) {
                            var hi = (char)ssmEvent.dwSubReason & 0x00ff;
                            ret = ret + $"dtmf={hi}";
                        }
                        break;
                    case (ushort)EventCode.E_RCV_IPR_STATION_ADDED:
                        stationID = ssmEvent.dwXtraInfo & 0xffff;
                        ret = ret + $"staID={stationID}";
                        break;
                    case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_STARTED:
                    case (ushort)EventCode.E_RCV_IPR_AUX_MEDIA_SESSION_STARTED:
                    case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_STOPED:
                    case (ushort)EventCode.E_RCV_IPR_AUX_MEDIA_SESSION_STOPED:
                        stationID = ssmEvent.dwXtraInfo & 0xffff;
                        var sessionInfo = (IPR_SessionInfo)Marshal.PtrToStructure(ssmEvent.pvBuffer, typeof(IPR_SessionInfo));
                        var str1 = lib_synway.GetIPAddress(sessionInfo.PrimaryAddr);
                        var str2 = lib_synway.GetIPAddress(sessionInfo.SecondaryAddr);
                        ret = ret + $"nRef={ssmEvent.nReference}, callRef={sessionInfo.nCallRef}, staID={stationID}, sessionID={sessionInfo.dwSessionId}, " +
                                    $"stationID1={sessionInfo.nStationId}, stationID2={sessionInfo.nStationId2}, " +
                                    $"primary(ip={str1}, codec={sessionInfo.nPrimaryCodec}), " +
                                    $"secondary(ip={str2}, codec={sessionInfo.nSecondaryCodec})";
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
                        slaverID = ssmEvent.dwParam >> 16;
                        retCode = ssmEvent.dwParam & 0xffff;
                        str = DecodeSlaverErrorMsg(retCode);
                        ret = ret + $"nRef={ssmEvent.nReference}, SlaverID={slaverID}, retCode={retCode}({str})";
                        break;
                    case (ushort)EventCode.E_PROC_RecordEnd:
                        ret = $"nRef={ssmEvent.nReference}, {DecodeRecordEndReason(ssmEvent.dwParam)}";
                        //switch (ssmEvent.dwParam) {
                        //    case 1: ret = $"nRef={ssmEvent.nReference}, stopRecByApp"; break; // 1: Terminated by the application program
                        //    case 2: ret = $"nRef={ssmEvent.nReference}, stopRecByDTMF"; break; // 2: Terminates upon detecting DTMF digits
                        //    case 3: ret = $"nRef={ssmEvent.nReference}, stopRecByPeerHangup"; break; // 3: Terminates upon detecting the remote client’s hangup behavior
                        //    case 4: ret = $"nRef={ssmEvent.nReference}, stopRecByhMaxTime"; break; // 4: Terminates when the recorded data reach a specified length or the recording operation lasts for a specified time.
                        //    case 5: ret = $"nRef={ssmEvent.nReference}, stopRecByRecPaused"; break; // 5: The task of file recording paused
                        //    case 6: ret = $"nRef={ssmEvent.nReference}, stopRecByWriteDataError"; break; // 6: Writing recorded data to files failed
                        //    case 7: ret = $"nRef={ssmEvent.nReference}, stopRecByRTPTimeout"; break; // 7: RTP timeout
                        //    case 8: ret = $"nRef={ssmEvent.nReference}, stopRecByNewPayloadUnsupported"; break; // 8: The RTP payload format is changed in the session, and the new payload format is unsupported
                        //    default: ret = $"nRef={ssmEvent.nReference}, ???({ssmEvent.dwParam})"; break;
                        //}
                        break;
                    case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_FOWARDING:
                    case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_FOWARD_STOPED:
                        ret = ret + $"nRef={ssmEvent.nReference}";
                        break;
                    case (ushort)EventCode.E_RCV_IPR_DONGLE_ADDED:
                        break;
                    default:
                        ret = ret + $"???(eventCode=0x{ssmEvent.wEventCode:X4})";
                        break;
                } // switch
            }
            catch (Exception ex) {
                ret = $"IPR_DecodeSsmEventParamStr exception: {ex.Message}";
                _IprEventLog.Trace(ret);
            }
            return ret;
        }

        public static string DecodeRecordEndReason(uint dwParam) {
            var ret = "";
            switch (dwParam) {
                case 1: ret = "stopRecByApp"; break;            // 1: Terminated by the application program
                case 2: ret = "stopRecByDTMF"; break;           // 2: Terminates upon detecting DTMF digits
                case 3: ret = "stopRecByPeerHangup"; break;     // 3: Terminates upon detecting the remote client’s hangup behavior
                case 4: ret = "stopRecByhMaxTime"; break;       // 4: Terminates when the recorded data reach a specified length or the recording operation lasts for a specified time.
                case 5: ret = "stopRecByRecPaused"; break;      // 5: The task of file recording paused
                case 6: ret = "stopRecByWriteDataError"; break; // 6: Writing recorded data to files failed
                case 7: ret = "stopRecByRTPTimeout"; break;     // 7: RTP timeout
                case 8: ret = "stopRecByNewPayloadUnsupported"; // 8: The RTP payload format is changed in the session, and the new payload format is unsupported
                    break; 
                default: ret = $"???({dwParam})"; break;
            }
            return ret;
        }


        public static string DecodeSsmEventParamStr(MKT_EVENT mktEvent) {
            string ret = "";
            string str = "";
            uint slaverID = 0;
            uint retCode = 0;
            uint stationID = 0;
            try {
                switch (mktEvent.SsmEvent.wEventCode) {
                    case (ushort)EventCode.E_CHG_ChState:
                        var chState = DecodeChState(mktEvent.SsmEvent);
                        ret = $"nRef={mktEvent.SsmEvent.nReference}, {chState}";
                        break;
                    case (ushort)EventCode.E_IPR_LINK_REC_SLAVER_CONNECTED:
                        slaverID = mktEvent.SsmEvent.dwParam >> 16;
                        ret = $"nRef={mktEvent.SsmEvent.nReference}, SlaverID = {slaverID}";
                        break;
                    case (ushort)EventCode.E_IPR_SLAVER_INIT_CB:
                        slaverID = mktEvent.SsmEvent.dwParam >> 16;
                        retCode = mktEvent.SsmEvent.dwParam & 0xffff;
                        str = DecodeSlaverErrorMsg(retCode);
                        ret = $"nRef={mktEvent.SsmEvent.nReference}, SlaverID = {slaverID}, retCode={retCode}({str})";
                        break;
                    case (ushort)EventCode.E_RCV_IPR_DChannel:
                        var protoType = mktEvent.SsmEvent.dwXtraInfo >> 16;
                        stationID = mktEvent.SsmEvent.dwXtraInfo & 0xffff;
                        var protoName = DecodeIprProtocolStr(protoType);
                        var dchName = DecodeDEventStr((int)mktEvent.SsmEvent.dwParam);
                        //
                        //ret = $"{protoName}, nRef={mktEvent.SsmEvent.nReference}, staID={stationID}, DCh={dchName,-24}";
                        ret = $"nRef={mktEvent.SsmEvent.nReference}, staID={stationID}, DCh={dchName,-24}";
                        //
                        if (mktEvent.SsmEvent.dwParam == (uint)DEventCode.DE_CISCO_SCCP_CALL_INFO) {
                            // 以後碰到 CISCO 再補
                        }
                        else if ((mktEvent.SsmEvent.dwParam >= (uint)DEventCode.DE_CALL_IN_PROGRESS) && (mktEvent.SsmEvent.dwParam <= (uint)DEventCode.DE_CALL_REJECTED)) {
                            var mktCallInfo = mktEvent.MktCallInfo; 
                            ret = ret + $"callRef={mktCallInfo.CallRef}, callSrc={mktCallInfo.CallSource}, cause={mktCallInfo.Cause}, " +
                                        $"callerID={mktCallInfo.szCallerId}, calledID={mktCallInfo.szCalledId}, " +
                                        $"transFrom={mktCallInfo.szReferredBy}, transTo={mktCallInfo.szReferTo}";
                        }
                        else if (mktEvent.SsmEvent.dwParam == (uint)DEventCode.DE_SIP_RAW_MSG) {
                            var mktCallInfo = mktEvent.MktCallInfo; 
                            ret = ret + $"callRef={mktCallInfo.CallRef}, callSrc={mktCallInfo.CallSource}, cause={mktCallInfo.Cause}, " +
                                        $"callerID={mktCallInfo.szCallerId}, calledID={mktCallInfo.szCalledId}, " +
                                        $"transFrom={mktCallInfo.szReferredBy}, transTo={mktCallInfo.szReferTo}";
                        }
                        else if (mktEvent.SsmEvent.dwParam == (uint)DEventCode.DE_REMOTE_PARTYID) {
                            ret = ret + $"RemotePartyID={mktEvent.PvBuffer}";
                        }
                        else if (mktEvent.SsmEvent.dwParam == (uint)DEventCode.DE_MSG_CHG) {
                            ret = ret + $", LCD={mktEvent.PvBuffer}";
                        }
                        else if (mktEvent.SsmEvent.dwParam == (uint)DEventCode.DE_AUDIO_CHG) {
                            ret = ret + $", dwSubReason={mktEvent.SsmEvent.dwSubReason}";
                            #region Translate MIC & SPEAKER
                            if ((mktEvent.SsmEvent.dwSubReason & 0x0001) == 0x0001)
                                ret = ret + $", HD-mic(1)";
                            else
                                ret = ret + $", HD-mic(0)";
                            if ((mktEvent.SsmEvent.dwSubReason & 0x0002) == 0x0002)
                                ret = ret + $", HD-spk(1)";
                            else
                                ret = ret + $", HD-spk(0)";
                            if ((mktEvent.SsmEvent.dwSubReason & 0x0004) == 0x0004)
                                ret = ret + $", SP-mic(1)";
                            else
                                ret = ret + $", SP-mic(0)";
                            if ((mktEvent.SsmEvent.dwSubReason & 0x0008) == 0x0008)
                                ret = ret + $", SP-spk(1)";
                            else
                                ret = ret + $", SP-spk(0)";
                            #endregion
                        }

                        else if (mktEvent.SsmEvent.dwParam == (uint)DEventCode.DE_DGT_PRS) {
                            var hi = (char)mktEvent.SsmEvent.dwSubReason & 0x00ff;
                            ret = ret + $"dtmf={hi}";
                        }
                        break;
                    case (ushort)EventCode.E_RCV_IPR_STATION_ADDED:
                        stationID = mktEvent.SsmEvent.dwXtraInfo & 0xffff;
                        ret = ret + $"staID={stationID}";
                        break;
                    case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_STARTED:
                    case (ushort)EventCode.E_RCV_IPR_AUX_MEDIA_SESSION_STARTED:
                    case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_STOPED:
                    case (ushort)EventCode.E_RCV_IPR_AUX_MEDIA_SESSION_STOPED:
                        stationID = mktEvent.SsmEvent.dwXtraInfo & 0xffff;
                        var sessionInfo = (IPR_SessionInfo)Marshal.PtrToStructure(mktEvent.SsmEvent.pvBuffer, typeof(IPR_SessionInfo));
                        var str1 = lib_synway.GetIPAddress(sessionInfo.PrimaryAddr);
                        var str2 = lib_synway.GetIPAddress(sessionInfo.SecondaryAddr);
                        ret = ret + $"nRef={mktEvent.SsmEvent.nReference}, callRef={sessionInfo.nCallRef}, staID={stationID}, sessionID={sessionInfo.dwSessionId}, " +
                                    $"stationID1={sessionInfo.nStationId}, stationID2={sessionInfo.nStationId2}, " +
                                    $"primary(ip={str1}, codec={sessionInfo.nPrimaryCodec}), " +
                                    $"secondary(ip={str2}, codec={sessionInfo.nSecondaryCodec})";
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
                        slaverID = mktEvent.SsmEvent.dwParam >> 16;
                        retCode = mktEvent.SsmEvent.dwParam & 0xffff;
                        str = DecodeSlaverErrorMsg(retCode);
                        ret = ret + $"nRef={mktEvent.SsmEvent.nReference}, SlaverID={slaverID}, retCode={retCode}({str})";
                        break;
                    case (ushort)EventCode.E_PROC_RecordEnd:              // 敶??:敶隞餃蝏迫
                        switch (mktEvent.SsmEvent.dwParam) {
                            case 1: ret = $"nRef={mktEvent.SsmEvent.nReference}, stopRecByApp"; break; // 1: Terminated by the application program
                            case 2: ret = $"nRef={mktEvent.SsmEvent.nReference}, stopRecByDTMF"; break; // 2: Terminates upon detecting DTMF digits
                            case 3: ret = $"nRef={mktEvent.SsmEvent.nReference}, stopRecByPeerHangup"; break; // 3: Terminates upon detecting the remote client’s hangup behavior
                            case 4: ret = $"nRef={mktEvent.SsmEvent.nReference}, stopRecByhMaxTime"; break; // 4: Terminates when the recorded data reach a specified length or the recording operation lasts for a specified time.
                            case 5: ret = $"nRef={mktEvent.SsmEvent.nReference}, stopRecByRecPaused"; break; // 5: The task of file recording paused
                            case 6: ret = $"nRef={mktEvent.SsmEvent.nReference}, stopRecByWriteDataError"; break; // 6: Writing recorded data to files failed
                            case 7: ret = $"nRef={mktEvent.SsmEvent.nReference}, stopRecByRTPTimeout"; break; // 7: RTP timeout
                            case 8: ret = $"nRef={mktEvent.SsmEvent.nReference}, stopRecByNewPayloadUnsupported"; break; // 8: The RTP payload format is changed in the session, and the new payload format is unsupported
                            default: ret = $"nRef={mktEvent.SsmEvent.nReference}, ???({mktEvent.SsmEvent.dwParam})"; break;
                        }
                        break;
                    case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_FOWARDING:
                    case (ushort)EventCode.E_RCV_IPR_MEDIA_SESSION_FOWARD_STOPED:
                        ret = ret + $"nRef={mktEvent.SsmEvent.nReference}";
                        break;
                    case (ushort)EventCode.E_RCV_IPR_DONGLE_ADDED:
                        ret = ret + $"nRef={mktEvent.SsmEvent.nReference}";
                        break;
                    case (ushort)EventCode.E_IPR_RCV_DTMF:                        
                        var dtmf = (char)mktEvent.SsmEvent.dwParam; // 直接就是 dtmf 的十進位，跟三匯文件不一樣
                        ret = ret + $"nRef={mktEvent.SsmEvent.nReference}, dwParam={mktEvent.SsmEvent.dwParam}, dtmf={dtmf}";
                        break;
                    default:
                        ret = ret + $"???(eventCode=0x{mktEvent.SsmEvent.wEventCode:X4})";
                        break;
                } // switch
            }
            catch (Exception ex) {
                ret = $"IPR_DecodeSsmEventParamStr exception: {ex.Message}";
                _IprEventLog.Trace(ret);
            }
            return ret;
        }

        public static string DecodeDEventStr(int dwParam) {
            if (Enum.IsDefined(typeof(DEventCode), dwParam)) {
                return Enum.GetName(typeof(DEventCode), dwParam);
            }
            else {                
                return $@"???(0x{dwParam:X4})";
            }
        }

        public static string DecodeChState(SSM_EVENT ssmEvent) {
            uint hi = 0;    // 之前的迴路狀態
            uint low = 0;   // 目前的迴路狀態
            hi = ssmEvent.dwParam & 0xffff0000;
            hi = hi >> 16;
            var str1 = DecodeChStateStr((int)hi);
            //
            low = ssmEvent.dwParam & 0x0000ffff;
            var str2 = DecodeChStateStr((int)low);
            //            
            return $"{str1}=>{str2}";
        }

        public static string GetChState(SSM_EVENT ssmEvent) {            
            uint low = 0;   // 目前的迴路狀態            
            low = ssmEvent.dwParam & 0x0000ffff;
            return DecodeChStateStr((int)low);                        
        }

        public static string DecodeIprProtocolStr(uint protoType) {
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

        public static string DecodeSlaverErrorMsg(uint errorCode) {
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

        public static string DecodeChStateStr(int chState) {
            if (Enum.IsDefined(typeof(ChState), chState)) {
                return Enum.GetName(typeof(ChState), chState);
            }
            else {
                return $"???({chState})";
            }
        }

        public static string GetSessionInfoStr(MKT_SessionInfo sessionInfo) {
            var priIpAddr = lib_synway.GetIPAddress(sessionInfo.PrimaryAddr);
            var sndIpAddr = lib_synway.GetIPAddress(sessionInfo.SecondaryAddr);
            return $"sessionID={sessionInfo.dwSessionId}, callRef={sessionInfo.nCallRef}, " +
                    $"priIp={priIpAddr}, sndIp={sndIpAddr}, staId1={sessionInfo.nStationId}, staId2={sessionInfo.nStationId2}";
        }

        // 此處假設只有一個 Slaver 
        public static string GetSlaverInfoStr(IPR_SLAVERADDR slaverAddr) {
            var ipAddr = lib_synway.GetIPAddress(slaverAddr.ipAddr);
            return $"Slaver: ID={slaverAddr.nRecSlaverID}, ip={ipAddr}, thdPairs={slaverAddr.nThreadPairs}, " +
                        $"totalResource={slaverAddr.nTotalResources}, usedResource={slaverAddr.nUsedResources}";        
        }


    }
}
