using shpa3api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Synway.Models {

    // 三匯實體卡版的設定
    public class HwBoardDataModel {         
        public int AccreditId { set; get; }
        public int AccreditIdEx { set; get; }
        public int ModelID { set; get; }
        public string Name { set; get; }
        public long SerialNo { set; get; }
        public string ErrMsg { set; get; }
        public int MonitorType { set; get; }            // if IPR_ANA => must = 1 (Station mode)        
        public int IprSlaverCount { set; get; } = 0;    // 若卡版為 IPR_REC，則會有 Slaver(就是負責錄音的迴路數)。否則 = 0        
    }

    // 三匯實體硬體的配置
    public class HwInfoDataModel {
        public int MaxCfgBoard { set; get; }            //SsmGetMaxCfgBoard
        public int MaxUsableBoard { set; get; }         //SsmGetMaxUsableBoard ==> We use number to operate in application
        public int TotalPciBoard { set; get; }          //GetTotalPciBoard
        public int MaxChannel { set; get; }             //SsmGetMaxCh
        public SSM_VERSION DllVersion { set; get; }     //SsmGetDllVersion
        public int InterEventType { set; get; }         // must = 1        
        public int IprRecBoardCount { set; get; }
        public int IprAnaBoardCount { set; get; }
        public int IprRecChannelCount { set; get; }
        public int IprAnaChannelCount { set; get; }
        public int TotalSlaverCount { set; get; }
        public int IPRAnaBoardID { set; get; }
        public int IPRRecBoardID { set; get; }
        public IPR_SLAVERADDR IPRSlaverAddr { set; get; }

        public List<HwBoardDataModel> BoardList { set; get; }

        public HwInfoDataModel() {
            TotalSlaverCount = 0;
            IprAnaBoardCount = 0;
            IprRecBoardCount = 0;
            IprRecChannelCount = 0;
            IprAnaChannelCount = 0;
            IPRAnaBoardID = -1; // 先假設都只有 1 board
            IPRRecBoardID = -1; // 先假設都只有 1 board
            BoardList = new List<HwBoardDataModel>();
            IPRSlaverAddr = new IPR_SLAVERADDR();
        }
                
    }


}
