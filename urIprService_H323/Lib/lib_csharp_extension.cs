using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Security.Cryptography;
using System.Globalization;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;

namespace MyProject.lib {
    public static partial class Extensions {        
        public static string ToStdStr(this DateTime? @this, string dateSep = "-", string timeSep = ":", int fractionDigit = 0) {
            var frac = "";
            if (fractionDigit > 0) {
                var len = fractionDigit;
                if (len > 6)
                    len = 6;
                frac = $".{new string('f', len)}";
            }
            var format = $"yyyy{dateSep}MM{dateSep}dd HH{timeSep}mm{timeSep}ss{frac}";
            return @this?.ToString(format) ?? "";
        }

        public static string ToStdStr(this DateTime @this, string dateSep = "-", string timeSep = ":", int fractionDigit = 0) {
            var frac = "";
            if (fractionDigit > 0) {
                var len = fractionDigit;
                if (len > 6)
                    len = 6;
                frac = $".{new string('f', len)}";
            }
            var format = $"yyyy{dateSep}MM{dateSep}dd HH{timeSep}mm{timeSep}ss{frac}";
            return @this.ToString(format) ?? "";
        }

        public static string ToTimeStr(this DateTime? @this, string timeSep = ":", int fractionDigit = 0) {
            var frac = "";
            if (fractionDigit > 0) {
                var len = fractionDigit;
                if (len > 6)
                    len = 6;
                frac = $".{new string('f', len)}";
            }
            var format = $"HH{timeSep}mm{timeSep}ss{frac}";
            return @this?.ToString(format) ?? "";
        }

        public static string ToTimeStr(this DateTime @this, string timeSep = ":", int fractionDigit = 0) {
            var frac = "";
            if (fractionDigit > 0) {
                var len = fractionDigit;
                if (len > 6)
                    len = 6;
                frac = $".{new string('f', len)}";
            }
            var format = $"HH{timeSep}mm{timeSep}ss{frac}";            
            return @this.ToString(format) ?? "";
        }


        public static string GetDescription<TEnum>(this TEnum value) where TEnum : Enum {
            var fi = value.GetType().GetField(value.ToString() ?? string.Empty);

            if (fi != null) {
                var attributes = (DescriptionAttribute[])(fi.GetCustomAttributes(typeof(DescriptionAttribute), false) ?? Array.Empty<DescriptionAttribute>());

                if (attributes.Length > 0) {
                    return attributes[0].Description ?? string.Empty;
                }
            }

            return value.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Build a select list for an enum
        /// </summary>
        public static SelectList? SelectListFor<T>() where T : struct {
            Type t = typeof(T);
            return !t.IsEnum
                ? null
                : new SelectList(BuildSelectListItemsEx(t), "Value", "Text");
        }

        /// <summary>
        /// Build a select list for an enum with a particular value selected
        /// </summary>
        public static SelectList? SelectListFor<T>(T selected) where T : struct {
            Type t = typeof(T);
            return !t.IsEnum
                ? null
                : new SelectList(BuildSelectListItemsEx(t), "Value", "Text", selected.ToString() ?? string.Empty);
        }

        /// <summary>
        /// Build a select list for an enum's names and descriptions
        /// </summary>
        private static IEnumerable<SelectListItem> BuildSelectListItems(Type t) {
            return Enum.GetValues(t)
                       .Cast<Enum>()
                       .Select(e => new SelectListItem {
                           Value = e.ToString() ?? string.Empty,
                           Text = e.GetDescription()
                       });
        }

        /// <summary>
        /// Build a select list for an enum's values and descriptions
        /// </summary>
        public static IEnumerable<SelectListItem> BuildSelectListItemsEx(Type t) {
            return Enum.GetValues(t)
                       .Cast<Enum>()
                       .Select(e => new SelectListItem {
                           Value = Convert.ToInt32(e).ToString(),
                           Text = e.GetDescription()
                       });
        }

        /// <summary>
        /// Convert an enum to its description or name
        /// </summary>
        public static string ToDescription<TEnum>(this TEnum value) where TEnum : Enum {
            var fi = value.GetType().GetField(value.ToString() ?? string.Empty);

            if (fi != null) {
                var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                 ?? Array.Empty<DescriptionAttribute>();

                if (attributes.Length > 0) {
                    return attributes[0].Description ?? string.Empty;
                }
            }

            return value.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Clone a list of items implementing ICloneable
        /// </summary>
        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }

        /// <summary>
        /// Store an object in TempData
        /// </summary>
        public static void Put<T>(this ITempDataDictionary tempData, string key, T value) where T : class {
            tempData[key] = JsonConvert.SerializeObject(value);
        }

        /// <summary>
        /// Retrieve an object from TempData
        /// </summary>
        public static T? Get<T>(this ITempDataDictionary tempData, string key) where T : class {
            tempData.TryGetValue(key, out var o);
            return o == null ? null : JsonConvert.DeserializeObject<T>((string)o);
        }


    }

}

