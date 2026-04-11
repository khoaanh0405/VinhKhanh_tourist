using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Server.Data;
using Server.Models; // Trỏ đúng vào thư mục Entities của bạn
using Server.DTOs;
using BCrypt.Net;
namespace Server.Services
{
	public interface IAuthService
	{
		Task<LoginResponse?> LoginAsync(LoginRequest request);
	}

	public class AuthService : IAuthService
	{
		private readonly AppDbContext _db;
		private readonly IConfiguration _config;

		public AuthService(AppDbContext db, IConfiguration config)
		{
			_db = db;
			_config = config;
		}

		public async Task<LoginResponse?> LoginAsync(LoginRequest request)
		{
			var user = await _db.Users
				.Include(u => u.ManagedRestaurant)
				.FirstOrDefaultAsync(u => u.Username == request.Username);

			// Kiểm tra mật khẩu (Giả sử bạn dùng BCrypt để mã hóa)
			if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
				return null;

			// Khách du lịch (Tourist) thì không được vào trang Quản trị (Admin/Manager)
			if (user.Role == "Tourist") return null;

			var token = GenerateJwt(user);
			return new LoginResponse(
				token,
				user.Role,
				user.DisplayName,
				user.ManagedRestaurant?.RestaurantId
			);
		}

		private string GenerateJwt(User user)
		{
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var claims = new[]
			{
				new Claim("sub", user.UserId.ToString()),
				new Claim(ClaimTypes.Role, user.Role),
				new Claim("displayName", user.DisplayName),
				new Claim("restaurantId", user.ManagedRestaurant?.RestaurantId.ToString() ?? "")
			};

			var token = new JwtSecurityToken(
				issuer: _config["Jwt:Issuer"],
				audience: _config["Jwt:Audience"],
				claims: claims,
				expires: DateTime.UtcNow.AddHours(8),
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}