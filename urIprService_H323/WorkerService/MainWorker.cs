using MyProject.lib;
using MyProject.ProjectCtrl;
using MyProject.Synway;
using MyProject.Urs;
using MyProject.Utils;
using NLog;
using richpod.synway;

namespace MyProject.WorkerService {
    class MainWorker : BaseWorker {        
        public override string className => GetType().Name;
        private IHostApplicationLifetime? _hostLifeTime = null;
        private readonly UrsProcess _urs;
        private readonly SynwayCti _cti;        

        public MainWorker(IHttpClientFactory httpClientFactory, IHostApplicationLifetime hostLifeTime) : base(hostLifeTime) {
            _hostLifeTime = hostLifeTime;            

            //初始化nlog
            nlog = LogManager.GetLogger("System");
            GVar.CreateHttpClient(httpClientFactory, "RecordingAPI");

            _urs = new UrsProcess(ENUM_ChannelType.SYNIPR, "Urs", true);
            GVar.URS = _urs; // 讓全域變數也能存取 URS 物件

            _cti = new SynwayCti();
            GVar.CTI = _cti;
        }

        //TODO: 在 API 提供增加/停止分機錄音的功能，方便變更系統設定，而不需要將錄音核心重啟!!!
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            GVar.CancellationToken = stoppingToken;

            // 初始化 RECID (設定機器 ID 與檔案位置)
            var lastRecIDFile = Path.Combine(GVar.MyExePath, "LastRecID.txt");
            Snowflake.Init(GVar.Config!.Recording.MachineID, lastRecIDFile);

            nlog.Info($"{className} ExecuteAsync ...");
            if (!CheckSystemSettings(out string errMsg)) {
                nlog.Error($"[{className}] {errMsg}");
                nlog.Info($"[{className}] 服務停止");
                _hostLifeTime!.StopApplication();
                return;
            }

            //必須給定前置時間給 Batch 檔啟動服務,不然console會判定service啟動錯誤
            var delaySecBefroreExec = GVar.Config?.DelayBefroreServiceExecute ?? 2;
            await Task.Delay(delaySecBefroreExec * 1000, stoppingToken);
            nlog.Info($"{GVar.ProjectName} MainWorker.ExecuteAsync starting ...");

            #region 初始化 Urs
            nlog.Info($"URS process init ...");
            _urs.Init();
            if (_urs.Status == ENUM_UrsStatus.NotSupport) {
                nlog.Error($"[{className}] 設定為不支援 ...，URS 工作停止.");
                return;
            }
            else if (_urs.Status == ENUM_UrsStatus.Failed) {
                nlog.Error($"[{className}] URS process 初始化失敗，URS 工作停止.");
                return;
            }
            #endregion

            #region 初始化 Synway CTI
            var cpuCores = GVar.Config?.IPR.CpuCores ?? 2;
            nlog.Info($"CTI init ...(cpuCores={cpuCores})");
            if (!_cti.InitShCTI(cpuCores)) {
                nlog.Error($"[{className}] CTI init failed.");
                return;
            }
            else {
                nlog.Info($"[{className}] CTI init ok, 監控設備 = {_cti.StationMapped}");
            }
            #endregion

            var deviceCount = 0;
            nlog.Info($"Create IPR worker ...");
            for (var i = 0; i < GVar.CTI!.IPRChInfo.Count; i++) {
                var iprCh = GVar.CTI!.IPRChInfo[i];
                if (iprCh.ChType == ENUM_IprChType.IPR_ANA && !string.IsNullOrEmpty(iprCh.MapToExt))
                    deviceCount++;

                nlog.Info($"\t > Create: IPR[{i,2}], chType={iprCh.ChType.ToString()}, staID={iprCh.StationID,4}, rcvPort=[{iprCh.PriRcvPort,5}][{iprCh.SecRcvPort,5}], mapExt={iprCh.MapToExt,5}, mapUrsChID={iprCh.MapToUrsChID,4}");
                GVar.IprManager.CreateIprWorker(i, iprCh.ChType);
            }            

            var time_ProcessPacket = DateTime.MinValue; // 故意讓流程一開始要先跑
            var time_CheckReport = DateTime.MinValue;
            
            var verStr = GVar.LicenseModel!.DemoExpired.HasValue ? $"Demo版本:{GVar.LicenseModel.DemoExpired.Value.ToString("yyyy-MM-dd")}" : "正式版";            
            nlog.Info($"系統啟動({verStr})...共 {deviceCount} 個設備開始錄音 ...");          

            while (!stoppingToken.IsCancellationRequested) {
                if (TimeIsUp(5, ref time_ProcessPacket)) { // every hour
                    nlog.Info($"訊息分派: {GVar.EventDispatchedCount} 信息處理: {GVar.EventProcessedCount} 錄音中: {GVar.RecordingCount} 錄音完成: {GVar.RecordedCount}");                    
                }

                if (TimeIsUp(3600, ref time_CheckReport)) { // every hour
                    if (!CheckRunningDemo()) {
                        _hostLifeTime.StopApplication();
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
            nlog.Info($"MainWorker.ExecuteAsync 已經離開!");
            // TODO: 此處最好要等待所有的 Tnread 都離開。
            DoExit();
        }

        // 最後面再調整 ...
        private void DoExit() {
            nlog.Info("DoExit() ...");
            nlog.Info("\t> try to stop all channel threads ...");
            //for (var i = 0; i < Global.ChannelWorkers.Count; i++) {
            //    Global.ChannelWorkers[i].RequestStop();
            //    Global.ChannelThreads[i].Join();
            //}



            nlog.Info("\t> try to stop rec data threads ...");
            //if (Global.RecDataWorker != null) {
            //    Global.RecDataWorker.RequestStop();
            //    Global.RecDataThread.Join();
            //}

            nlog.Info("\t> try DeactiveIPRSession and close cti...");            
            lib_synway.CloseCti();
            
            //nLog.Info("\t>try StopSignalRServer...");
            //StopSignalRServer();            
        }

        private bool CheckRunningDemo() {
            // 1. 基礎防呆：如果沒有 License 或沒設定期限，視為永久/正常，不檢查
            if (GVar.LicenseModel == null || !GVar.LicenseModel.DemoExpired.HasValue) {
                return true;
            }

            // 2. 取得相關時間點
            // 基準點：授權日期的「當天午夜 23:59:59」視為結束點，比較符合直覺
            // (例如授權到 2/7，代表 2/7 整天都能用，2/8 00:00 算過期)
            DateTime demoEndDate = GVar.LicenseModel.DemoExpired.Value.Date.AddDays(1).AddSeconds(-1);

            // 寬限期死線：正式過期後再加 3 天
            DateTime finalDeadLine = demoEndDate.AddDays(3);

            // 現在時間 (精確到時分秒，因為你要算小時)
            DateTime now = DateTime.Now;

            // 3. 判斷邏輯
            // 如果「現在時間」已經超過「原本授權結束日」，代表進入【寬限期】或【已失效】狀態
            if (now > demoEndDate) {
                // 計算距離「最終死線」還剩多久
                TimeSpan remainingTime = finalDeadLine - now;
                double leftHours = remainingTime.TotalHours; // 使用 TotalHours 取得含小數點的總時數

                nlog.Info($"Demo 版本授權日期已過 ({GVar.LicenseModel.DemoExpired.Value:yyyy/MM/dd})");

                // Case A: 還在寬限期內 (剩餘時間 > 0)
                if (leftHours > 0) {
                    // 這裡可以轉成整數 int 顯示，比較好看
                    nlog.Info($"系統處於寬限期，預計在 {(int)leftHours} 小時後程式將強制終止！");
                    return true; // 還是回傳 true，讓程式繼續跑
                }
                // Case B: 超過寬限期 (剩餘時間 <= 0)
                else {
                    nlog.Fatal("寬限期已過，程式終止！");
                    return false; // 回傳 false，外層收到後執行 Application.Exit()
                }
            }

            // 4. 還沒過期
            return true;
        }
        
        private bool CheckSystemSettings(out string checkErr) {
            checkErr = "";
            var licFile = GVar.Config!.LicenseFile;
            var authFile = GVar.Config!.AuthFile;

            GVar.LicenseModel = UniFileIDLicense.CheckLicense(licFile, authFile, out string osName, out string fileID, out string err);
            if (GVar.LicenseModel == null) {
                checkErr = $"授權錯誤: {err} @{osName}";
                CreateLicenceKey();
                return false;
            }

            var verExp = "正式授權";
            if (GVar.LicenseModel.DemoExpired.HasValue) {
                verExp = $"\t Demo版本 ~ {GVar.LicenseModel.DemoExpired.Value.ToString("yyyy-MM-dd")}";
            }

            nlog.Info($"license detected:");
            nlog.Info($"\t LicSeq = {GVar.LicenseModel.LicSeq}");
            nlog.Info($"\t LicVer = {GVar.LicenseModel.LicVer}");
            nlog.Info($"\t CustID = {GVar.LicenseModel.CustID}");
            nlog.Info($"\t CustName = {GVar.LicenseModel.CustName}");
            //nlog.Info($"\t IP 監控授權 = {GlobalVar.LicenseModel.MonitorCount}");
            nlog.Info($"\t IPR 錄音授權 = {GVar.LicenseModel.SynipPort}"); // 跟三匯的 SynIp Port 共用
            nlog.Info($"\t 授權方式 = {verExp}"); // 跟三匯的 SynIp Port 共用
            //nlog.Info($"\t SystemFunction = {GlobalVar.LicenseModel.SystemFunction}");
            //nlog.Info($"\t WebFunction = {GlobalVar.LicenseModel.WebFunction}");
            nlog.Info($"\t InstallDateTime = {GVar.LicenseModel.InstallDateTime}");
            nlog.Info($"\t InstallSWName = {GVar.LicenseModel.InstallSWName}");
            nlog.Info($"\t InstallSWVersion = {GVar.LicenseModel.InstallSWVersion}");            

            return true;
        }

        private void CreateLicenceKey() {
            var keyFileName = Path.Combine(GVar.MyExePath, "sipRecorder.key");
            var licFile = GVar.Config!.LicenseFile;
            try {
                var keyModel = UniFileIDLicense.CreateLicenseKeyModel(licFile, out string err);

                nlog.Info($"偵測到平台 OS: {keyModel.Item2}");
                //nlog.Info($"Get FileID={keyModel.Item1}");

                if (!string.IsNullOrEmpty(keyModel.Item3)) {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(keyFileName)) {
                        file.WriteLine(keyModel.Item3);
                    }
                }
                else {
                    nlog.Error($"無法產生 license key 檔案({keyFileName}): {err}");
                }
            }
            catch (Exception ex) {
                nlog.Error($"無法產生 license key 檔案({keyFileName}): {ex.Message}");
            }
            return;
        }
    }
}
