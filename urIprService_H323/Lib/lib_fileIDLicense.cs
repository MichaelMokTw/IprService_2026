using Newtonsoft.Json;
using MyProject.lib;
using MyProject.ProjectCtrl;
using System.Runtime.InteropServices;
using System.Text;

namespace MyProject.lib {

    internal class LicRegisterExModel {
        public long LicSeq { set; get; }
        public int LicVer { set; get; }
        public string ProductID { set; get; }
        public string DealerID { set; get; }// 批發商
        public string DealerName { set; get; }
        public string CustID { set; get; }
        public string CustName { set; get; }
        public string HwCpuID { set; get; }
        public string HwBaseBoardID { set; get; }
        public string HwDiskVolumeSN { set; get; } // 專指 Disk C 的序號
        public string HwBiosName { set; get; }
        public string HwBiosVersion { set; get; }
        public string AuthCode { set; get; }
        public DateTime? DemoExpired { set; get; }
        //
        public int UraPort { set; get; }
        public int UrdPort { set; get; }
        public int UrtPort { set; get; }
        public int UripPort { set; get; }
        public int SynipPort { set; get; }
        //        
        public string SystemFunction { set; get; }  // 系統允許那些功能        
        public string WebFunction { set; get; }     // 網頁允許那些功能        
        public string RptFunction { set; get; }     // 報表允許那些功能
        public string AdvRptFunction { set; get; }  // 進階報表功能
        public string AdvFuncExpired { set; get; }  // 進階功能截止日期
                                                    //
        public string Rev1 { set; get; }
        public string Rev2 { set; get; }
        public string Rev3 { set; get; }
        //
        public DateTime? InstallDateTime { set; get; }
        public string InstallHostInfo { set; get; }
        public string InstallSWName { set; get; }
        public string InstallSWVersion { set; get; }
        public int AuthFlag { set; get; }
    }

    public class LicRegisterEx2Model {
        public long LicSeq { set; get; }
        public int LicVer { set; get; }
        public string ProductID { set; get; }
        public string DealerID { set; get; }// 批發商
        public string DealerName { set; get; }
        public string CustID { set; get; }
        public string CustName { set; get; }
        public string HwCpuID { set; get; }
        public string HwBaseBoardID { set; get; }
        public string HwDiskVolumeSN { set; get; } // 專指 Disk C 的序號
        public string HwBiosName { set; get; }
        public string HwBiosVersion { set; get; }
        public string AuthCode { set; get; }
        public DateTime? DemoExpired { set; get; }

        // 錄音系統使用
        public int UraPort { set; get; }
        public int UrdPort { set; get; }
        public int UrtPort { set; get; }
        public int UripPort { set; get; }
        public int SynipPort { set; get; }

        // iQMS 或 Call Center 使用, 20190520 Added
        public int AgentCount { set; get; } // 客服人員數量
        public int AuditAgentCount { set; get; } // 督導席人員數量
        public int ScoreAgentCount { set; get; } // 評分人員數量
        public int FaxCount { set; get; } // 傳真數量數量
        public int MonitorCount { set; get; } // 監聽數量

        public string SystemFunction { set; get; }  // 系統允許那些功能        
        public string WebFunction { set; get; }     // 網頁允許那些功能        
        public string RptFunction { set; get; }     // 報表允許那些功能
        public string AdvRptFunction { set; get; }  // 進階報表功能
        public string AdvFuncExpired { set; get; }  // 進階功能截止日期
                                                    //
        public string Rev1 { set; get; }
        public string Rev2 { set; get; }
        public string Rev3 { set; get; }
        //
        public DateTime? InstallDateTime { set; get; }
        public string InstallHostInfo { set; get; }
        public string InstallSWName { set; get; }
        public string InstallSWVersion { set; get; }
        public int AuthFlag { set; get; }
    }
    
    public class DeviceInfoModel {
        public string CpuID { private set; get; }
        public string BaseBoardID { private set; get; }
        public string DiskVolumeSN { private set; get; } // 專指 Disk C 的序號
        public string BiosName { private set; get; }
        public string BiosVersion { private set; get; }
        public DeviceInfoModel(string cpuID, string boardID, string diskSN, string biosName, string biosVer) {
            CpuID = cpuID;
            BaseBoardID = boardID;
            DiskVolumeSN = diskSN;
            BiosName = biosName;
            BiosVersion = biosVer;
        }
    }


    public class UniFileIDLicense {

        // Windows Struct
        [StructLayout(LayoutKind.Sequential)]
        private struct BY_HANDLE_FILE_INFORMATION {
            public uint FileAttributes;
            public ulong CreationTime;
            public ulong LastAccessTime;
            public ulong LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        // Windows API
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetFileInformationByHandle(IntPtr hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        // Linux Struct
        [StructLayout(LayoutKind.Sequential)]
        private struct Stat {
            public ulong st_dev;
            public ulong st_ino; // Inode number
            public ulong st_nlink;
            public uint st_mode;
            public uint st_uid;
            public uint st_gid;
            public ulong st_rdev;
            public long st_size;
            public long st_blksize;
            public long st_blocks;
            public long st_atime;
            public long st_mtime;
            public long st_ctime;
        }

        // Linux API
        [DllImport("libc.so.6", SetLastError = true)]
        private static extern int stat(string path, out Stat buf);

        // Windows Method
        private static string GetWindowsFileId(string filePath, out string err) {
            err = "";
            try {
                using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    BY_HANDLE_FILE_INFORMATION fileInfo;
                    if (GetFileInformationByHandle(fileStream.SafeFileHandle.DangerousGetHandle(), out fileInfo)) {
                        return $"{fileInfo.FileIndexHigh:X8}{fileInfo.FileIndexLow:X8}";
                    }
                    else {
                        err = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message;
                    }
                }
            }
            catch(Exception ex) {
                err = ex.Message;
            }            
            return "";
        }

        // Linux Method
        private static string GetLinuxFileId(string filePath, out string err) {
            err = "";
            Stat fileStat;
            try {
                if (stat(filePath, out fileStat) == 0) {
                    return fileStat.st_ino.ToString();
                }
                else {
                    err = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message;
                }
            }
            catch(Exception ex) {
                err = ex.Message;
            }            
            return "";
        }

        private static object DecodeLicenseFile(string authFileName, out int licVer, out string err) {
            licVer = 0;
            LicRegisterExModel model = null;
            LicRegisterEx2Model model2 = null;
            var encode = "";            
            err = "";

            // 檢查 file exists
            if (!System.IO.File.Exists(authFileName)) {                
                err = $"lic file not found({authFileName})";
                return null;
            }

            // 讀取 file            
            try {
                encode = System.IO.File.ReadAllText(authFileName, Encoding.UTF8);
                if (encode[0] == '"') {
                    encode = encode.Substring(1, encode.Length - 2);
                }
            }
            catch (Exception ex) {                
                err = $"read lic file error({ex.Message})";
                return null;
            }

            var decodeJson = lib_encode.DecryptAES256(encode, GVar.AesKey, GVar.AesIv, out err);
            if (!string.IsNullOrEmpty(decodeJson)) {
                try {
                    if (decodeJson.Contains("\"LicVer\":1")) {
                        licVer = 1;
                        model = JsonConvert.DeserializeObject<LicRegisterExModel>(decodeJson);
                        return model;
                    }
                    else if (decodeJson.Contains("\"LicVer\":2")) {
                        licVer = 2;
                        model2 = JsonConvert.DeserializeObject<LicRegisterEx2Model>(decodeJson);
                        return model2;
                    }
                    else {                        
                        err = $"decoded result without lic ver";
                        return null;
                    }
                }
                catch (Exception ex) {                    
                    err = $"decoded result convert JSON error: {ex.Message}";
                    return null;
                }
            }
            else {                
                err = "decoded result is null";
                return null;
            }
        }

        /// <summary>
        /// 取得某一個固定檔名的 FileID，這 FileID 在 copy 進OS的時候就不會變更了，
        /// 除非檔案被移動，或 User 把 整個資料夾複製到別處...，用這種方式來保護你的軟體
        /// </summary>
        /// <param name="file">固定檔名</param>
        /// <param name="err">錯誤信息</param>
        /// <returns>(fileID, osName)</returns>
        //public static (string, string) GetClueFileID(string file, out string err) {
        //    var fileID = "";
        //    err = "";            
        //    var osNameAndVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription;            
        //    if (!File.Exists(file)) {
        //        err = "file not found.";
        //        return (fileID, osNameAndVersion);
        //    }

        //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        //        fileID = GetWindowsFileId(file, out err);
        //    }
        //    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
        //        fileID = GetLinuxFileId(file, out err);
        //    }
        //    else {
        //        err = "OS is not supperted";
        //    }
        //    return (fileID, osNameAndVersion);
        //}

        /// <summary>
        /// 取得某一個固定檔名的 FileID，這 FileID 在 copy 進OS的時候就不會變更了，
        /// 除非檔案被移動，或 User 把 整個資料夾複製到別處...，用這種方式來保護你的軟體
        /// </summary>
        /// <param name="file">固定檔名</param>
        /// <param name="err">錯誤信息</param>
        /// <returns>(fileID, osName)</returns>
        public static (string, string) GetLicenseFileID(string file, out string err) {
            var fileID = "";
            err = "";
            var osNameAndVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
            if (!File.Exists(file)) {
                err = "license file not found.";
                return (fileID, osNameAndVersion);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                fileID = GetWindowsFileId(file, out err);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                fileID = GetLinuxFileId(file, out err);
            }
            else {
                err = "OS is not supperted";
            }
            return (fileID, osNameAndVersion);
        }
       

        // 新版
        //  1. 把 clueFile -> licFile，由 appsettings.json 設定路徑，避免常常換版被覆蓋、或更動
        //  2. 把 licFile -> authFile
        public static LicRegisterEx2Model? CheckLicense(string licFile, string authFile, out string osName, out string fileID, out string err) {
            osName = "";
            fileID = "";
            err = "";
            LicRegisterEx2Model? licenseModel = null;

            // 先取 File ID(key)
            try {
                var lic = GetLicenseFileID(licFile, out err);
                osName = lic.Item2;
                if (string.IsNullOrEmpty(lic.Item1)) {
                    err = $"failed to get file ID: {err}"; // Key 取不到
                    return null;
                }
                fileID = lic.Item1;

                if (string.IsNullOrEmpty(authFile)) {
                    err = "auth file not assigned";
                    return null;
                }
                else if (!System.IO.File.Exists(authFile)) {
                    err = $"auth file not found({authFile})";
                    return null;
                }

                var obj = DecodeLicenseFile(authFile, out int authVer, out string authErr);
                if (obj == null) {
                    err = $"license decrypt error: {authErr}";
                    return null;
                }

                // 檢查 license port 數量 > 0
                licenseModel = obj as LicRegisterEx2Model;
                if (licenseModel == null || licenseModel.SynipPort == 0) { // 用當初三匯的 IP port 來管控
                    err = "no recording license";
                    return null;
                }
                // 檢查 license demo
                if (licenseModel.DemoExpired.HasValue) {
                    if ((DateTime.Now - licenseModel.DemoExpired.Value).TotalSeconds > 0) {
                        err = $"demo license expired: {licenseModel.DemoExpired.Value.ToString("yyyy-MM-dd")}";
                        return null;
                    }
                }
                // 檢查 key，把原來的 CPUID 看成 FileID
                if (fileID != licenseModel.HwCpuID) {
                    err = "license key is not matched";
                    return null;
                }
            }
            catch (Exception ex) {
                err = ex.Message;
            }
            return licenseModel;
        }

        private static string EncodeDeviceInfo(string licKey, out string err) {
            err = "";
            DeviceInfoModel devInfo = new DeviceInfoModel(licKey, "Not Used", "", "", "");
            if (devInfo.CpuID == "" || devInfo.BaseBoardID == "") {
                err = "failed to get device info.";
                return ""; // error
            }
            else {
                string jsonStr = JsonConvert.SerializeObject(devInfo);
                return lib_encode.EncryptAES256(jsonStr, GVar.AesKey, GVar.AesIv, out err);
            }
        }

        // keyFile 就是固定的 license.txt
        // return:
        //   string1: key(FileID)
        //   string2: OSName
        //   string3: key file json string
        public static (string, string, string) CreateLicenseKeyModel(string keyFile, out string err) {
            err = "";
            var ret = "";
            var key = GetLicenseFileID(keyFile, out err);
            if (string.IsNullOrEmpty(key.Item1)) {             
                return ("", "", "");
            }

            var encodedDevInfo = EncodeDeviceInfo(key.Item1, out err);
            if (encodedDevInfo != null) {
                var model = new {
                    SWName = "SipRecorder",
                    SWVersion = GVar.CurrentVersion,
                    FunctionName = "GetLicenseEx2", // 表示要呼叫 LicManager 的 GetLicenseEx2(新版)，另外一個是GetLicenseEx(舊版)
                    DeviceInfo = encodedDevInfo
                };
                ret = JsonConvert.SerializeObject(model);
            }
            return (key.Item1, key.Item2, ret);
        }



    }


}
