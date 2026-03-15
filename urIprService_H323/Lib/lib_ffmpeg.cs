using MyProject.lib;
using System.Diagnostics;
using System.Text;

namespace MyProject.lib {
    public class RecMaker {
        private string _ffmpegExe { get; set; }
        private string _soxExe { get; set; }
        public string OutputMessage { get; private set; }

        public RecMaker(string ffmpegExe, string soxExe) {
            _ffmpegExe = ffmpegExe;
            _soxExe = soxExe;
        }

        public int FFMpegCall(string args, string workDir, out string err) {
            err = "";
            int ret = 0;

            if (!File.Exists(_ffmpegExe)) {
                err = $"EXE執行檔不存在: {_ffmpegExe}";
                return -1;
            }

            OutputMessage = "";

            Process process = new Process();
            try {
                process.StartInfo.WorkingDirectory = workDir;
                process.StartInfo.Arguments = args;
                process.StartInfo.FileName = _ffmpegExe;

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.ErrorDataReceived += build_ErrorDataReceived;
                process.OutputDataReceived += build_ErrorDataReceived;
                process.EnableRaisingEvents = true;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
            catch (Exception e) {
                ret = -2;
                err = e.Message;
            }

            if (!process.HasExited) {
                try {
                    process.Kill();
                    process.Close();
                    process.Dispose();
                    process = null;
                }
                catch (Exception e) {
                }
            }
            return ret;
        }

        public int WaitForSoxCall(string args, string workDir, out string err) {
            err = "";
            int ret = 0;

            if (!File.Exists(_soxExe)) {
                err = $"EXE執行檔不存在: {_soxExe}";
                return -1;
            }

            OutputMessage = "";

            Process process = new Process();
            try {
                process.StartInfo.WorkingDirectory = workDir;
                process.StartInfo.Arguments = args;
                process.StartInfo.FileName = _soxExe;

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.ErrorDataReceived += build_ErrorDataReceived;
                process.OutputDataReceived += build_ErrorDataReceived;
                process.EnableRaisingEvents = true;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
            catch (Exception e) {
                ret = -2;
                err = e.Message;
            }

            ret = process.ExitCode;

            if (!process.HasExited) {
                try {
                    process.Kill();
                    process.Close();
                    process.Dispose();
                    process = null;
                }
                catch (Exception e) {
                }
            }
            return ret;
        }

        public int WaitForSoxCall(string args, string workDir, out string err, int timeoutSec = 30) {
            err = "";
            int ret = 0;

            if (!File.Exists(_soxExe)) {
                err = $"EXE執行檔不存在: {_soxExe}";
                return -1;
            }

            Process? process = null;

            try {
                process = new Process();
                process.StartInfo.FileName = _soxExe;
                process.StartInfo.Arguments = args;
                process.StartInfo.WorkingDirectory = workDir;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                var stdoutBuilder = new StringBuilder();
                var stderrBuilder = new StringBuilder();

                process.Start();

                // 同步讀取，避免非同步事件處理問題
                string stdOutput = process.StandardOutput.ReadToEnd();
                string stdError = process.StandardError.ReadToEnd();

                // 等待結束（含 timeout 控制）
                if (!process.WaitForExit(timeoutSec*1000)) {
                    try {
                        process.Kill(true);
                    }
                    catch (Exception exKill) {
                        stderrBuilder.AppendLine($"[強制Kill失敗]: {exKill.Message}");
                    }

                    err = $"[Timeout] Sox 未在 {timeoutSec} 秒內完成，已強制終止。\n{stdError}";
                    return -3;
                }

                ret = process.ExitCode;

                if (ret != 0) {
                    err = $"[Sox錯誤] ExitCode={ret}\n{stdError}";
                }
                else if (!string.IsNullOrWhiteSpace(stdError)) {
                    // 有些工具 exit code 雖成功但 stderr 還是有警告
                    err = $"[Sox警告]\n{stdError}";
                }
            }
            catch (Exception ex) {
                try {
                    if (process != null && !process.HasExited)
                        process.Kill(true);
                }
                catch (Exception exKill) {
                    err += $"\n[強制Kill失敗]: {exKill.Message}";
                }

                err = $"[例外] {ex.Message}";
                return -2;
            }
            finally {
                process?.Dispose();
            }

            return ret;
        }


        private void build_ErrorDataReceived(object sender, DataReceivedEventArgs e) {
            OutputMessage = OutputMessage+ $"\r\n{e.Data}";            
        }

        public void CreateProcessOutputFile(string fullFileName, long fsize) {
            try {
                var ret = fsize <= 0 ? "失敗" : "OK";
                var path = Path.GetDirectoryName(fullFileName);
                var fileName = Path.GetFileName(fullFileName);
                lib_misc.ForceCreateFolder(path);
                
                fileName = fileName.Substring(0, fileName.Length - 4) + $"_{ret}.txt";
                var fullPath = Path.Combine(path, fileName);

                using (StreamWriter writer = new StreamWriter(fullPath)) {
                    writer.WriteLine(OutputMessage);
                }
            }
            catch (Exception ex) {
            }
        }


    }

}
