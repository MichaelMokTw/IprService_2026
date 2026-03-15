namespace MyAPI.Models {
    public class UserLoginModel {
        public string UserID { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string AuthType { get; set; } = string.Empty;
    }
}
