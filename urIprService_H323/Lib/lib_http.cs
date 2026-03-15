using Microsoft.AspNetCore.Http.Features;
using MyProject.lib;
using MyProject.Models;
using MyProject.ProjectCtrl;
using System.Net;
using System.Text;
using MyAPI.Models;

namespace MyAPI.Lib {
    public static class lib_http {        

        public static async Task<ApiResponseModel> ReadRequestStream(HttpRequest req) {
            var respModel = new ApiResponseModel();
            try {
                using (StreamReader stream = new StreamReader(req.Body)) {
                    respModel.HttpCode = (int)HttpStatusCode.OK;
                    respModel.Content = await stream.ReadToEndAsync();
                }
            }
            catch (Exception ex) {
                var err = ENUM_ApiResultCode.ReadRequestStreamError;
                respModel.ResultCode = (int)err;
                respModel.HttpCode = (int)HttpStatusCode.BadRequest;
                respModel.Content = "";
                respModel.ResponseText = err.ToDescription();
                respModel.Exception = ex.Message;
            }
            return respModel;
        }

        // Dotnet 5 的版本
        public static bool GetBasicAuth(string authHeader, out string account, out string password, out string errMsg) {
            account = "";
            password = "";
            errMsg = "";
            var ret = false;

            if (string.IsNullOrEmpty(authHeader)) {
                errMsg = "本次呼叫未帶 authorization";
                return false;
            }

            if (!authHeader.StartsWith("basic", StringComparison.OrdinalIgnoreCase)) {
                errMsg = "basic auth string wrong format(without basic)";
                return false;
            }

            try {
                var token = authHeader.Substring("Basic ".Length).Trim();
                var str = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                if (string.IsNullOrEmpty(str)) {
                    errMsg = $"basic auth string is null/empty";
                    ret = false;
                }
                else if (!str.Contains(":")) {
                    errMsg = $"basic auth string wrong format(without :)";
                    ret = false;
                }
                else {
                    ret = true;
                    account = str.Split(':')[0];
                    password = str.Split(':')[1];
                }
            }
            catch(Exception ex) {
                errMsg = ex.Message;
                ret = false;
            }            
            return ret;
        }

        public static bool GetBasicAuth(HttpRequestMessage req, out string account, out string password, out string errMsg ) {
            account = "";
            password = "";
            errMsg = "";
            var ret = false;
            string basicAuthBase64String = req.Headers.Authorization?.Parameter ?? "";
            var basicAuthBytes = Convert.FromBase64String(basicAuthBase64String);
            string str = Encoding.UTF8.GetString(basicAuthBytes).Trim();
            if (string.IsNullOrEmpty(str)) {
                errMsg = $"basic auth string is null/empty";
                ret = false;
            }
            else if (!str.Contains(":")) {
                errMsg = $"basic auth string wrong format(without :)";
                ret = false;
            }
            else {
                ret = true;
                account = str.Split(':')[0];
                password = str.Split(':')[1];                
            }            
            return ret;
        }

        public static ContextInfoModel GetContextInfo(HttpContext context, out string traceID) {
            var model = new ContextInfoModel();
            model.RequestTime = DateTime.Now;
            // 如果不先判斷是否有載入 Session Service，即使有判斷是否 null 仍會當機
            if (context.Features.Get<ISessionFeature>()?.Session != null) { 
                model.SessionID = context?.Session?.Id ?? ""; 
            }            
            model.Path = context?.Request?.Path.Value ?? "";
            model.Method = context?.Request?.Method ?? "";
            model.TraceId = context?.TraceIdentifier ?? "";
            traceID = model.TraceId;
            model.User = context?.User?.Identity?.Name ?? "Anonymous";
            model.UserAgent = context?.Request?.Headers["User-Agent"].ToString() ?? "";
            model.LocalIP = context?.Connection?.LocalIpAddress?.ToString();
            model.ClientIP = context?.Connection?.RemoteIpAddress?.ToString();
            model.IsAuth = context?.User?.Identity?.IsAuthenticated ?? false;
            return model;
        }

        public static string GetContextLog(HttpContext context, out string traceID) {
            var model = GetContextInfo(context, out traceID);
            var sb = new StringBuilder();
            sb.AppendLine($"{model.Path}({model.Method}), trace={model.TraceId}, auth={model.IsAuth}, localIP={model.LocalIP}, clientIP={model.ClientIP} ...");            
            sb.AppendLine($"SessionID  : {model.SessionID}");
            sb.AppendLine($"User       : {model.User}");
            sb.Append($"UserAgent  : {model.UserAgent}");            
            return sb.ToString();
        }
    }
}