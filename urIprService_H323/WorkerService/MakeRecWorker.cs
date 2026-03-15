using MyProject.Database;
using MyProject.Helper;
using MyProject.lib;
using MyProject.Models;
using MyProject.ProjectCtrl;
using NLog;
using System.Diagnostics;

namespace MyProject.WorkerService {
    class MakeRecWorker : BaseWorker {        
        public override string className => GetType().Name;
        private IHostApplicationLifetime? _hostLifeTime = null;
        private readonly Logger _nlog = LogManager.GetLogger("makeRec");

        private readonly SemaphoreSlim _semaphore; // 最多 5 個並行

        private long _processCount = 0;
        private long _recOkCount = 0;
        private long _recErrorCount = 0;

        public MakeRecWorker(HttpClientHelper httpClientHelper, IHostApplicationLifetime hostLifeTime) : base(hostLifeTime) {
            _hostLifeTime = hostLifeTime;

            // TODO-1. 這裡先暫時 1 個，因為多個 Task 寫 recording list 檔案會 lock，...後續解決
            // TODO-2. UusProcess.GetRecFullFileName => 要偵測有沒有一樣的 *.711、*.wav 
            //         如果有一樣的，要直接換 FileSN ...
            _semaphore = new SemaphoreSlim(1); // 多個並行?
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _nlog.Info("");
            _nlog.Info("");
            _nlog.Info("********************************************");
            _nlog.Info($"\t {className} 服務啟動 ..., version = {GVar.CurrentVersion}");
            _nlog.Info("********************************************");
            //必須給定前置時間給 Batch 檔啟動服務,不然console會判定service啟動錯誤
            var delayBeforeExec = GVar.Config?.DelayBefroreServiceExecute ?? 1;
            await Task.Delay(delayBeforeExec * 1000, stoppingToken);

            _nlog.Info($"{className} ExecuteAsync starting ...");            
            
            DateTime checkTime = DateTime.MinValue; // 故意讓流程一開始要先跑            

            while (!stoppingToken.IsCancellationRequested) {                
                try {
                    GVar.WaitUrsTaskComing.WaitOne(1000); // wait task coming
                    var obj = GVar.GetUrsTaskQueue();
                    _ = ProcessTask(obj, stoppingToken); // fire-and-forget

                    if (TimeIsUp(5, ref checkTime)) {
                        _nlog.Info($"process recording file: {_processCount}, ok={_recOkCount}, err={_recErrorCount}");
                    }
                }
                catch (Exception ex) {
                    _nlog.Error($"[{className}] 執行工作發生錯誤：{ex.Message}");
                }                
                await Task.Delay(1, stoppingToken);
            }
        }

        private async Task ProcessTask(object? obj, CancellationToken ct) {
            if (obj == null || ct.IsCancellationRequested)
                return;

            var timeoutSec = (GVar.Config?.Recording?.ProcessRecFileMaxMin ?? 3) * 60;
            await _semaphore.WaitAsync(ct); // 等待可用名額
            var sw = Stopwatch.StartNew();   // 開始計時

            var taskName = "";            
            try {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSec)); // 最大執行時間 = 20 秒
                var linkedCt = timeoutCts.Token;

                switch (obj) {
                    case RecordingDataModel model:
                        Interlocked.Increment(ref _processCount);
                        taskName = $"MAKE_REC";                        
                        _nlog.Info($">>>>>> ProcessTask[{taskName}] ..., recID={model.RecID}, ext={model.Ext}, len={model.RecLen}, start={model.RecStartTime.ToStdStr()}, caller={model.CallerID}, dtmf={model.DTMF}");                        
                        var ret = await HandleURSRecording(model, linkedCt);                        
                        
                        if (ret) Interlocked.Increment (ref _recOkCount);
                        else     Interlocked.Increment(ref _recErrorCount);

                        break;                    
                    default:
                        _nlog.Warn($"[{className}] 💥ProcessTask: 未知的工作型別：{obj.GetType().FullName}, task 忽略.");
                        break;
                }

                _nlog.Info($"[{className}] ✅ ProcessTask[{taskName}] 完成,  ,耗時 {sw.Elapsed.TotalSeconds:F2} 秒");
            }
            catch (OperationCanceledException) {
                _nlog.Warn($"[{className}] ###### [{taskName}] 被取消或超時 ({timeoutSec} 秒)，已執行 {sw.Elapsed.TotalSeconds:F2} 秒");
            }
            catch (Exception ex) {
                _nlog.Error(ex, $"[{className}] ❌❌❌❌ [{taskName}] 執行發生例外，已執行 {sw.Elapsed.TotalSeconds:F2} 秒");
            }
            finally {
                sw.Stop();
                _semaphore.Release(); // 一定要釋放，避免死鎖
            }
        }


        // 處理 URS 事務:
        //  1. 複製錄音檔到 URS 目錄，並 rename 成 urs 的錄音檔名編碼
        //  2. 寫 URS recording list
        //  3. 寫 URS DB
        private async Task<bool> HandleURSRecording(RecordingDataModel model, CancellationToken ct) {
            // 模擬小延遲，可被取消
            await Task.Delay(1, ct);            
            if (model == null || ct.IsCancellationRequested) 
                return false;            
            
            _nlog.Info($"[{model.RecID}] process recording ..., file={model.BaseFileName}");

            // 1. 檢查是否在排程內
            if (!CheckRecordingSchedule(model.RecID, model.Ext, out string scheduleStr))
                return false;
            
            // 2. 檢查音檔是否太小
            if (!CheckMinRecTime(model))
                return false;

            // 3.檢查檔案是否存在? 注意: BaseFileName 已經在之前變成 *.711 => 並不存在，存在的是 wav
            var file_711 = Path.Combine(model.NewRecFolder, model.BaseFileName); // 
            var file_wav = Path.ChangeExtension(file_711, ".wav");
            
            // 檢查 wav 是否存在
            if (!File.Exists(file_wav)) {
                _nlog.Error($"[{model.RecID}] recording file not found.({file_wav}) ");
                return false;
            }
            // 4. rename file: *.wav => *.711            
            if (!TryRenameFile(file_wav, file_711, 5, out string err)) {
                _nlog.Error($"[{model.RecID}] file rename failed: {err}");
                return false;
            }

            // 5. 寫入 recording list
            var retWrite = GVar.URS!.WriteRecordingList(model);
            if (retWrite.Result)
                _nlog.Info($"[{model.RecID}] 寫入 recording list 成功");
            else
                _nlog.Info($"[{model.RecID}] 寫入 recording list 失敗: {retWrite.ErrorMsg}");

            // 6. 寫 DB
            if (ct.IsCancellationRequested)
                return false;

            var db = new RecDb();
            var backupFlag = false; // 暫時不要單機備份            
            _nlog.Info($"[{model.RecID}] 寫入 recording DB ..., localBackup={backupFlag}");

            var ret = false;
            // 建議改成真正的 async API，而不是直接用 .Result
            try {
                var result = await db.InsertRecData(model, backupFlag ? 1 : 0).WaitAsync(ct);
                if (result.Success && result.Data > 0) {
                    _nlog.Info($"[{model.RecID}] 寫入 URS db 成功, recSeq={result.Data}");
                    ret = true;
                }
                else
                    _nlog.Info($"[{model.RecID}] 寫入 URS db 失敗: {result.UserMessage} {result.TraceMessage}");
            }
            catch (OperationCanceledException) {
                _nlog.Warn($"[{model.RecID}] HandleRecording 被取消: recID={model.RecID}");
            }
            return ret;

        }

        private bool CheckMinRecTime(RecordingDataModel model) {
            var ret = true;
            var minRecSec = GVar.URS?.LoggerConfig?.MinRecTime ?? 3;
            _nlog.Info($"[{model.RecID}] CheckMinRecTime... ext={model.Ext}, recLen={model.RecLen}, minRecTime={minRecSec}s");
            if (model.RecLen < minRecSec) {
                _nlog.Info($"[{model.RecID}] Recording time is too short, ignored!");
                ret = false;
            }
            return ret;
        }

        private bool TryRenameFile(string srcFile, string newFile, int maxSec, out string errMsg) {
            errMsg = "";
            var ret = false;
            var nowTime = DateTime.Now;
            while ((DateTime.Now - nowTime).TotalSeconds <= maxSec) {
                if (lib_misc.RenameFile(srcFile, newFile, out errMsg)) {
                    ret = true;
                    break;
                }
                Thread.Sleep(50);
            }
            return ret;
        }        

        private bool CheckRecordingSchedule(long recID, string ext, out string scheduleStr) {
            scheduleStr = "";
            var ret = true;
            _nlog.Info($"[{recID}] CheckRecordingSchedule... ext={ext}");
            try {
                var chConfig = GVar.URS!.GetChannelConfig(ext);
                if (chConfig == null) {
                    _nlog.Warn($"[{recID}] CheckRecordingSchedule... channelConfig not found(ext={ext}), keep REC file.");
                    return true;
                }

                scheduleStr = GetScheduleString(chConfig);
                _nlog.Info($"[{recID}] CheckRecordingSchedule..., get scheduleStr={scheduleStr}");
                if (CheckNowInScedule(recID, scheduleStr)) {
                    _nlog.Info($"[{recID}] CheckRecordingSchedule = true");
                }
                else {
                    _nlog.Info($"[{recID}] CheckRecordingSchedule = false");
                }
            }
            catch (Exception ex) {
                ret = true; // 若有錯誤，一律視為要錄音，免得漏錄
                _nlog.Info($"\t\t > CheckRecordingSchedule exception(ext={ext}): {ex.Message}");
            }
            return ret;
        }

        public string GetScheduleString(ChannelConfigDataModel chConfig) {
            var scheduleStr = "";
            if (chConfig != null) {
                switch (DateTime.Now.DayOfWeek) {
                    case DayOfWeek.Monday:
                        scheduleStr = (chConfig.MonSchedule == null) ? "" : chConfig.MonSchedule;
                        break;
                    case DayOfWeek.Tuesday:
                        scheduleStr = (chConfig.TueSchedule == null) ? "" : chConfig.TueSchedule;
                        break;
                    case DayOfWeek.Wednesday:
                        scheduleStr = (chConfig.WedSchedule == null) ? "" : chConfig.WedSchedule;
                        break;
                    case DayOfWeek.Thursday:
                        scheduleStr = (chConfig.ThuSchedule == null) ? "" : chConfig.ThuSchedule;
                        break;
                    case DayOfWeek.Friday:
                        scheduleStr = (chConfig.FriSchedule == null) ? "" : chConfig.FriSchedule;
                        break;
                    case DayOfWeek.Saturday:
                        scheduleStr = (chConfig.SatSchedule == null) ? "" : chConfig.SatSchedule;
                        break;
                    case DayOfWeek.Sunday:
                        scheduleStr = (chConfig.SunSchedule == null) ? "" : chConfig.SunSchedule;
                        break;
                    default:
                        scheduleStr = "";
                        break;
                }
            }
            return scheduleStr;
        }

        //檢查是否再目前的 schedule 時間內
        public bool CheckNowInScedule(long recID, string scheduleStr) {
            var ret = false;
            var nowMin = (DateTime.Now.Hour * 60) + DateTime.Now.Minute;
            if (scheduleStr.Length != 24) {                
                _nlog.Info($"[{recID}] CheckNowInScedule Error: invalid schedule format({scheduleStr})");
                return true;
            }
            
            try {
                for (var i = 0; i < 3; i++) {
                    var str = scheduleStr.Substring(i * 8, 8);
                    var start = (int.Parse(str.Substring(0, 2)) * 60) + int.Parse(str.Substring(2, 2)); // HH:MM -> 換算成分鐘
                    var end = (int.Parse(str.Substring(4, 2)) * 60) + int.Parse(str.Substring(6, 2)); // HH:MM -> 換算成分鐘
                    if (!(start == 0 && end == 0) && (start < end)) {
                        if (nowMin >= start && nowMin <= end) {
                            ret = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex) {
                ret = true; // 若有錯誤，一律視為要錄音，免得漏錄
                _nlog.Info($"[{recID}] CheckNowInScedule Exception({scheduleStr}): {ex.Message}");
            }
            
            return ret;
        }
    }
}
