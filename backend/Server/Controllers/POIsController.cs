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
                    .Select(p => new POIDto
                    {
                        PoiId = p.PoiId,
                        Name = p.Name,
                        Latitude = p.Latitude,
                        Longitude = p.Longitude,
                        ImageUrls = p.PoiImages
                                      .OrderBy(i => i.DisplayOrder)
                                      .Select(i => i.ImageUrl)
                                      .ToList(),
                        Narrations = p.Narrations
                                      .Where(n => n.LanguageCode == lang)
                                      .Select(n => new NarrationDto
                                      {
                                          NarrationId = n.NarrationId,
                                          LanguageCode = n.LanguageCode,
                                          Text = n.Text,
                                          VoiceName = n.VoiceName,
                                          SpeechRate = n.SpeechRate,
                                          Volume = n.Volume
                                      })
                                      .ToList(),
                        Restaurants = p.Restaurants
                                       .Select(r => new RestaurantDto
                                       {
                                           RestaurantId = r.RestaurantId,
                                           Name = r.Name,
                                           Address = r.Address,
                                           Foods = r.Foods
                                                           .Select(f => new FoodDto
                                                           {
                                                               FoodId = f.FoodId,
                                                               Price = f.Price,
                                                               Name = f.FoodTranslations
                                                                         .Where(t => t.LanguageCode == lang)
                                                                         .Select(t => t.Name)
                                                                         .FirstOrDefault() ?? f.Name
                                                           })
                                                           .ToList()
                                       })
                                       .ToList()
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

        // ── GET /api/POIs/{id}?lang=vi ─────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<ActionResult<POIDto>> GetById(int id, [FromQuery] string lang = "vi")
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
                    ImageUrls = p.PoiImages
                                  .OrderBy(i => i.DisplayOrder)
                                  .Select(i => i.ImageUrl)
                                  .ToList(),
                    Narrations = p.Narrations
                                  .Where(n => n.LanguageCode == lang)
                                  .Select(n => new NarrationDto
                                  {
                                      NarrationId = n.NarrationId,
                                      LanguageCode = n.LanguageCode,
                                      Text = n.Text,
                                      VoiceName = n.VoiceName,
                                      SpeechRate = n.SpeechRate,
                                      Volume = n.Volume
                                  })
                                  .ToList(),
                    Restaurants = p.Restaurants
                                   .Select(r => new RestaurantDto
                                   {
                                       RestaurantId = r.RestaurantId,
                                       Name = r.Name,
                                       Address = r.Address,
                                       Foods = r.Foods
                                                       .Select(f => new FoodDto
                                                       {
                                                           FoodId = f.FoodId,
                                                           Price = f.Price,
                                                           Name = f.FoodTranslations
                                                                     .Where(t => t.LanguageCode == lang)
                                                                     .Select(t => t.Name)
                                                                     .FirstOrDefault() ?? f.Name
                                                       })
                                                       .ToList()
                                   })
                                   .ToList()
                })
                .FirstOrDefaultAsync();

            if (poi == null) return NotFound();
            return Ok(poi);
        }

        // ── POST /api/POIs  (Admin only) ───────────────────────────────────
        // [CHANGED] Bỏ Description, AverageRating, ReviewCount khỏi request và model
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreatePoi([FromBody] CreatePoiAdminRequest req)
        {
            var newPoi = new POI
            {
                Name = req.Name,
                Latitude = req.Latitude,
                Longitude = req.Longitude
            };

            _context.POIs.Add(newPoi);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Tạo POI thành công", PoiId = newPoi.PoiId });
        }

        // ── PUT /api/POIs/{id}  (Admin only) ──────────────────────────────
        // [CHANGED] Bỏ Description
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePoi(int id, [FromBody] UpdatePoiAdminRequest req)
        {
            var poi = await _context.POIs.FindAsync(id);
            if (poi == null) return NotFound("Không tìm thấy POI.");

            poi.Name = req.Name;
            poi.Latitude = req.Latitude;
            poi.Longitude = req.Longitude;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ── DELETE /api/POIs/{id}  (Admin only) ───────────────────────────
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