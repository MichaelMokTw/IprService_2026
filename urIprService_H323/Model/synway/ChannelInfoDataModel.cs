using MyProject.ProjectCtrl;
using System.Net;

namespace urIprService.Models {
    
    // 資料庫所對應的迴路設定
    public class ChannelConfigDataModel {       
        public long ChSeq { set; get; }         // DB.tblChannel.ChSeq 資料庫序號
        public int ChType { set; get; }         // DB.tblChannel.ChType 迴路型態
        public int ChID { set; get; }           // DB.tblChannel.ChID 迴路編號, 對應到 URS 系統的迴路編號, Start From 1
        public string ChName { set; get; }      // DB.tblChannel.ChName
        public string AgentID { set; get; }     // DB.tblChannel.AgentID 
        public string AgentName { set; get; }   // DB.tblChannel.AgentID 
        public int ChActive { set; get; }       // DB.tblChannel.ChActive 迴路啟用狀態， Ref. ENUM_ChActiveType
        public string ExtNo { set; get; }       // DB.tblChannel.ExtNo 分機號碼
        public int TriggerType { set; get; }    // DB.tblChannel.TriggerType 錄音啟動方式, Ref. ENUM_RecTriggerType
        public int RecordingType { set; get; }  // DB.tblChannel.RecordingType 錄音啟動方式, Ref. ENUM_RecordingType
        public int TalkType { set; get; }       // DB.tblChannel.TalkType 通話方向方式, Ref. ENUM_TalkType
        public int CallerIDType { set; get; }   // DB.tblChannel.CallerIDType 來電偵測方式方式, Ref. ENUM_CallerIDType
        //public int RecordBeep { set; get; }   // 錄音時 Beep 次數 -> 停用，因交換機不支援       
        public int AGC { set; get; }            // 錄音自動增益 0/1
        public int SOD { set; get; }            // SOD = Store On Demand
        public int RecVolume { set; get; }      // 錄音音量
        public int RecVolumeChanged { set; get; }   // 錄音音量(RecVolume)設定已變更 0/1
        public int Voltage { set; get; }        // 錄音啟動的電壓值(標準=24V)
        public int VoltageChanged { set; get; } // 錄音啟動電壓(Voltage)設定已變更 0/1
        public string MonSchedule { set; get; } // 每週一的錄音行程: 000023590000000000000000, 共3個時段的設定
        public string TueSchedule { set; get; } // 每個時段 hhmmhhmm = 8 碼，3個時段共 24 碼...，餘此類推。
        public string WedSchedule { set; get; }
        public string ThuSchedule { set; get; }
        public string FriSchedule { set; get; }
        public string SatSchedule { set; get; }
        public string SunSchedule { set; get; }
        //
        public string IPAddr { set; get; }
        public string Mac { set; get; }

        // Constructer
        public ChannelConfigDataModel() {            
        }
    }

    public class RTP {        
        public UInt16 Seq { set; get; }
        public UInt32 Timestamp { set; get; }
        // Constructer
        public RTP() {            
            Seq = 1;
            Timestamp = 1;            
        }
    }

    public class LiveMonitorModel {
        public IPEndPoint CommEndPoint { set; get; }    // 用來通訊的 UDP EndPoint，IP 一樣，但 port = 6800
        public IPEndPoint RtpEndPoint { set; get; }     // 用來傳送 RTP 的 EndPoint，IP 一樣，但 port = 6800 + iprRecChID
        public DateTime LastTime { set; get; }
        public string AgentID { set; get; }

        // Constructer
        public LiveMonitorModel() {
            CommEndPoint = null;
            RtpEndPoint = null;
            LastTime = DateTime.MinValue; // 若不是在 2 分鐘之內的命令，則要把 RtpEndPoint = null，代表停止監聽
            AgentID = "";
        }

        // LastTime 在期限內(validTimeSec), 就 return true，表示持續監聽，否則就是停止監聽
        public bool CheckIfValidMonitorTime(int validTimeSec) {
            var ret = false;
            try {
                ret = (DateTime.Now - LastTime).TotalSeconds <= validTimeSec;
                if (!ret) {
                    RtpEndPoint = null;
                    AgentID = "";
                }
            }
            catch(Exception ex) {
                ret = false;
            }            
            return ret;
        }
    }


    // 這個結構是對應到 IPRRec/IPRAna 的 Channel(兩種共用), 而不是真正要錄音的分機 Channel
    public class IPRChannelInfoDataModel {
        public int ChID { set; get; } = -1;          // IPR Channel ID, start from 0
        public ENUM_IprChType ChType { set; get; }   // IPR 的 迴路型態(IPR_REC(25), IPR_ANA(26)), 請參考 ENUM_IprChType
        public int ChState { set; get; } = -1;
        public int CallState { set; get; } = -1;
        public int RecordingState { get; set; } = (int)ENUM_RecordingState.Idle; // 目前的錄音狀態
        public long CallRef { set; get; } = -1;      // 用來控制錄音開始結束，開始的 CallRef 要跟結束的一樣!!!        
        public int StationID { set; get; } = -1;
        public int RingFlag { set; get; } = 0;
        public int RecordedCount { set; get; } = 0;
        public int PriRcvPort { set; get; } = -1;
        public int SecRcvPort { set; get; } = -1;
        public int CallSource { set; get; }         // 1: inbound, 2: outbound, 3: nothing
        public string CallerID { set; get; } = string.Empty;    // 來電號碼
        public string DTMF { set; get; } = string.Empty;           
        public DateTime RecPauseTime { set; get; } = DateTime.MinValue;  // 紀錄開始暫停錄音的時間，為了要判斷是否為誤判的錄音暫停(實際上是停止的)                                                          
        public string RemotePartyID { set; get; } = string.Empty;   // 0x1401: SIP 抓到的 RemotePartyID                        
        public string MapToExt { set; get; } = string.Empty; 
        public int MapToUrsChID { set; get; } = -1; // 對應到 Urs 系統的迴路編號，就是指 DB.tblChannel.ChID       

        // 以下欄位 => 開始錄音時先記錄，結束時要迅速抄入 RecControl 中
        public ulong RecID { set; get; }            // 開始錄音時先取得 RECID
        public string RecFullFileName { set; get; } = string.Empty; // 錄音檔檔名
        public DateTime RecStartTime { set; get; }  // 錄音開始時間
        public string RecGuid { set; get; } = string.Empty;        // 錄音的 GUID
        public string CTIEventLink { set; get; } = string.Empty;

        // RTP 專用
        public RTP RTP { set; get; } = new RTP();

        // Live Monitor
        public LiveMonitorModel LiveMonitor { set; get; } = new LiveMonitorModel();                
    }    

}
