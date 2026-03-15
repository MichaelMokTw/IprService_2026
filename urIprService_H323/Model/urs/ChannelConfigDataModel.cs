using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Models {

    public enum ENUM_UrsChType {
        Analog = 1,     // 類比
        Digital = 2,    // 數位
        SMDR = 3,       // SMDR(應該沒用)
        T1E1 = 4,       // T1/E1
        IP = 5,         // IP
    }


    public class ChannelConfigDataModel {
        public long ChSeq { set; get; }                         // DB.tblChannel.ChSeq 資料庫序號
        public int ChType { set; get; }                         // DB.tblChannel.ChType 迴路型態
        public int ChID { set; get; }                           // DB.tblChannel.ChID 迴路編號, 對應到 URS 系統的迴路編號, Start From 1
        public string ChName { set; get; } = string.Empty;      // DB.tblChannel.ChName
        public string AgentID { set; get; } = string.Empty;     // DB.tblChannel.AgentID 
        public string AgentName { set; get; } = string.Empty;   // DB.tblChannel.AgentID 
        public int ChActive { set; get; }                       // DB.tblChannel.ChActive 迴路啟用狀態， Ref. ENUM_ChActiveType
        public string ExtNo { set; get; } = string.Empty;       // DB.tblChannel.ExtNo 分機號碼
        public int TriggerType { set; get; }                    // DB.tblChannel.TriggerType 錄音啟動方式, Ref. ENUM_RecTriggerType
        public int RecordingType { set; get; }                  // DB.tblChannel.RecordingType 錄音啟動方式, Ref. ENUM_RecordingType
        public int TalkType { set; get; }                       // DB.tblChannel.TalkType 通話方向方式, Ref. ENUM_TalkType
        public int CallerIDType { set; get; }                   // DB.tblChannel.CallerIDType 來電偵測方式方式, Ref. ENUM_CallerIDType
        //public int RecordBeep { set; get; }                   // 錄音時 Beep 次數 -> 停用，因交換機不支援       
        public int AGC { set; get; }                            // 錄音自動增益 0/1
        public int SOD { set; get; }                            // SOD = Store On Demand
        public int RecVolume { set; get; }                      // 錄音音量
        public int RecVolumeChanged { set; get; }                   // 錄音音量(RecVolume)設定已變更 0/1
        public int Voltage { set; get; }                        // 錄音啟動的電壓值(標準=24V)
        public int VoltageChanged { set; get; }                 // 錄音啟動電壓(Voltage)設定已變更 0/1
        public string MonSchedule { set; get; } = string.Empty; // 每週一的錄音行程: 000023590000000000000000, 共3個時段的設定
        public string TueSchedule { set; get; } = string.Empty; // 每個時段 hhmmhhmm = 8 碼，3個時段共 24 碼...，餘此類推。
        public string WedSchedule { set; get; } = string.Empty;
        public string ThuSchedule { set; get; } = string.Empty;
        public string FriSchedule { set; get; } = string.Empty;
        public string SatSchedule { set; get; } = string.Empty;
        public string SunSchedule { set; get; } = string.Empty;
        //
        public string IPAddr { set; get; } = string.Empty;
        public string Mac { set; get; } = string.Empty;

        // Constructer
        public ChannelConfigDataModel() {
        }
    }

}
