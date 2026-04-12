using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net; // Thư viện mã hóa
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Data;
using Server.Models;
using Microsoft.AspNetCore.Authorization;

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

		// DTO cho Đăng ký
		public class RegisterRequest
		{
			public string DisplayName { get; set; }
			public string Username { get; set; }
			public string Email { get; set; } // 👈 Gộp của thằng bạn: Thêm Email
			public string Password { get; set; }
			public string Role { get; set; } = "Tourist"; // App cho người dùng nên để mặc định là Tourist
		}

		// DTO cho Đăng nhập
		public class LoginRequest
		{
			public string Username { get; set; }
			public string Password { get; set; }
		}

		// 1. ĐĂNG KÝ (Mã hóa mật khẩu trước khi lưu)
		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] RegisterRequest request)
		{
			if (await _context.Users.AnyAsync(u => u.Username == request.Username))
			{
				return BadRequest("Tên đăng nhập đã tồn tại.");
			}

			// MÃ HÓA MẬT KHẨU (Hashing)
			string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

			var newUser = new User
			{
				DisplayName = request.DisplayName,
				Username = request.Username,
				Email = request.Email, // 👈 Gộp của thằng bạn: Lưu Email vào DB
				Password = passwordHash,
				Role = request.Role,
				CreatedAt = DateTime.UtcNow
			};

			_context.Users.Add(newUser);
			await _context.SaveChangesAsync();

			return Ok(new { Message = "Đăng ký thành công!", UserId = newUser.UserId });
		}

		// 2. ĐĂNG NHẬP (Kiểm tra Hash và trả về JWT)
		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginRequest request)
		{
			// Tìm user trong DB
			var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

			// Kiểm tra: User có tồn tại không? Mật khẩu có khớp với Hash không?
			if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
			{
				return Unauthorized("Sai tên đăng nhập hoặc mật khẩu.");
			}

			// Kiểm tra xem User có bị khóa không (Bảo vệ thêm cho hệ thống)
			if (user.IsLocked)
			{
				return Unauthorized("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin.");
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

		// ==========================================
		// CÁC API DÀNH RIÊNG CHO ADMIN WEB ADMIN
		// ==========================================

		// 4. LẤY DANH SÁCH NGƯỜI DÙNG (Admin Only)
		[HttpGet]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> GetAllUsers()
		{
			// Lấy danh sách nhưng TUYỆT ĐỐI KHÔNG trả về cột Password
			var users = await _context.Users
				.Select(u => new
				{
					u.UserId,
					u.DisplayName,
					u.Username,
					u.Email, // 👈 Gửi kèm Email xuống WebAdmin luôn cho đủ bộ
					u.Role,
					u.CreatedAt,
					u.IsLocked
				})
				.ToListAsync();

			return Ok(users);
		}

		// 5. ADMIN TẠO TÀI KHOẢN (Được phép chọn Role là Manager/Admin)
		[HttpPost("admin-create")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> CreateUserByAdmin([FromBody] RegisterRequest request)
		{
			if (await _context.Users.AnyAsync(u => u.Username == request.Username))
			{
				return BadRequest("Tên đăng nhập đã tồn tại.");
			}

			// Mã hóa mật khẩu
			string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

			var newUser = new User
			{
				DisplayName = request.DisplayName,
				Username = request.Username,
				Email = request.Email, // 👈 Lưu Email khi Admin tạo tài khoản
				Password = passwordHash,
				Role = request.Role,
				CreatedAt = DateTime.UtcNow
			};

			_context.Users.Add(newUser);
			await _context.SaveChangesAsync();

			return Ok(new { Message = "Tạo tài khoản thành công!", UserId = newUser.UserId });
		}

		// DTO nhận mật khẩu mới
		public class AdminChangePasswordRequest
		{
			public string NewPassword { get; set; }
		}

		// 6. ADMIN ĐỔI MẬT KHẨU NGƯỜI DÙNG
		[HttpPut("admin-change-password/{id}")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> AdminChangePassword(int id, [FromBody] AdminChangePasswordRequest req)
		{
			var user = await _context.Users.FindAsync(id);
			if (user == null) return NotFound("Không tìm thấy người dùng.");

			// Mã hóa mật khẩu mới và lưu lại
			user.Password = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
			await _context.SaveChangesAsync();

			return Ok(new { Message = "Đổi mật khẩu thành công!" });
		}

		// API Test thử xem Token có hoạt động không
		[HttpGet("me")]
		[Authorize] // Yêu cầu phải có Token mới vào được
		public IActionResult GetMyInfo()
		{
			var username = User.Identity.Name;
			var role = User.FindFirst(ClaimTypes.Role)?.Value;
			return Ok(new { Message = $"Xin chào {username}, bạn là {role}" });
		}

		// 7. KHÓA / MỞ KHÓA TÀI KHOẢN
		[HttpPut("{id}/toggle-lock")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> ToggleLock(int id)
		{
			var user = await _context.Users.FindAsync(id);
			if (user == null) return NotFound();

			// Không cho Admin tự khóa chính mình (để tránh bị kẹt)
			var currentAdminId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
			if (user.UserId == currentAdminId) return BadRequest("Bạn không thể tự khóa chính mình.");

			user.IsLocked = !user.IsLocked; // Đảo ngược trạng thái
			await _context.SaveChangesAsync();

			return Ok(new { isLocked = user.IsLocked });
		}
	}
}