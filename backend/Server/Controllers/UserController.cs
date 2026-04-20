using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Data;
using Server.Models;
using Microsoft.AspNetCore.Authorization;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public UserController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // DTOs
        public class RegisterRequest
        {
            public string DisplayName { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string Role { get; set; } = "Tourist";
        }

        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        public class AdminChangePasswordRequest
        {
            public string NewPassword { get; set; }
        }

        // ── POST /api/User/register ────────────────────────────────────────
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return BadRequest("Tên đăng nhập đã tồn tại.");

            var newUser = new User
            {
                DisplayName = request.DisplayName,
                Username = request.Username,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đăng ký thành công!", UserId = newUser.UserId });
        }

        // ── POST /api/User/login ───────────────────────────────────────────
        // [CHANGED] Bỏ kiểm tra IsLocked — cột đã xóa khỏi bảng Users
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                return Unauthorized("Sai tên đăng nhập hoặc mật khẩu.");

            // [REMOVED] if (user.IsLocked) — cột không còn trong schema mới

            var token = GenerateJwtToken(user);
            return Ok(new { Message = "Đăng nhập thành công", Token = token, Role = user.Role });
        }

        // ── GET /api/User  (Admin only) ────────────────────────────────────
        // [CHANGED] Bỏ IsLocked khỏi projection
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.UserId,
                    u.DisplayName,
                    u.Username,
                    u.Email,
                    u.Role,
                    u.CreatedAt
                    // [REMOVED] u.IsLocked — cột đã xóa khỏi Users
                })
                .ToListAsync();

            return Ok(users);
        }

        // ── POST /api/User/admin-create  (Admin only) ─────────────────────
        [HttpPost("admin-create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUserByAdmin([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return BadRequest("Tên đăng nhập đã tồn tại.");

            var newUser = new User
            {
                DisplayName = request.DisplayName,
                Username = request.Username,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Tạo tài khoản thành công!", UserId = newUser.UserId });
        }

        // ── PUT /api/User/admin-change-password/{id}  (Admin only) ────────
        [HttpPut("admin-change-password/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminChangePassword(int id, [FromBody] AdminChangePasswordRequest req)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            user.Password = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đổi mật khẩu thành công!" });
        }

        // ── GET /api/User/me  (Authorized) ────────────────────────────────
        [HttpGet("me")]
        [Authorize]
        public IActionResult GetMyInfo()
        {
            var username = User.Identity?.Name;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return Ok(new { Message = $"Xin chào {username}, bạn là {role}" });
        }

        // ── [REMOVED] PUT /{id}/toggle-lock ───────────────────────────────
        // Lý do: Cột IsLocked đã bị xóa khỏi bảng Users trong schema mới.
        //        Quản lý khóa tài khoản có thể thực hiện qua soft-delete hoặc Role nếu cần sau này.

        // ─────────────────────────────────────────────────────────────────
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name,             user.Username),
                new Claim(ClaimTypes.Role,             user.Role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(24),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(
                                         new SymmetricSecurityKey(key),
                                         SecurityAlgorithms.HmacSha256)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }
    }
}