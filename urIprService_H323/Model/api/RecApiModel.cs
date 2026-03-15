using Newtonsoft.Json.Linq;

namespace MyProject.Models {

    public class RecApiModel {
        public int ChannelNo { get; set; } = 0;
        public string Extn { get; set; } = string.Empty;
        public int Status { get; set; } = 0;
        public string Message { get; set; } = string.Empty;
        public string WavFileName { get; set; } = string.Empty;
        public bool Encrypt { get; set; } = true;
        public long RecID { get; set; } = 0;
        public string RecGUID { get; set; } = string.Empty;
        public string CallID { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; } = null;
        public decimal RecLength { get; set; } = 0;
        public decimal RecFileSizeMB { get; set; } = 0;
        public JObject? RecRtp { get; set; } = null;
    }
}
