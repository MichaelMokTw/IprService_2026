using MyAPI.Lib;
using MyProject.lib;
using MyProject.Models;
using MyProject.ProjectCtrl;
using Newtonsoft.Json;
using NLog;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace MyAPI.BizLogic {
    public class _BaseBiz {
        protected string? responseJson = "";
        protected string? responseText = "";
        protected string? tokenError = "";
        protected Logger apiLog = LogManager.GetLogger("ApiLog");
        protected string ApiTraceID = "";
        protected ENUM_ApiResultCode apiRetCode = ENUM_ApiResultCode.OK;

        protected IConfiguration? appConfig;       
        

        public _BaseBiz() {
        }

        public void LogApiStarting(HttpContext context, out string traceID) {                        
            apiLog.Info("");
            apiLog.Info($"===> API: {lib_http.GetContextLog(context, out traceID)}");
        }        

        public ApiResponseModel CreateApiRespModel(HttpStatusCode httpCode, int resultCode, string responseText, int dataCount = 0, string exception = "", Object? Conent = null, Object? extraInfo = null) {
            ApiResponseModel model = new ApiResponseModel();
            model.HttpCode = (int)httpCode;
            model.ResultCode = resultCode;
            model.DataCount = dataCount;
            model.ResponseText = responseText;
            model.Exception = exception;
            model.Content = Conent;
            model.ExtraInfo = extraInfo;
            return model;
        }

        public string CreateApiRespJson(HttpStatusCode httpCode, int resultCode, string responseText, int dataCount = 0, string exception = "", Object Conent = null) {
            var model = CreateApiRespModel(httpCode, resultCode, responseText, dataCount, exception, Conent);
            string jsonRsp = JsonConvert.SerializeObject(model);
            return jsonRsp;
        }

        protected T? GetInputModel<T>(HttpContext context, out ApiResponseModel responseModel) where T : class {
            // 讀取 RequestStream            
            responseModel = new ApiResponseModel();
            var read = lib_http.ReadRequestStream(context.Request).Result;
            if (read.HttpCode != (int)HttpStatusCode.OK) {
                apiLog.Error($"\t ReadRequestStream 失敗:{read.ResponseText}, {read.Exception}");
                responseModel = read;
                return null;
            }
            apiLog.Info($"\t inputStream => {JsonConvert.SerializeObject(read)}");

            //把 ReadRequestStream(string) 轉成Model
            T? inputModel;
            try {
                inputModel = JsonConvert.DeserializeObject<T>((string)read.Content);
            }
            catch (Exception ex) {
                apiRetCode = ENUM_ApiResultCode.JsonConvertModelError;
                responseText = apiRetCode.ToDescription();
                apiLog.Info($"\t{responseText}: {ex.Message}");
                apiLog.Error($"\t 讀取的資料=> {read.Content}");
                responseModel = CreateApiRespModel(HttpStatusCode.BadRequest, (int)apiRetCode, responseText, 0, ex.Message);
                return null;
            }
            apiLog.Info($"\t inputModel => {JsonConvert.SerializeObject(inputModel)}");

            // 0. 檢查 inputModel            
            if (inputModel == null) {
                responseText = $"\t 轉出的 inputModel = null";
                apiRetCode = ENUM_ApiResultCode.RequestStreamIsEmptyOrNull;
                apiLog.Error($"\t {responseText}");
                responseModel = CreateApiRespModel(HttpStatusCode.InternalServerError, (int)apiRetCode, responseText);
                return null;
            }
            return inputModel;
        }


        protected List<string> GetInvalidFields(object? obj) {
            var invalidFields = new List<string>();

            if (obj == null) {
                invalidFields.Add("Object is null");
                return invalidFields;
            }

            var type = obj.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties) {
                var value = prop.GetValue(obj);
                var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                if (propType == typeof(string)) {
                    if (string.IsNullOrEmpty(value as string))
                        invalidFields.Add(prop.Name);
                }
                else if (propType == typeof(int)) {
                    if (value == null || (int)value == 0)
                        invalidFields.Add(prop.Name);
                }
                else if (propType == typeof(double)) {
                    if (value == null || (double)value == 0.0)
                        invalidFields.Add(prop.Name);
                }
                else if (propType == typeof(bool)) {
                    if (value == null)
                        invalidFields.Add(prop.Name);
                }
                else if (propType == typeof(DateTime)) {
                    if (value == null || (DateTime)value == default)
                        invalidFields.Add(prop.Name);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(propType) && propType != typeof(string)) {
                    var enumerable = value as IEnumerable;
                    if (enumerable == null || !enumerable.Cast<object>().Any())
                        invalidFields.Add(prop.Name);
                }
            }

            return invalidFields;
        }
        
    }
}
