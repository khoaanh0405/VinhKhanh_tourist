// Controllers/UITranslationsController.cs
using Server.Data; // Thay bằng namespace chứa DbContext của bạn
using Server.DTOs;
using Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UITranslationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UITranslationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/UITranslations/vi
        [HttpGet("{langCode}")]
        public async Task<ActionResult<Dictionary<string, string>>> GetTranslations(string langCode)
        {
            // Lấy danh sách bản dịch theo mã ngôn ngữ
            var translations = await _context.UITranslations
                .Where(t => t.LanguageCode == langCode)
                .ToDictionaryAsync(t => t.ResourceKey, t => t.ResourceValue);

            if (translations == null || translations.Count == 0)
            {
                return NotFound(new { Message = "Language not found or no translations available." });
            }

            return Ok(translations);
        }
    }
}