namespace MyProject.Models {

    public class ConfigBoardDataModel {
        public long BoardSeq { set; get; }
        public int BoardID { set; get; }
        public string? BoardType { set; get; } = string.Empty;
        public string? Vendor { set; get; } = string.Empty;
        public string? ModalName { set; get; } = string.Empty;
        public string? HWSN { set; get; } = string.Empty;
        public string? LastError { set; get; } = string.Empty;
        public int HWChannelNum { set; get; }
        public int LogicBeginChNo { set; get; }
        public int LogicEndChNo { set; get; }
        public int Status { set; get; }
        public long LoggerSeq { set; get; }
    }
}
