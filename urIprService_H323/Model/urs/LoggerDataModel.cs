using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Models {    

    public class LoggerDataModel {
        public long LoggerSeq { set; get; } 
        public string LoggerID { set; get; } = string.Empty;
        public string? LoggerName { set; get; } = string.Empty;
        public string? UrsFolder { set; get; } = string.Empty;
        public int BoardNum { set; get; }
        public int VoiceCodec { set; get; }
        public int MinRecTime { set; get; }
        public int MaxRecTime { set; get; }
        public int MaxSilenceTime { set; get; }
        public int MaxBusyToneCycle { set; get; }
    }
}
