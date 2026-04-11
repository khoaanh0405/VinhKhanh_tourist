using Microsoft.AspNetCore.Mvc;
using Server.Services;
using Server.DTOs;

namespace Server.Controllers
{
	[ApiController]
	[Route("api/auth")]
	public class AuthController : ControllerBase
	{
		private readonly IAuthService _authService;

		public AuthController(IAuthService authService)
			=> _authService = authService;

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginRequest request)
		{
			var result = await _authService.LoginAsync(request);
			if (result == null)
				return Unauthorized(new { message = "Sai tài khoản/mật khẩu hoặc không có quyền quản trị." });

			return Ok(result);
		}
	}
}