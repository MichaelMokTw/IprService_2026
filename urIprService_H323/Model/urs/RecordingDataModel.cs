using MyProject.lib;

namespace MyProject.Models {

    public class RecordingDataModel {
        public long LoggerSeq { set; get; } = 0;
        public string LoggerID { set; get; } = string.Empty;
        public string LoggerName { set; get; } = string.Empty;
        public long RecID { set; get; }
        public string AgentID { set; get; } = string.Empty;
        public string BaseFileName { set; get; } = string.Empty;
        public int RecLen { set; get; }
        public DateTime RecStartTime { set; get; } = DateTime.MinValue;
        public int ChNo { set; get; }
        public string ChName { set; get; } = string.Empty;
        public string Ext { set; get; } = string.Empty;
        public int CallType { set; get; }
        public int ChType { set; get; }
        public int RecType { set; get; }
        public int TriggerType { set; get; }
        public string CallerID { set; get; } = string.Empty;
        public string DTMF { set; get; } = string.Empty;
        public int SMDR { set; get; }
        public string DNIS { set; get; } = string.Empty;
        public string rev1 { set; get; } = string.Empty;
        public string DID { set; get; } = string.Empty;
        public int RingLen { set; get; }
        public string rev4 { set; get; } = string.Empty;
        public string rev5 { set; get; } = string.Empty;
        public string rev6 { set; get; } = string.Empty;
        public string rev7 { set; get; } = string.Empty;
        //
        public string RecGuid { set; get; } = string.Empty;
        public string CTIEventLink { set; get; } = string.Empty;
        //
        public decimal RecFileSizeMB { set; get; } = 0;
        public string NewRecFolder { set; get; } = string.Empty;
    }
}
