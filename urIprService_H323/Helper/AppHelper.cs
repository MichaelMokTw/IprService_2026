using MyAPI.BizLogic;


namespace MyAPI.Helpers {

    public static class AppExtensions {       

        public static RouteHandlerBuilder CreateAPI<T>(
            this WebApplication app,
            string pattern,
            Func<string, Delegate, RouteHandlerBuilder> mapMethod, // MapGet | MapPost
            string? authorization = null)                          // null = 不授權, "" = 預設授權, "xxx" = 指定 policy
            where T : IBizInterface {
            // 創建處理函數
            async Task<IResult> HandleRequest(HttpContext context) {
                T api;

                // 檢查是否有帶 HttpContext 參數的構造函數
                var constructor = typeof(T).GetConstructor(new[] { typeof(HttpContext) });
                if (constructor != null) {
                    // 使用帶參數的構造函數
                    api = (T)Activator.CreateInstance(typeof(T), context)!;
                }
                else {
                    // 使用無參數構造函數
                    api = Activator.CreateInstance<T>();
                }

                return await api.Process(context);
            }

            // 映射端點
            var builder = mapMethod(pattern, (HttpContext context) => HandleRequest(context))
                .WithName(typeof(T).Name)
                .WithOpenApi();

            // 處理授權需求
            if (authorization != null) {
                builder = authorization == ""
                    ? builder.RequireAuthorization()
                    : builder.RequireAuthorization(authorization);
            }

            return builder;
        }
    }
}
