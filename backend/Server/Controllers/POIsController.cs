using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;

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
        [HttpGet]
        public async Task<ActionResult<List<POIDto>>> GetAll()
        {
            try
            {
                var pois = await _context.POIs
                    .AsNoTracking()
                    .AsSplitQuery() // Tránh lỗi hiệu suất của Entity Framework
                    .Select(p => new POIDto
                    {
                        PoiId = p.PoiId,
                        Name = p.Name,
                        Latitude = p.Latitude,
                        Longitude = p.Longitude,
                        AverageRating = p.AverageRating, // Điểm đánh giá
                        ReviewCount = p.ReviewCount,     // Số lượt đánh giá

                        // Lấy Ảnh
                        ImageUrls = p.PoiImages.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).ToList(),

                        // Lấy Audio / Text
                        Narrations = p.Narrations.Select(n => new NarrationDto
                        {
                            NarrationId = n.NarrationId,
                            Text = n.Text,
                            LanguageCode = n.LanguageCode,
                            AudioUrl = n.AudioUrl,
                            UseAudioFile = n.UseAudioFile
                        }).ToList(),

                        Restaurants = p.Restaurants.Select(r => new RestaurantDto
                        {
                            RestaurantId = r.RestaurantId,
                            Name = r.Name,
                            Address = r.Address,
                            Description = r.Description,
                            Foods = r.Foods.Select(f => new FoodDto
                            {
                                FoodId = f.FoodId,
                                Name = f.Name,
                                Price = f.Price,
                                Description = f.Description
                            }).ToList()
                        }).ToList(),

                        Reviews = p.Reviews.OrderByDescending(r => r.CreatedAt).Select(rv => new ReviewDto
                        {
                            ReviewId = rv.ReviewId,
                            UserName = rv.UserName,
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
        public async Task<ActionResult<POIDto>> GetById(int id)
        {
            var poi = await _context.POIs
                .AsNoTracking()
                .AsSplitQuery()
                .Where(p => p.PoiId == id)
                .Select(p => new POIDto
                {
                    PoiId = p.PoiId,
                    Name = p.Name,
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    AverageRating = p.AverageRating, 
                    ReviewCount = p.ReviewCount,
                    ImageUrls = p.PoiImages.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).ToList()
                })
                .FirstOrDefaultAsync();

            if (poi == null) return NotFound();
            return Ok(poi);
        }
    }
}