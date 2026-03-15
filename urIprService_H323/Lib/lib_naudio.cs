using NAudio.Wave;

namespace MyProject.lib {
    public static class lib_naudio {
        public static double GetWavLength(string wavFileName) {
            double length = 0.0;
            if (!File.Exists(wavFileName)) {
                return 0.0;
            }
            try {
                // 使用 NAudio 的 AudioFileReader 讀取音檔資訊
                using (var audioFileReader = new AudioFileReader(wavFileName)) {
                    // 獲取音檔的總時長
                    var duration = audioFileReader.TotalTime;
                    // 格式化輸出秒數，保留到小數點 1 位
                    length = Math.Round(duration.TotalSeconds, 1);
                }
            }
            catch (Exception ex) {
                length = 0.0;
            }
            return length;
        }
    }
}
