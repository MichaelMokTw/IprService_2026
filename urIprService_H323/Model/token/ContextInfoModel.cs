namespace MyAPI.Models {
    public class ContextInfoModel {
        public DateTime? RequestTime { get; set; }        
        public string? SessionID { get; set; }
        public string? Path { get; set; }
        public string? Method { get; set; }
        public string? TraceId { get; set; }
        public string? User { get; set; }
        public string? UserID { get; set; }
        public string? Roles { get; set; }
        public string? UserAgent { get; set; }
        public string? LocalIP { get; set; }
        public string? ClientIP { get; set; }
        public bool IsAuth { get; set; }

    }
}
