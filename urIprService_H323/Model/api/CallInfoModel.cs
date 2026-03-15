using System.ComponentModel.DataAnnotations;

namespace MyProject.Models {

    public class CallInfoModel {
        [StringLength(32), Required(ErrorMessage = "請輸入 Event Source(Mitel、Avaya...等)")]
        public string EventSource { get; set; } // 事件來源：Mitel、AVAYA、Cisco、Sangoma、3CX 等 CTI 系統

        [StringLength(32), Required(ErrorMessage = "請輸入分機或設備DN")]
        public string DN { get; set; } // 分機或設備DN

        [Display(Name = "EventType")]
        [Range(1, 12, ErrorMessage = "錯誤: {0} 必須介於 {1} 和 {2} 之間")]
        public int EventType { get; set; } // 事件類別()

        public string GlobalCallID { get; set; } = ""; //發出此通話的全域識別碼，通常為 UUID 格式
        public string CallID { get; set; } = ""; // CallID 不一定是 SIP 的 Call-ID
        public int Cause { get; set; } = -1; // 未指定時為 -1        
        public int CallState { get; set; } = -1; // 未指定時為 -1，參考 CTI 規範
        public string CallStateName { get; set; } = ""; // CallState 的名稱描述

        public int CallType { get; set; } // 0: Outbound,  1: inbound
        public string CallerID { get; set; } // 來電號碼
        public string CalledNo { get; set; } // 外撥號碼
        public string OtherParty { get; set; }

        public CallInfoModel() {
        }


    }
}
