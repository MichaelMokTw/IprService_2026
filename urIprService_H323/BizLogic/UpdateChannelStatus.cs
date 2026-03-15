using MyAPI.Helpers;
using MyProject.Database;
using MyProject.lib;
using MyProject.Models;
using MyProject.ProjectCtrl;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace MyAPI.BizLogic {

    public class UpdateChannelStatus : _BaseBiz, IBizInterface {

        public UpdateChannelStatus() {
            appConfig = GVar.Configuration;            
        }

        public T? CheckAndGetInputModel<T>(HttpContext context, out ApiResponseModel responseModel) where T : class {
            // 如果沒有 input model, 直接 return null
            //responseModel = new ApiResponseModel();
            //return null;

            // 如果有 InputModel，讀取 context.Request 轉 T Model
            return GetInputModel<T>(context, out responseModel);
        }

        // 用來檢查 Json Body => Model 中任何欄位的值
        public ENUM_ApiResultCode ValidInputValue(object? input, out string errorMsg) {
            errorMsg = "";

            //*** 如果不須檢查傳入的 Json Body => Model 的值，就直接 return
            //return ENUM_ApiResultCode.Ok;

            //*** 如果要檢查 input 的值                        
            // 依照 ViewModel 中 DataAnnotation 的定義檢查欄位 ...
            if (!ValidatorHelper.ValidateModel(input, out List<ValidationResult> listInvalid)) {
                errorMsg = $"上傳資料錯誤: {string.Join(";", listInvalid.Select(r => r.ErrorMessage))}";
                apiLog.Error($"\t {errorMsg}");
                return ENUM_ApiResultCode.IncorrectRequestData;
            }

            // 如果還有其他複雜的邏輯檢查，寫在這裡 ...            

            return ENUM_ApiResultCode.OK;
        }


        public async Task<IResult> Process(HttpContext context) {

            await Task.Delay(1);
            LogApiStarting(context, out ApiTraceID);

            //1. 讀取 RequestStream            
            apiLog.Info($"\t 接收到 API: {this.GetType().Name} ({ApiTraceID}), 檢查參數 ...");
            var inputModel = CheckAndGetInputModel<ChannelStatusModel>(context, out ApiResponseModel apiRespModel);
            if (apiRespModel.ResultCode != (int)ENUM_ApiResultCode.OK) {
                return Results.Json(apiRespModel, statusCode: apiRespModel.HttpCode);
            }

            //2. 檢查 input value
            apiLog.Info($"\t 接收到 API: {this.GetType().Name} ({ApiTraceID}), 檢查參數 ...");
            apiRetCode = ValidInputValue(inputModel, out string errorMsg);
            if (apiRetCode != ENUM_ApiResultCode.OK) {
                var resp = CreateApiRespModel(HttpStatusCode.BadRequest, (int)apiRetCode, errorMsg);
                return Results.Json(resp, statusCode: resp.HttpCode);
            }

            //3. 開始處理 API
            apiLog.Info($"\t 開始處理 API Request({ApiTraceID}) ...");
            return await DoProcess(context, inputModel); // <=== 真正要處理 API 的 function                           
        }

        private async Task<IResult> DoProcess(HttpContext context, ChannelStatusModel? model) {
            var db = new RecDb();            
            var errHD = await db.UpdateChannelStatus(model!);
            ApiResponseModel resp;
            if (errHD.Success) {
                var count = 1;
                resp = CreateApiRespModel(HttpStatusCode.OK, (int)ENUM_ApiResultCode.OK, "Ok", count, "", errHD.Data);
            }
            else {
                resp = CreateApiRespModel(HttpStatusCode.OK, (int)ENUM_ApiResultCode.OK, "Error", 0, "", $"{errHD.UserMessage}");
            }
            apiLog.Info($"\t 回傳({ApiTraceID}): {JsonConvert.SerializeObject(resp)}");
            return Results.Json(resp, statusCode: resp.HttpCode);
        }

    }
}
