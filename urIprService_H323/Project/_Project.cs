using MyProject.lib;
using MyProject.Synway;
using MyProject.Urs;
using NLog;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace MyProject.ProjectCtrl {

    //專案資訊
    public static class GVar {

        private static object objRecorded = new object();
        private static object objRecording = new object();

        //專案名稱
        public const string ServiceName = "IprService_H323";
        public const string ServiceName_Ch = "IP錄音服務";

        // 資料庫加密的 Key & iv
        public const string ProjectName = "IprService_H323";
        public const string DBAesKey = "550102mktok" + "42751171@mitek.com.tw"; // 32 個英文或數字        
        public const string DBAesIV = "0955502123ASDzxc"; // 16 個英文或數字                        
        public const string ENC_PWD = "ab1234..Mkt@Richpod";
        //        
        public static Logger nlog = LogManager.GetLogger("Startup");
        public static string LicenseFile { private set; get; } = "";
        public static string LocalIP { private set; get; } = "";
        public static AppSettings? Config { private set; get; } = new AppSettings();
        public static IConfiguration? Configuration { private set; get; } = null;
        public static CancellationToken CancellationToken { set; get; }

        public static string FFMpegExeFileName { private set; get; } = "";
        public static string SoxExeFileName { private set; get; } = "";        
        public static HttpClient? HttpClient { private set; get; } = null;
        
        // URS
        public static ManualResetEvent WaitUrsTaskComing = new ManualResetEvent(false);
        private static object _ursTaskLock = new object();
        private static Queue<object> _ursTaskQueue = new Queue<object>(500);        

        public static UrsProcess? URS = null;
        public static SynwayCti? CTI = null;

        internal static LicRegisterEx2Model? LicenseModel = null;
        public static int ServerMaxChannel { private set; get; } = 300;
        public static IprManager IprManager = new IprManager();

        public static ulong EventProcessedCount { get; private set; } = 0;
        public static ulong EventDispatchedCount { get; private set; } = 0;
        public static ulong RecordedCount { get; private set; } = 0;
        public static ulong RecordingCount { get; private set; } = 0;

        // 建構子
        static GVar() {
            // 取得 FFMpegExeFileName 位置            
            FFMpegExeFileName = Path.Combine(MyExePath, "3rdParty","ffmpeg.exe");
            SoxExeFileName = Path.Combine(MyExePath, "3rdParty", "sox", "sox.exe");

            // to get local ip address string                        
            try {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
                    socket.Connect("8.8.8.8", 65530);                    
                    IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;
                    LocalIP = (endPoint == null) ? "" : endPoint.Address.ToString();                                        
                }
            }
            catch (Exception ex) {                
                LocalIP = "";
            }
        }

        public static string AesKey {
            //private static string key = "0955502123" + "0955683903" + "9011663903" + "MW"; // 32 個英文或數字        
            get {
                int a = 950;
                int b = 500;
                int c = 100;
                string x = "0" + (a + 5).ToString() + (b + 2).ToString() + (c + 23).ToString(); // 0955502123            
                string y = "0" + (a + 5).ToString() + (b + 183).ToString() + (c + 803).ToString(); // 0955683903
                string z = "0" + (a - 39).ToString() + (b + 163).ToString() + (c + 803).ToString(); // 9011663903
                return x + y + z + "MW";
            }
        }

        // 說明：getIv() 只是在預防DLL被反組譯時增加破解難度。這個 iv 一定要跟 LicenseManager 專案中: MKTConst.cs 的 LicenseIv 一致
        // internal const string LicenseIv = "0492721473" + "mktwen"; // 16 個英文或數字
        public static string AesIv {
            //private static string iv = "0492721473" + "mktwen"; // 16 個英文或數字
            get {
                int a = 50;
                int b = 2700;
                int c = 470;
                return "0" + (a - 1).ToString() + (b + 21).ToString() + (c + 3).ToString() + "mktwen";
            }
        }

        public static string MyExePath {
            get {
                return global::System.AppContext.BaseDirectory;
            }
        }

        public static int LicenseMaxChannel {
            get {
                return LicenseModel?.SynipPort ?? 0;
            }            
        }

        public static void CreateHttpClient(IHttpClientFactory httpClientFactory, string httpName) {
            HttpClient = httpClientFactory.CreateClient(httpName);
        }

        public static bool SetConfiguration(IConfiguration config) {            
            Configuration = config;            
            try {
                Configuration.GetSection("AppSettings").Bind(Config);
            }
            catch(Exception ex) {                
                nlog.Error("*****************************************************");
                nlog.Error("*** 讀取 appsettings.json 錯誤，程式啟動失敗 ***");
                nlog.Error($"\t錯誤: {ex.Message}");
                nlog.Error("*****************************************************");
                return false;
            }            
            
            return true;
        }

        public static string SqliteDBFile {
            get {
                return Path.Combine(MyExePath, "rec", "db", "rec.db");
            }
        }

        public static string? CurrentVersion {
            get {
                return FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;                
            }
        }

        public static string RecRootPath {
            get {                
                return Path.Combine(MyExePath, "rec");
            }
        }


        public static void AddUrsTaskQueue(object obj) {
            lock (_ursTaskLock) {
                _ursTaskQueue.Enqueue(obj);
                WaitUrsTaskComing.Set(); // 有API，要 set，thread 往下跑
            }
        }

        public static object? GetUrsTaskQueue() {
            object? obj = null;
            lock (_ursTaskLock) { // <== 一定要 lock
                if (_ursTaskQueue.Count > 0) {
                    obj = _ursTaskQueue.Dequeue();
                }
                else { // 沒有就東西 reset，thread 會卡住不動
                    WaitUrsTaskComing.Reset();
                }
            }
            return obj;
        }
        
        public static string CheckWavOrEncFileExists(string filePath) {
            var encFilePath = filePath;
            // 一開始應該都是 *.wav
            if (File.Exists(filePath)) {
                return filePath;
            }
            else { // wav 不存在，有可能是 enc 加密檔
                encFilePath = Path.ChangeExtension(filePath, "enc");
                if (!File.Exists(encFilePath)) {
                    encFilePath = "";
                }
            }
            return encFilePath;
        }

        public static void AddDispatchedEventCount() {
            try {
                if (EventDispatchedCount >= (ulong.MaxValue - 1))
                    EventDispatchedCount = 0;
                EventDispatchedCount++;
            }
            catch {
                EventDispatchedCount = 1; // 若錯誤，不重要，直接設 0
            }
        }

        public static void AddProcessedEventCount() {
            try {
                if (EventProcessedCount >= (ulong.MaxValue - 1))
                    EventProcessedCount = 0;
                EventProcessedCount++;
            }
            catch {
                EventProcessedCount = 1; // 若錯誤，不重要，直接設 0
            }
        }

        public static void AddRecordedCount(int value = 1) {
            lock (objRecorded) {
                try {
                    if (RecordedCount >= (ulong.MaxValue - 1))
                        RecordedCount = 0;
                    RecordedCount = RecordedCount + (ulong)value;
                }
                catch (Exception ex) {
                    RecordedCount = 1; // 若錯誤，不重要，直接設 0
                }
            }
        }

        public static void IncRecordingCount() {
            lock (objRecording) {
                RecordingCount++;
            }
        }

        public static void DecRecordingCount() {
            lock (objRecording) {
                RecordingCount--;
            }
        }


    }
}
