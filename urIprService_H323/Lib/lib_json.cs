using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyProject.lib {
    public static class lib_json {
        /// <summary>
        /// 解析字串為 JObject，失敗返回 null
        /// </summary>
        public static JObject GetJsonObject(string s) {
            if (string.IsNullOrWhiteSpace(s)) 
                return null;
            try {
                return JObject.Parse(s);
            }
            catch {
                return null;
            }
        }

        /// <summary>
        /// 從 JSON 字串中根據多層 Key 取得內容
        /// </summary>
        public static string GetJsonValue(string jsonStr, params string[] keyList) {
            var jobj = GetJsonObject(jsonStr);
            return jobj == null ? "" : GetJsonValue(jobj, keyList);
        }

        /// <summary>
        /// 核心方法：從 JObject 根據層級 Key 取得內容
        /// </summary>
        public static string GetJsonValue(JToken token, params string[] keyList) {
            if (token == null || keyList == null || keyList.Length == 0)
                return "";

            JToken currentToken = token;

            foreach (var key in keyList) {
                // 檢查節點是否存在且為 Object 類型才能繼續往下找
                if (currentToken is JObject jobj && jobj.ContainsKey(key)) {
                    currentToken = currentToken[key];
                }
                else {
                    return "";
                }
            }

            // 最後回傳字串，處理 null 的情況
            return currentToken?.ToString().Trim() ?? "";
        }
    }
}