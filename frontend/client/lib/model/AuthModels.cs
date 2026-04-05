namespace client.lib.models
{
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        public string DisplayName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class AuthResponse
    {
        public string Message { get; set; }
        public string Token { get; set; } // Dùng cho Login
        public string Role { get; set; }  // Dùng cho Login
        public int? UserId { get; set; }  // Dùng cho Register
    }
}