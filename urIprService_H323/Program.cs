using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.IdentityModel.Tokens;
using MyAPI.BizLogic;
using MyAPI.Helpers;
using MyProject.Helper;
using MyProject.lib;
using MyProject.Models;
using MyProject.ProjectCtrl;
using MyProject.WorkerService;
using Newtonsoft.Json;
using NLog;
using System.Data;
using System.IO;
using System.Net;
using System.Text;

namespace MyProject {
    internal class Program {
        static void Main(string[] args) {
            WebApplication webApp;
            WebApplicationBuilder builder;
            var nlog = LogManager.GetLogger("Startup");
            var authErrLog = LogManager.GetLogger("authError");

            try {
                var log = $"{new string('*', 12)} {GVar.ServiceName} starting ... v{GVar.CurrentVersion,-19} {new string('*', 12)}";
                Console.WriteLine(log);
                nlog.Info("\r\n\r\n");                
                nlog.Info(new string('*', 80));
                nlog.Info(log);
                nlog.Info(new string('*', 80));

                builder = WebApplication.CreateBuilder(args);

                builder.Services.AddWindowsService();

                nlog.Info($"AddHttpLogging ...");
                builder.Services.AddHttpLogging(options => {
                    options.CombineLogs = true;
                    options.LoggingFields = HttpLoggingFields.RequestBody | HttpLoggingFields.ResponseBody |
                                            HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestPath |
                                            HttpLoggingFields.ResponseStatusCode;
                });

                nlog.Info($"SetConfiguration ..., env={builder.Environment.EnvironmentName}");
                GVar.SetConfiguration(builder.Configuration);
                nlog.Info($"read appSettings =>\r\n{JsonConvert.SerializeObject(GVar.Config, Formatting.Indented)}");

                nlog.Info($"AddCors ...");
                builder.Services.AddCors(options => {
                    options.AddPolicy("Custom", policy => 
                    policy.WithOrigins(builder.Configuration["AppSettings:AllowCORSOrigin"] ?? "*")
                    .AllowAnyHeader()
                    .AllowAnyMethod());
                });

                
                builder.Services.AddTransient<HttpClientHelper>();

                //builder.Services.AddHttpClient(); // <= WEB 不用加，但 console/service 要加，不然 AddTransient 會 error
                builder.Services.AddHttpClient("RecordingAPI", client => {
                    // 全域設定，例如 Timeout
                    client.Timeout = TimeSpan.FromSeconds(GVar.Config?.WebAPI?.SendAPITimeoutSec ?? 10);
                    // 預設標頭
                    client.DefaultRequestHeaders.CacheControl =
                        new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
                });

                // 註冊 Worker                
                nlog.Info("\t 註冊 worker ...");
                builder.Services.AddHostedService<MainWorker>();
                builder.Services.AddHostedService<MakeRecWorker>();

                #region token 驗證機制
                builder.Services.AddAuthentication(options => {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options => {
                    var key = Encoding.UTF8.GetBytes(GVar.Config!.Jwt!.SecretKey);
                    // 配置 JWT 變數
                    options.TokenValidationParameters = new TokenValidationParameters {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = GVar.Config!.Jwt!.Issuer,
                        ValidAudience = GVar.Config!.Jwt!.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ClockSkew = TimeSpan.FromMinutes(5),
                    };
                    // 启用HTTPS要求
                    options.RequireHttpsMetadata = builder.Environment.IsProduction();
                    // 保存token到认证属性中
                    options.SaveToken = true;
                    // 認證失敗處理
                    options.Events = new JwtBearerEvents {
                        OnAuthenticationFailed = async context => {
                            var apiRetCode = ENUM_ApiResultCode.OK;
                            if (context.Exception is SecurityTokenExpiredException) {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                context.Response.Headers.Append("Token-Expired", "true");
                                apiRetCode = ENUM_ApiResultCode.TokenTimeout;
                            }
                            else {
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                apiRetCode = ENUM_ApiResultCode.InvalidToken;
                            }
                            context.Response.ContentType = "application/json; charset=utf-8";

                            FailedToken.CreateAuthFailedLog(context, authErrLog);
                            var baseBiz = new _BaseBiz();
                            var resp = baseBiz.CreateApiRespModel((HttpStatusCode)context.Response.StatusCode, (int)apiRetCode, apiRetCode.GetDescription(), 0, "");
                            await context.Response.WriteAsJsonAsync(resp);
                        },

                        // token驗證失敗或無 token
                        OnChallenge = async context => {
                            // 跳过默认处理
                            context.HandleResponse();

                            if (!context.Response.HasStarted) {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                var apiRetCode = ENUM_ApiResultCode.InvalidToken;
                                context.Response.ContentType = "application/json; charset=utf-8";

                                FailedToken.CreateAuthFailedLog(context, authErrLog);
                                var baseBiz = new _BaseBiz();
                                var resp = baseBiz.CreateApiRespModel((HttpStatusCode)context.Response.StatusCode, (int)apiRetCode, apiRetCode.ToDescription(), 0, "請提供有效的token");
                                await context.Response.WriteAsJsonAsync(resp);
                            }
                        },
                        OnForbidden = async context => {
                            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                            var apiRetCode = ENUM_ApiResultCode.TokenForbiden;
                            context.Response.ContentType = "application/json; charset=utf-8";

                            FailedToken.CreateAuthFailedLog(context, authErrLog);
                            var baseBiz = new _BaseBiz();
                            var resp = baseBiz.CreateApiRespModel((HttpStatusCode)context.Response.StatusCode, (int)apiRetCode, apiRetCode.ToDescription(), 0, "token權限不足");
                            await context.Response.WriteAsJsonAsync(resp);
                        },
                        #region 額外驗證
                        // 可以在这里添加额外的验证逻辑，如检查用户是否仍然存在于数据库中 <== 有需要再加
                        //OnTokenValidated = async context => {            
                        //    var userService = context.HttpContext.RequestServices.GetService<IUserService>();
                        //    var userId = context.Principal?.Identity?.Name;

                        //    if (userService != null && !string.IsNullOrEmpty(userId)) {
                        //        var userExists = await userService.ValidateUserExistsAsync(userId);
                        //        if (!userExists) {
                        //            // 用户不存在或已被禁用
                        //            context.Fail("用户不存在或已被禁用");
                        //        }
                        //    }

                        //    return;
                        //}
                        #endregion
                    };
                });

                // 加入授權機制
                builder.Services.AddAuthorization();
                #endregion

                #region 設定角色政策
                builder.Services.AddAuthorization(options => {
                    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin")); // 配合 login 時的 role，在程式中定義2種
                    options.AddPolicy("UserOnly", policy => policy.RequireRole("user"));   // admin 、user，若有其他，程式中自己再定義
                });
                #endregion

                webApp = builder.Build();
                webApp.UseHttpLogging();
                webApp.UseHttpsRedirection();
                webApp.UseCors("Custom");

                webApp.UseHttpsRedirection();
                webApp.UseAuthentication();
                webApp.UseAuthorization();

                nlog.Info("API Route Mapping ...");
                var path = "api";

                //echo: 測試驗證 API 網址是否有效、有回應。
                webApp.CreateAPI<GetSiteInfo>($"/{path}/echo", webApp.MapGet);

                //login，取得 token
                webApp.CreateAPI<Login>($"/{path}/login", webApp.MapPost);
                
                webApp.CreateAPI<UpdateChannelStatus>($"/{path}/UpdateChannelStatus", webApp.MapPost);

                nlog.Info($"Application is running ..., env={builder.Environment.EnvironmentName}");
                webApp.Run();
            }
            catch (Exception e) {
                nlog.Error($"program.cs exception:\r\n {e}");
            }
            finally {
                nlog.Info("program.cs:結束");
                LogManager.Shutdown();
            }
        }
        
    }
}
