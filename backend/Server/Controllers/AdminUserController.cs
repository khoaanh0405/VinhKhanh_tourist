using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Authorization;

namespace Server.Controllers
{
	[ApiController]
	[Route("api/admin/users")]
	[Authorize(Roles = "Admin")] // Chốt chặn chỉ cho Admin vào
	public class AdminUserController : ControllerBase
	{
		private readonly AppDbContext _db;

		public AdminUserController(AppDbContext db) => _db = db;

		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			var users = await _db.Users
				.Select(u => new { u.UserId, u.DisplayName, u.Username, u.Role, u.CreatedAt })
				.ToListAsync();
			return Ok(users);
		}
	}
}