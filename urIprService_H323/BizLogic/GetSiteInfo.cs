using MyProject.Models;
using MyProject.ProjectCtrl;
using Newtonsoft.Json;
using System.Net;

namespace MyAPI.BizLogic {

    public class GetSiteInfo : _BaseBiz, IBizInterface {

        public GetSiteInfo() {
            appConfig = GVar.Configuration;            
        }

        public T? CheckAndGetInputModel<T>(HttpContext context, out ApiResponseModel responseModel) where T : class {
            // 如果沒有 input model, 直接 return null
            responseModel = new ApiResponseModel();
            return null;

            // 如果有 InputModel，讀取 context.Request 轉 T Model
            //return GetInputModel<T>(context, out responseModel);
        }

        public ENUM_ApiResultCode ValidInputValue(object? input, out string extraErrorMsg) {
            extraErrorMsg = "";
            // 不須檢查
            return ENUM_ApiResultCode.OK;
        }

        public ENUM_ApiResultCode ValidToken(HttpContext context, string bodyToken, out string extraErrorMsg) {
            // 不須帶 token
            extraErrorMsg = "";
            return ENUM_ApiResultCode.OK;
        }

        public async Task<IResult> Process(HttpContext context) {
            await Task.Yield();
            LogApiStarting(context, out ApiTraceID);            
            var info = new {
                ServerID = GVar.Config!.ServerID,
                AppID = GVar.Config.AppID,
                ApiVersion = GVar.CurrentVersion,
                ServerIP = GVar.LocalIP,
                ApiType = "WkrService_Net8API",
                Project = GVar.ProjectName,                
            };
            var resp = CreateApiRespModel(HttpStatusCode.OK, (int)ENUM_ApiResultCode.OK, "", 0, "", info);
            apiLog.Info($"\t 回傳({ApiTraceID}): {JsonConvert.SerializeObject(resp)}");
            //return resp;
            return Results.Json(resp, statusCode: resp.HttpCode);
        }

    }
}
