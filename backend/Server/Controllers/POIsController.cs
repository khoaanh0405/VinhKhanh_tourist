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

        public POIsController(
            AppDbContext context,
            ILogger<POIsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==============================
        // GET ALL POIS
        // ==============================
        [HttpGet]
        public async Task<ActionResult<List<POIDto>>> GetAll()
        {
            try
            {
                var pois = await _context.POIs
                    .AsNoTracking()
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

                        Narrations = p.Narrations.Select(n => new NarrationDto
                        {
                            NarrationId = n.NarrationId,
                            Text = n.Text,
                            LanguageCode = n.LanguageCode,
                            LanguageName = n.Language != null
                                ? n.Language.LanguageName
                                : null,

                            VoiceName = n.VoiceName,
                            SpeechRate = n.SpeechRate,
                            Volume = n.Volume
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
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(pois);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all POIs");
                return StatusCode(500, "Internal server error");
            }
        }

        // ==============================
        // GET POI BY ID
        // ==============================
        [HttpGet("{id}")]
        public async Task<ActionResult<POIDto>> GetById(int id)
        {
            try
            {
                var poi = await _context.POIs
                    .AsNoTracking()
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

                        Narrations = p.Narrations.Select(n => new NarrationDto
                        {
                            NarrationId = n.NarrationId,
                            Text = n.Text,
                            LanguageCode = n.LanguageCode,
                            LanguageName = n.Language != null
                                ? n.Language.LanguageName
                                : null,

                            VoiceName = n.VoiceName,
                            SpeechRate = n.SpeechRate,
                            Volume = n.Volume
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (poi == null)
                    return NotFound($"POI with ID {id} not found");

                return Ok(poi);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting POI {id}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
