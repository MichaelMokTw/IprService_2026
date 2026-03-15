using MyProject.lib;
using MyProject.ProjectCtrl;

namespace MyProject.Models {

    public class ChannelInfoModel {
        public long ChSeq { set; get; }                         // DB.tblChannel.ChSeq 資料庫序號
        public int ChType { set; get; }                         // DB.tblChannel.ChType 迴路型態
        public int ChID { set; get; }                           // DB.tblChannel.ChID 迴路編號, 對應到 URS 系統的迴路編號, Start From 1
        public string ChName { set; get; } = string.Empty;      // DB.tblChannel.ChName
        public string AgentID { set; get; } = string.Empty;     // DB.tblChannel.AgentID 
        public string AgentName { set; get; } = string.Empty;   // DB.tblChannel.AgentID 
        public int ChActive { set; get; }                       // DB.tblChannel.ChActive 迴路啟用狀態， Ref. ENUM_ChActiveType
        public string ExtNo { set; get; } = string.Empty;       // DB.tblChannel.ExtNo 分機號碼        
        public string IPAddr { set; get; } = string.Empty;
        public string Mac { set; get; } = string.Empty;

        // Constructer
        public ChannelInfoModel() {
        }
    }
}
