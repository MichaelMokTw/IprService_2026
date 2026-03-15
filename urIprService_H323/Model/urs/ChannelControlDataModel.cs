using MyProject.ProjectCtrl;

namespace urIprService.Models {

    public class ChannelControlDataModel {
        public ENUM_CallDir CallDir { set; get; } = ENUM_CallDir.Inbound;
        public string DTMF { set; get; } = string.Empty;
        public string CallerID { set; get; } = string.Empty;
        public string LastRemotePartyID { set; get; } = string.Empty;
        public DateTime RemotePartyIDSetTime { set; get; } = DateTime.MinValue;

        public ChannelControlDataModel() {            
        }

        public void Reset() {
            CallDir = ENUM_CallDir.Inbound;
            RemotePartyIDSetTime = DateTime.MinValue;
            LastRemotePartyID = "";
            DTMF = "";
            CallerID = "";
        }

        public double GetRemotePartyIDSetSec() {
            var x = (DateTime.Now - RemotePartyIDSetTime).TotalSeconds;
            return x;
        }


    }



}
