using System.Collections;
using System.ComponentModel.DataAnnotations;


namespace MyAPI.Helpers {

    #region DataAnnotation 自定義的範例
    //[AttributeUsage(AttributeTargets.Property)]
    //public class NotNullOrEmptyAttribute : ValidationAttribute {
    //    public override bool IsValid(object value) {
    //        if (value == null) return false;

    //        if (value is string str)
    //            return !string.IsNullOrWhiteSpace(str);

    //        if (value is IEnumerable enumerable)
    //            return enumerable.Cast<object>().Any();

    //        return true;
    //    }

    //    public override string FormatErrorMessage(string name) =>
    //        $"{name} 不可為 null 或空值";
    //}

    //[AttributeUsage(AttributeTargets.Property)]
    //public class NotZeroAttribute : ValidationAttribute {
    //    public override bool IsValid(object value) {
    //        if (value == null) return false;

    //        if (value is int intValue)
    //            return intValue != 0;

    //        if (value is double doubleValue)
    //            return doubleValue != 0.0;

    //        return true;
    //    }

    //    public override string FormatErrorMessage(string name) =>
    //        $"{name} 不可為 0";
    //}

    //[AttributeUsage(AttributeTargets.Property)]
    //public class GreaterThanAttribute : ValidationAttribute {
    //    private readonly double _minValue;

    //    public GreaterThanAttribute(double minValue) {
    //        _minValue = minValue;
    //    }

    //    public override bool IsValid(object value) {
    //        if (value == null) return false;

    //        try {
    //            double d = Convert.ToDouble(value);
    //            return d > _minValue;
    //        }
    //        catch {
    //            return false;
    //        }
    //    }

    //    public override string FormatErrorMessage(string name) =>
    //        $"{name} 必須大於 {_minValue}";
    //}
    #endregion


    public static class ValidatorHelper {        

        /// <summary>
        /// 檢查 model 物件中的每一個欄位() 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        public static bool ValidateModel<T>(T model, out List<ValidationResult> results) {
            try {
                results = new List<ValidationResult>();

                if (model == null) {
                    results.Add(new ValidationResult("上傳的資料為 null", new[] { typeof(T).Name }));
                    return false;
                }

                var context = new ValidationContext(model);
                return Validator.TryValidateObject(model, context, results, validateAllProperties: true);
            }
            catch (Exception ex) {
                results = new List<ValidationResult> {
                    new ValidationResult($"驗證過程發生例外: {ex.Message}", new[] { typeof(T).Name })
                };
                return false;
            }

        }

    }
}

