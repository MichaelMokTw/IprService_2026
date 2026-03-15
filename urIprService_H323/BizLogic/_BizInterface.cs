using MyProject.Models;
using MyProject.ProjectCtrl;

namespace MyAPI.BizLogic {
    public interface IBizInterface {
        Task<IResult> Process(HttpContext context);

        T? CheckAndGetInputModel<T>(HttpContext context, out ApiResponseModel responseModel) where T : class;

        ENUM_ApiResultCode ValidInputValue(object? input, out string extraErrorMsg);
    }
}
