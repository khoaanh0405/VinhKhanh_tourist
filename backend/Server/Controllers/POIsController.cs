using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Models;

namespace Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class POIsController : ControllerBase
	{
		private readonly AppDbContext _context;
		private readonly ILogger<POIsController> _logger;

		public POIsController(AppDbContext context, ILogger<POIsController> logger)
		{
			_context = context;
			_logger = logger;
		}

		[HttpGet]
		public async Task<ActionResult<List<POIDto>>> GetAll([FromQuery] string lang = "vi")
		{
			try
			{
				var pois = await _context.POIs
					.AsNoTracking()
					.AsSplitQuery()
                    .Where(p => !p.Restaurants.Any() || p.Restaurants.Any(r => !r.IsLocked))
					.Select(p => new POIDto
            {
						PoiId = p.PoiId,
						Name = p.Name,
						Description = p.PoiTranslations.FirstOrDefault(t => t.LanguageCode == lang) != null
									  ? p.PoiTranslations.FirstOrDefault(t => t.LanguageCode == lang).Description
									  : p.Description,
						Latitude = p.Latitude,
						Longitude = p.Longitude,
						AverageRating = p.AverageRating,
						ReviewCount = p.ReviewCount,
						ImageUrls = p.PoiImages.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).ToList(),
						Narrations = p.Narrations.Where(n => n.LanguageCode == lang).Select(n => new NarrationDto
						{
							NarrationId = n.NarrationId,
							Text = n.Text,
							LanguageCode = n.LanguageCode,
							AudioUrl = n.AudioUrl,
							UseAudioFile = n.UseAudioFile,
							VoiceName = n.VoiceName
						}).ToList(),
                        Restaurants = p.Restaurants.Where(r => !r.IsLocked).Select(r => new RestaurantDto
                        {
							RestaurantId = r.RestaurantId,
							Name = r.Name,
							Address = r.Address,
							Description = r.RestaurantTranslations.FirstOrDefault(t => t.LanguageCode == lang) != null
										  ? r.RestaurantTranslations.FirstOrDefault(t => t.LanguageCode == lang).Description
										  : r.Description,
							Foods = r.Foods.Select(f => new FoodDto
							{
								FoodId = f.FoodId,
								Price = f.Price,
								Name = f.FoodTranslations.FirstOrDefault(t => t.LanguageCode == lang) != null
									   ? f.FoodTranslations.FirstOrDefault(t => t.LanguageCode == lang).Name
									   : f.Name,
								Description = f.FoodTranslations.FirstOrDefault(t => t.LanguageCode == lang) != null
											  ? f.FoodTranslations.FirstOrDefault(t => t.LanguageCode == lang).Description
											  : f.Description
							}).ToList()
						}).ToList(),
						Reviews = p.Reviews.OrderByDescending(r => r.CreatedAt).Select(rv => new ReviewDto
						{
							ReviewId = rv.ReviewId,
							UserId = rv.UserId,
							Rating = rv.Rating,
							Comment = rv.Comment,
							CreatedAt = rv.CreatedAt
						}).ToList()
					})
					.ToListAsync();

				return Ok(pois);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi lấy danh sách POI");
				return StatusCode(500, "Lỗi server nội bộ");
			}
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<POIDto>> GetById(int id, [FromQuery] string lang = "vi")
		{
			var poi = await _context.POIs
				.AsNoTracking()
				.AsSplitQuery()
				.Where(p => p.PoiId == id)
                .Where(p => p.PoiId == id && (!p.Restaurants.Any() || p.Restaurants.Any(r => !r.IsLocked)))
				.Select(p => new POIDto
        {
					PoiId = p.PoiId,
					Name = p.Name,
					Description = p.PoiTranslations.FirstOrDefault(t => t.LanguageCode == lang) != null ? p.PoiTranslations.FirstOrDefault(t => t.LanguageCode == lang).Description : p.Description,
					Latitude = p.Latitude,
					Longitude = p.Longitude,
					AverageRating = p.AverageRating,
					ReviewCount = p.ReviewCount,
					ImageUrls = p.PoiImages.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).ToList(),
					Narrations = p.Narrations.Where(n => n.LanguageCode == lang).Select(n => new NarrationDto { NarrationId = n.NarrationId, Text = n.Text, LanguageCode = n.LanguageCode, AudioUrl = n.AudioUrl, UseAudioFile = n.UseAudioFile, VoiceName = n.VoiceName }).ToList(),
                    Restaurants = p.Restaurants.Where(r => !r.IsLocked).Select(r => new RestaurantDto
                    {
						RestaurantId = r.RestaurantId,
						Name = r.Name,
						Address = r.Address,
						Description = r.RestaurantTranslations.FirstOrDefault(t => t.LanguageCode == lang) != null ? r.RestaurantTranslations.FirstOrDefault(t => t.LanguageCode == lang).Description : r.Description,
						Foods = r.Foods.Select(f => new FoodDto
						{
							FoodId = f.FoodId,
							Price = f.Price,
							Name = f.FoodTranslations.FirstOrDefault(t => t.LanguageCode == lang) != null ? f.FoodTranslations.FirstOrDefault(t => t.LanguageCode == lang).Name : f.Name,
							Description = f.FoodTranslations.FirstOrDefault(t => t.LanguageCode == lang) != null ? f.FoodTranslations.FirstOrDefault(t => t.LanguageCode == lang).Description : f.Description
						}).ToList()
					}).ToList(),
					Reviews = p.Reviews.OrderByDescending(r => r.CreatedAt).Select(rv => new ReviewDto { ReviewId = rv.ReviewId, UserId = rv.UserId, Rating = rv.Rating, Comment = rv.Comment, CreatedAt = rv.CreatedAt }).ToList()
				})
				.FirstOrDefaultAsync();

			if (poi == null) return NotFound();
			return Ok(poi);
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> CreatePoi([FromBody] CreatePoiAdminRequest req)
		{
			var newPoi = new POI
			{
				Name = req.Name,
				Latitude = req.Latitude,
				Longitude = req.Longitude,
				Description = req.Description,
				AverageRating = 0,
				ReviewCount = 0
			};

			_context.POIs.Add(newPoi);
			await _context.SaveChangesAsync();

			return Ok(new { Message = "Tạo POI thành công", PoiId = newPoi.PoiId });
		}

		[HttpPut("{id}")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> UpdatePoi(int id, [FromBody] UpdatePoiAdminRequest req)
		{
			var poi = await _context.POIs.FindAsync(id);
			if (poi == null) return NotFound("Không tìm thấy POI.");

			poi.Name = req.Name;
			poi.Latitude = req.Latitude;
			poi.Longitude = req.Longitude;
			poi.Description = req.Description;

			await _context.SaveChangesAsync();
			return NoContent();
		}

		[HttpDelete("{id}")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> DeletePoi(int id)
		{
			var poi = await _context.POIs.FindAsync(id);
			if (poi == null) return NotFound("Không tìm thấy POI.");

			_context.POIs.Remove(poi);
			await _context.SaveChangesAsync();
			return NoContent();
		}
	}	
}