using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

public static class BypassChecker {
    /// <summary>
    /// 驗證 bypass 授權檔是否有效（支援多日期格式、以台灣時區為準）
    /// 
    /// 格式:
    /// until=2025-07-26 23:59:59
    /// token=95BC2AEF2D33AF8FCD6A6B3C9AB4673A3FAE4174B92A84AEF3A81CBCE4ED7FA3
    /// </summary>
    public static bool IsBypassAllowed(string secretKey, string path = "bypass.txt") {
        if (!File.Exists(path))
            return false;

        try {
            var dict = File.ReadLines(path)
                .Select(line => line.Split('='))
                .Where(parts => parts.Length == 2)
                .ToDictionary(p => p[0].Trim(), p => p[1].Trim());

            if (!dict.TryGetValue("until", out var untilStr) ||
                !dict.TryGetValue("token", out var tokenStr))
                return false;

            if (!TryParseTaiwanUntil(untilStr, out var untilTaiwanTime))
                return false;

            // 轉換成 UTC 再比較
            var untilUtc = TimeZoneInfo.ConvertTimeToUtc(untilTaiwanTime, TaiwanTimeZone());
            var nowUtc = DateTime.UtcNow;

            if (nowUtc > untilUtc)
                return false;

            var expectedToken = ComputeHmacSha256(secretKey, untilStr);
            return tokenStr.Equals(expectedToken, StringComparison.OrdinalIgnoreCase);
        }
        catch {
            return false;
        }
    }

    /// <summary>
    /// 支援多種格式的 until 值解析為 DateTime（台灣時區）
    /// </summary>
    private static bool TryParseTaiwanUntil(string input, out DateTime taiwanTime) {
        string[] formats = {
            "yyyy-MM-dd",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy/MM/dd",
            "yyyy/MM/dd HH:mm:ss"
        };

        // 如果格式是日期，補上 23:59:59
        foreach (var format in formats) {
            if (DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out taiwanTime)) {
                if (format.Contains("HH")) return true;
                // 是純日期 → 補上 23:59:59
                taiwanTime = taiwanTime.Date.Add(new TimeSpan(23, 59, 59));
                return true;
            }
        }

        // 最後嘗試標準 DateTime.Parse（萬一是其他格式）
        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out taiwanTime)) {
            if (input.Contains(':'))
                return true;
            else {
                taiwanTime = taiwanTime.Date.Add(new TimeSpan(23, 59, 59));
                return true;
            }
        }

        taiwanTime = default;
        return false;
    }

    private static TimeZoneInfo TaiwanTimeZone() =>
        TimeZoneInfo.FindSystemTimeZoneById(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Taipei Standard Time" : "Asia/Taipei");

    private static string ComputeHmacSha256(string key, string message) {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var msgBytes = Encoding.UTF8.GetBytes(message);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(msgBytes);
        return BitConverter.ToString(hash).Replace("-", "");
    }
}
