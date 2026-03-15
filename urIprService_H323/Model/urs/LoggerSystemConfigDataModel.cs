using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Models {

    public class LoggerSystemConfigDataModel {
        public long LoggerSeq { set; get; }
        public long DefaultRecMbPerDay { set; get; }
        public int DataReservedDays { set; get; }
        public int DebugLogResvDays { set; get; }
        public int SysLogResvYears { set; get; }
        public int LCDEventDebug { set; get; }
        public int RecordingDebug { set; get; }
        public int CTIEventDebug { set; get; }
        public int UserDebug { set; get; }
        public int SystemDebug { set; get; }
        public int UraIsSMDR { set; get; }
        public string? SearchPlayShowColumns { set; get; } = string.Empty;
        public int RtpBasePort { set; get; }
        public int HttpAudioPort { set; get; }
        public int EncryptRecFile { set; get; }
        public int EnableRTP { set; get; }
        public int EnableVLC { set; get; }
    }
}
