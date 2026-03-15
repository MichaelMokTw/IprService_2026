using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NLog;


namespace MyAPI.Helpers {

    public static class FailedToken {
        
        public static string CreateAuthFailedLog(AuthenticationFailedContext context, Logger errLog) {
            var e = new {
                Timestamp = DateTime.UtcNow,
                TraceId = context.HttpContext.TraceIdentifier,
                ExceptionType = context.Exception.GetType().Name,
                ExceptionMessage = context.Exception.Message,
                Path = context.Request.Path.Value,
                Method = context.Request.Method,
                ClientIP = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request?.Headers["User-Agent"].ToString(),
                TokenExpired = context.Exception is SecurityTokenExpiredException,
                AuthHeader = context.Request == null
                                ? ""
                                : context.Request.Headers.ContainsKey("Authorization")
                                    ? "[Present]" // 不记录实际令牌值，避免安全风险
                                    : "[Missing]"
            };
            var sb = new StringBuilder();
            sb.AppendLine($"info     : clientIP={e.ClientIP}, path={e.Path}|{e.Method}, expired={e.TokenExpired}, authHdr={e.AuthHeader}, traceID={e.TraceId}");
            sb.AppendLine($"userAgent: {e.UserAgent}");
            sb.AppendLine($"type     : {e.ExceptionType}");
            sb.AppendLine($"message  : {e.ExceptionMessage}");
            errLog.Error($"Token驗證失敗(AuthFailed) => \r\n{sb.ToString()}");
            return sb.ToString();
        }
        public static string CreateAuthFailedLog(JwtBearerChallengeContext context, Logger errLog) {
            var e = new {
                Timestamp = DateTime.UtcNow,
                TraceId = context.HttpContext.TraceIdentifier,
                Path = context.Request.Path.Value,
                Method = context.Request.Method,
                ClientIP = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                HasAuthHeader = context.Request.Headers.ContainsKey("Authorization"),
                Error = context.Error ?? "None",
                ErrorDescription = context.ErrorDescription ?? "None",
                AuthFailureType = context.AuthenticateFailure?.GetType().Name,
                AuthFailureMessage = context.AuthenticateFailure?.Message
            };
            var sb = new StringBuilder();
            sb.AppendLine($"info     : clientIP={e.ClientIP}, path={e.Path}|{e.Method}, traceID={e.TraceId}");
            sb.AppendLine($"userAgent: {e.UserAgent}");
            sb.AppendLine($"type     : {e.AuthFailureType}{e.AuthFailureMessage}");
            sb.AppendLine($"message  : {e.Error}:{e.ErrorDescription}");
            errLog.Error($"Token驗證失敗(BearerChallenge) => \r\n{sb.ToString()}");
            return sb.ToString();
        }
        public static string CreateAuthFailedLog(ForbiddenContext context, Logger errLog) {
            var user = context.HttpContext.User;

            var e = new {
                Timestamp = DateTime.UtcNow,
                TraceId = context.HttpContext.TraceIdentifier,
                Path = context.Request.Path.Value,
                Method = context.Request.Method,
                ClientIP = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers["User-Agent"].ToString(),

                // 用户信息
                UserId = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown",
                UserName = user?.Identity?.Name ?? "Unknown",
                UserRoles = string.Join(",", user?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? Array.Empty<string>()),
                UserClaims = user?.Claims
                    .Where(c => !new[] { ClaimTypes.NameIdentifier, ClaimTypes.Name, ClaimTypes.Role }.Contains(c.Type))
                    .Select(c => new { Type = c.Type, Value = c.Value })
                    .ToList(),

                AuthType = user?.Identity?.AuthenticationType,
                IsAuthenticated = user?.Identity?.IsAuthenticated ?? false,

                // 尝试确定请求的权限/资源
                RouteController = context.HttpContext.GetRouteData()?.Values["controller"]?.ToString(),
                RouteAction = context.HttpContext.GetRouteData()?.Values["action"]?.ToString(),

                // 请求的权限或资源，如果你的系统有特定的权限标识
                RequiredPermission = context.HttpContext.Items.ContainsKey("RequiredPermission")
                    ? context.HttpContext.Items["RequiredPermission"]?.ToString()
                    : "Unknown"
            };
            var sb = new StringBuilder();
            sb.AppendLine($"info     : clientIP={e.ClientIP}, path={e.Path}|{e.Method}, authType={e.AuthType}, isAuth={e.IsAuthenticated}, traceID={e.TraceId}");
            sb.AppendLine($"userAgent: {e.UserAgent}");
            sb.AppendLine($"userInfo : {e.UserId}|{e.UserName}, roles={e.UserRoles}");
            sb.AppendLine($"message  : 權限不足禁止存取: {e.RequiredPermission}");
            errLog.Error($"Token權限不足(Forbidden) => \r\n{sb.ToString()}");
            return sb.ToString();
        }
       
    }
}
