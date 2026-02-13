using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Server.Data;
using Server.Models;
using BCrypt.Net; // Thư viện mã hóa

namespace VServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration; // Để đọc Key từ appsettings

        public UserController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // DTO cho Register & Login
        public class AuthRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Role { get; set; } = "Manager"; // Mặc định là Manager
        }

        // 1. ĐĂNG KÝ (Mã hóa mật khẩu trước khi lưu)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest("Tên đăng nhập đã tồn tại.");
            }

            // MÃ HÓA MẬT KHẨU (Hashing)
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new User
            {
                Username = request.Username,
                Password = passwordHash, // Lưu chuỗi mã hóa vào DB, KHÔNG lưu pass thật
                Role = request.Role,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đăng ký thành công!", UserId = newUser.UserId });
        }

        // 2. ĐĂNG NHẬP (Kiểm tra Hash và trả về JWT)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthRequest request)
        {
            // Tìm user trong DB
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

            // Kiểm tra: User có tồn tại không? Mật khẩu có khớp với Hash không?
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                return Unauthorized("Sai tên đăng nhập hoặc mật khẩu.");
            }

            // Nếu đúng -> Tạo Token
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                Message = "Đăng nhập thành công",
                Token = token,
                Role = user.Role
            });
        }

        // Hàm tạo Token JWT
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()), // ID người dùng
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role) // Lưu quyền vào Token
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(24), // Token hết hạn sau 24h
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // API Test thử xem Token có hoạt động không
        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize] // Yêu cầu phải có Token mới vào được
        public IActionResult GetMyInfo()
        {
            var username = User.Identity.Name;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return Ok(new { Message = $"Xin chào {username}, bạn là {role}" });
        }
    }
}