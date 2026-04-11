using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;

namespace Server.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize(Roles = "Admin, Manager")]
	public class DashboardController : ControllerBase
	{
		private readonly AppDbContext _context;
		public DashboardController(AppDbContext context) => _context = context;

		[HttpGet("stats")]
		public async Task<ActionResult<DashboardStatsDto>> GetStats()
		{
			var stats = new DashboardStatsDto
			{
				TotalPois = await _context.POIs.CountAsync(),
				TotalRestaurants = await _context.Restaurants.CountAsync(),
				TotalFoods = await _context.Foods.CountAsync(),
				TotalReviews = await _context.Reviews.CountAsync(),

				// Lấy Top 5 POI có AverageRating cao nhất (Ưu tiên những quán có nhiều Review để công bằng)
				TopPois = await _context.POIs
					.Where(p => p.ReviewCount > 0)
					.OrderByDescending(p => p.AverageRating)
					.ThenByDescending(p => p.ReviewCount)
					.Take(5)
					.Select(p => new TopPoiDto
					{
						PoiId = p.PoiId,
						Name = p.Name,
						AverageRating = p.AverageRating,
						ReviewCount = p.ReviewCount
					}).ToListAsync()
			};

			return Ok(stats);
		}
	}
}