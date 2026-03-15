using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Models {
    public class ChannelStatusModel {

        [Required(ErrorMessage = "請輸入錄音主機序號")]
        public long? LoggerSeq { set; get; }
        public int ChID { set; get; }                           // DB.tblChannel.ChID 迴路編號, 對應到 URS 系統的迴路編號, Start From 1
        public string ChName { set; get; } = string.Empty;      // DB.tblChannel.ChName
        public string AgentName { set; get; } = string.Empty;   // DB.tblChannel.AgentID 

        [Required(ErrorMessage = "請輸入分機號碼")]
        public string? ExtNo { set; get; }                      // DB.tblChannel.ExtNo 分機號碼

        // DB.tblChannelStatus: 4: Inbound, 5: Outbound, else: ?
        public int TalkType { set; get; }                       

        public string CallerID { set; get; } = string.Empty;    // DB.tblChannel.AgentID 
        public string DTMF { set; get; } = string.Empty;        // DB.tblChannel.AgentID 
        public long StatusSN { set; get; }

        /*
        --- LineStatus
        ST_CH_IDLE		2
        ST_CH_RING		3
        ST_CH_INBOUND	4
        ST_CH_OUTBOUND	5
        ST_CH_INTERCOM	6
        ST_CH_FAILED	   	54
        在 ura/urd/urt...中, LineSttaus 是用 ASCII
        故需減 48, 其中'f' = 102, 減 48 = 54
        */
        [Required(ErrorMessage = "請輸入 LineStatus: 閒置(2)、響鈴(3)、撥入(4)、外撥(5)")]
        [Range(2, 6, ErrorMessage = "LineStatus 必須是 2 ~ 6 之間")]
        public int? LineStatus { set; get; }

        /*
        ST_REC_IDLE		     2
        ST_REC_SOD		     3
        ST_REC_SCHEDULE	     4
        ST_REC_CONTINUOUS	 5
        ST_REC_FAILED		-1
         */
        [Required(ErrorMessage = "請輸入 RecStatus: 閒置(2)、手動(3)、排程(4)、連續(5)")]
        [Range(2, 5, ErrorMessage = "RecStatus 必須是 2 ~ 5 之間")]
        public int? RecStatus { set; get; }

        //閒置時可以不輸入
        public DateTime? StartTime { set; get; } // 通話開始時間                 

        //閒置時可以不輸入
        public string RecGuid { set; get; }                     


        // Constructer
        public ChannelStatusModel() {
        }
    }

}
