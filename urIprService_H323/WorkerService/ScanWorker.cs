using NLog;
using MyProject.lib;
using MyProject.ProjectCtrl;

namespace MyProject.WorkerService {
    class ScanWorker : BaseWorker {
        public override string className => GetType().Name;
        

        public ScanWorker(IHostApplicationLifetime hostLifeTime) : base(hostLifeTime) {
        
            //初始化nlog
            nlog = LogManager.GetLogger(className);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            //必須給定前置時間給 Batch 檔啟動服務,不然console會判定service啟動錯誤
            var delaySecBefroreExec = GVar.Config?.DelayBefroreServiceExecute ?? 2;
            await Task.Delay(delaySecBefroreExec * 1000, stoppingToken);
            nlog.Info($"{className} ExecuteAsync starting ...");            

            //DateTime checkTime = DateTime.MinValue; // 故意讓流程一開始要先跑
            DateTime checkParser = DateTime.Now;
            DateTime checkLiveMonitorRenew = DateTime.Now;

            while (!stoppingToken.IsCancellationRequested) {                
                try {                    
                    // scan job                     
                }
                catch (Exception ex) {
                    nlog.Info($"執行工作發生錯誤：{ex.Message}");
                }                
                await Task.Delay(1000, stoppingToken);
            }
        }

        

        


        
    }
}
