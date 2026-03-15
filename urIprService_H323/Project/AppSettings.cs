using MyProject.ProjectCtrl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.ProjectCtrl {
    public class AppSettings {
        public string ServerID { get; set; } = "";
        public string AppID { get; set; } = "";
        public string LicenseFile { get; set; } = ""; //產生 fileID 的檔案，不可以亂搬動
                      
        public string AuthFile { get; set; } = ""; //真正的授權檔案 
        public int DelayBefroreServiceExecute { get; set; }        
        public int UpdateModuleStatusIntervalMin { get; set; } = 1;
        public AppSettings_DBConnection? DBConnection { get; set; } = null;
        public AppSettings_URS URS { get; set; } = new AppSettings_URS();
        public AppSettings_IPR IPR { get; set; } = new AppSettings_IPR();

        public AppSettings_Recording Recording { get; set; } = new AppSettings_Recording();

        public AppSettings_WebAPI WebAPI { get; set; } = new AppSettings_WebAPI();

        public AppSettings_Jwt Jwt { get; set; } = new AppSettings_Jwt();
    }

    public class AppSettings_DBConnection {
        public string DBName { get; set; } = "";
        public string SchemaName { get; set; } = "";
        public string MainDBConnStr { get; set; } = "";
        public int DBConnectTimeout { get; set; } = 60;
        public bool SqlTrace { get; set; } = false;
    }
    

    public class AppSettings_URS {        
        public string LoggerID { get; set; } = "";        
        public bool PrintIprDebug { get; set; } = false;
    }

    public class AppSettings_IPR {
        // 取得 E_IPR_RCV_DTMF 的最大時間，從錄音時間起算，超過後就不取, default或沒設定，預設=30秒
        // 如果都要收，可以設9999，如果都不要收，設 0
        public int GetDtmfMaxSec { get; set; } = 20;

        // 錄音占用的 CPU 核心
        public int CpuCores { get; set; } = 2;
    }



    public class AppSettings_Recording {
        public int MachineID { get; set; } = 1;                
        public bool VolumeNormalize { get; set; } = false;                
        public int ClearChannelIntervalSec { get; set; } = 60;        
        public int ForceStopRecMaxHoldMin { get; set; } = 5;        
        public int ProcessRecFileMaxMin { get; set; } = 3;        

    }

    public class AppSettings_WebAPI {
        public int SendAPITimeoutSec { get; set; } = 10;        
    }

    public class AppSettings_Jwt {
        public string SecretKey { get; set; } = string.Empty; // 32 bytes
        public string Issuer { get; set; } = string.Empty; // 發行者
        public string Audience { get; set; } = string.Empty;
        public int ValidMin { get; set; } = 60; // 有效時間(分鐘)        
    }

}
