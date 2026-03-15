using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Security.Cryptography;

namespace MyProject.lib {
    public static class lib_encode
    {
        /// <summary>
        /// 使用 AES-256 CBC 模式加密字串
        /// </summary>
        /// <param name="source">原始文字</param>
        /// <param name="key">32 字元長度金鑰 (256 bits)</param>
        /// <param name="iv">16 字元長度 IV (128 bits)</param>
        /// <param name="err">錯誤訊息</param>
        /// <returns>Base64 加密結果</returns>
        public static string EncryptAES256(string source, string key, string iv, out string err) {
            err = string.Empty;

            try {
                if (string.IsNullOrEmpty(source))
                    throw new ArgumentNullException(nameof(source), "來源字串不可為空");

                if (key.Length != 32)
                    throw new ArgumentException("金鑰長度必須為 32 字元 (256 bits)", nameof(key));

                if (iv.Length != 16)
                    throw new ArgumentException("IV 長度必須為 16 字元 (128 bits)", nameof(iv));

                byte[] sourceBytes = Encoding.UTF8.GetBytes(source);
                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                byte[] ivBytes = Encoding.UTF8.GetBytes(iv);

                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                byte[] encrypted = encryptor.TransformFinalBlock(sourceBytes, 0, sourceBytes.Length);
                return Convert.ToBase64String(encrypted);
            }
            catch (Exception ex) {
                err = ex.Message;
                return string.Empty;
            }
        }

        /// <summary>
        /// 使用 AES-256 CBC 模式解密字串
        /// </summary>
        /// <param name="encryptData">Base64 加密字串</param>
        /// <param name="key">32 字元長度金鑰 (256 bits)</param>
        /// <param name="iv">16 字元長度 IV (128 bits)</param>
        /// <param name="err">錯誤訊息</param>
        /// <returns>解密後的文字</returns>
        public static string DecryptAES256(string encryptData, string key, string iv, out string err) {
            err = string.Empty;

            try {
                if (string.IsNullOrEmpty(encryptData))
                    throw new ArgumentNullException(nameof(encryptData), "加密內容不可為空");

                if (key.Length != 32)
                    throw new ArgumentException("金鑰長度必須為 32 字元 (256 bits)", nameof(key));

                if (iv.Length != 16)
                    throw new ArgumentException("IV 長度必須為 16 字元 (128 bits)", nameof(iv));

                byte[] encryptBytes = Convert.FromBase64String(encryptData);
                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                byte[] ivBytes = Encoding.UTF8.GetBytes(iv);

                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                byte[] decrypted = decryptor.TransformFinalBlock(encryptBytes, 0, encryptBytes.Length);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch (Exception ex) {
                err = ex.Message;
                return string.Empty;
            }
        }

        public static string StringToISO_8859_1(string srcText) {
            string dst = "";
            char[] src = srcText.ToCharArray();
            for (int i = 0; i < src.Length; i++) {
                string str = @"&#" + (int)src[i] + ";";
                dst += str;
            }
            return dst;
        }


        /// <summary>
        /// 转换为原始字符串
        /// </summary>
        /// <param name="srcText"></param>
        /// <returns></returns>
        public static string ISO_8859_1ToString(string srcText) {
            string dst = "";
            string[] src = srcText.Split(';');
            for (int i = 0; i < src.Length; i++) {
                if (src[i].Length > 0) {
                    string str = ((char)int.Parse(src[i].Substring(2))).ToString();
                    dst += str;
                }
            }
            return dst;
        }
    }

}