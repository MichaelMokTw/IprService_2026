using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyProject.lib {
    public static class lib_regex {
        public static bool IsMatchPattern(string asrText, string tab, List<string> patternList, out string matchedPattern, out string matchedText, ref StringBuilder logText) {
            var ret = false;
            matchedText = "";
            matchedPattern = "";
            if (patternList == null || patternList.Count <= 0)
                return false;
            foreach (var ptrn in patternList) {
                var s = $"模型 = \"{ptrn}\" ，比對中 ...";
                try {
                    var regex = new Regex(ptrn);
                    var match = regex.Match(asrText);
                    if (match != null && match.Success) {   // 有找到 pattern，所以這個就是答案                        
                        s = s + $"***模型符合(\"{match.Value}\")";
                        matchedText = match.Value;
                        matchedPattern = ptrn;
                        ret = true;
                    }
                    else
                        s = s + $"X";
                }
                catch (Exception ex) {
                    s = $"模型語法錯誤: {ex.Message}";
                }
                logText.AppendLine($"{tab}{s}");
                if (ret)
                    return true;
            }
            return false;
        }

        public static bool IsMatchPattern(string asrText, string tab, List<string> patternList, out string matchedPattern, out string matchedText, out Match match, ref StringBuilder logText) {
            var ret = false;
            matchedText = "";
            matchedPattern = "";
            match = null;
            if (patternList == null || patternList.Count <= 0)
                return false;
            foreach (var ptrn in patternList) {
                var s = $"模型 = \"{ptrn}\" ，比對中 ...";
                try {
                    var regex = new Regex(ptrn);
                    match = regex.Match(asrText);
                    if (match != null && match.Success) {   // 有找到 pattern，所以這個就是答案                        
                        s = s + $"***模型符合(\"{match.Value}\")";
                        matchedText = match.Value;
                        matchedPattern = ptrn;
                        ret = true;
                    }
                    else
                        s = s + $"X";
                }
                catch (Exception ex) {
                    s = $"模型語法錯誤: {ex.Message}";
                }
                logText.AppendLine($"{tab}{s}");
                if (ret)
                    return true;
            }
            return false;
        }
    }
}
