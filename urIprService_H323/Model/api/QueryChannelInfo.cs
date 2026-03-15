using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

namespace MyProject.Models {

    public class QueryChannelInfo {
        [Required(ErrorMessage = "請輸入迴路型態 ChannelType: Analog, Digital, SMDR, T1E1, IP, All")]
        [RegularExpression("^(?i)(Analog|Digital|T1E1|IP|All)$", ErrorMessage = "無效的線路類型:Analog, Digital, T1E1, IP, All")]
        public string? ChannelType { get; set; } = null; // Analog, Digital, SMDR, T1E1, IP, All        
    }
}
