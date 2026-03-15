using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

namespace MyProject.Models {

    public class FileDecrypt {

        [Required(ErrorMessage = "請指定輸入檔名")]
        public string EncFile { get; set; } = "";


        [Required(ErrorMessage = "請指定解密後的輸出檔名")]
        public string OutputFile { get; set; } = "";
    }
}
