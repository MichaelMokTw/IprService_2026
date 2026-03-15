using Microsoft.IdentityModel.Tokens;
using MyAPI.Helpers;
using MyAPI.Models;
using MyProject.lib;
using MyProject.Models;
using MyProject.ProjectCtrl;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace MyAPI.BizLogic {
    public class Login : _BaseBiz, IBizInterface {
        private string? _userId {  get; set; }        
        private string? _userRole { get; set; }


        public Login() {
            appConfig = GVar.Configuration;
        }

        // 取得 WEB API 傳入的 Body(Json) 轉 Model(T泛型)
        public T? CheckAndGetInputModel<T>(HttpContext context, out ApiResponseModel responseModel) where T : class {
            //*** 如果沒有 input model, 直接 return null            
            //return null;            

            //*** 如果有 InputModel，讀取 context.Request 轉 T Model
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
            //檢查帳號、密碼...
            var inputModel = input as UserLoginModel;
            apiRetCode = CheckSystemAccount(inputModel!.UserID, inputModel.Password, out errorMsg);
            if (apiRetCode != ENUM_ApiResultCode.OK) {                
                return apiRetCode;
            }           

            return ENUM_ApiResultCode.OK;
        }

        private ENUM_ApiResultCode CheckSystemAccount(string userID, string pwd, out string errMsg) {
            errMsg = "";
            //// 用帳號、密碼判斷角色...，
            //var db = GlobalVar.CreateSqliteDB();
            //var ret = db.GetAllUsersAsync().Result;
            //if (ret.Data == null) { // 表示有錯誤
            //    errMsg = "無法取得 DB 中所有 user 資料(=null)";
            //    apiLog.Info($"CheckSystemAccount: {errMsg}");
            //    return ENUM_ApiResultCode.DataNotFoundInDB;
            //}
            
            //var aesPwd = lib_encode.EncryptAES256(pwd, GlobalVar.AesKey, GlobalVar.AesIv, out string err);

            //_userRole = null;
            //_userId = userID;
            //// 改成加密的 pwd 或 未加密的 pwd 一樣就 ok
            //var user = ret.Data.FirstOrDefault(u => u.UserID == _userId && (u.Password == aesPwd || u.Password == pwd));
            //if (user != null) {                
            //    _userRole = user.AuthType;
            //}
            //else {                                
            //    errMsg = "帳號密碼錯誤";
            //    apiLog.Info($"CheckSystemAccount: {errMsg}");
            //    return ENUM_ApiResultCode.InvalidAccountOrPassword;                
            //}
            return ENUM_ApiResultCode.OK;
        }


        public async Task<IResult> Process(HttpContext context) {
            await Task.Delay(1);
            LogApiStarting(context, out ApiTraceID);

            // 1. 讀取 RequestStream            
            apiLog.Info($"\t 接收到 API: {this.GetType().Name} ({ApiTraceID}), 讀取參數 ...");
            var inputModel = CheckAndGetInputModel<UserLoginModel>(context, out ApiResponseModel apiRespModel);
            if (apiRespModel.ResultCode != (int)ENUM_ApiResultCode.OK) {
                return Results.Json(apiRespModel, statusCode: apiRespModel.HttpCode);
            }

            //2. 檢查 input value
            apiLog.Info($"\t 接收到 API: {this.GetType().Name} ({ApiTraceID}), 讀取參數 ...");
            apiRetCode = ValidInputValue(inputModel, out string errorMsg);
            if (apiRetCode != ENUM_ApiResultCode.OK) {
                var resp = CreateApiRespModel(HttpStatusCode.BadRequest, (int)apiRetCode, $"{apiRetCode.ToDescription()}: {errorMsg}");
                return Results.Json(resp, statusCode: resp.HttpCode);
            }

            // processing 
            apiLog.Info($"\t 開始處理 API Request({ApiTraceID}) ...");
            var retRespJson = DoProcess(); // <=== 真正要處理 API 的 function               
            return retRespJson;            
        }

        private IResult DoProcess() {
            // 加入 claims（包含角色）
            var claims = new[] {
                new Claim(ClaimTypes.Name, _userId!),
                new Claim(ClaimTypes.Role, _userRole!)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GVar.Config!.Jwt!.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(GVar.Config!.Jwt!.ValidMin);
            var expireInSec = GVar.Config!.Jwt!.ValidMin * 60;

            var token = new JwtSecurityToken(
                issuer: GVar.Config!.Jwt!.Issuer,
                audience: GVar.Config!.Jwt!.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var strToken = new JwtSecurityTokenHandler().WriteToken(token);
            var body = new {
                token = strToken,
                expiredSec = expireInSec
            };
            var resp = CreateApiRespModel(HttpStatusCode.OK, (int)apiRetCode, "Ok", 0, "", body);            
            apiLog.Info($"\t 回傳({ApiTraceID}): {JsonConvert.SerializeObject(resp)}");
            return Results.Json(resp, statusCode: resp.HttpCode);
        }


    }
}
