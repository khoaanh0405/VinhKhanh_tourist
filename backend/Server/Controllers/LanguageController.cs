using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LanguageController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LanguageController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Language
        // Lấy danh sách tất cả ngôn ngữ hỗ trợ
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Language>>> GetLanguages()
        {
            return await _context.Languages.ToListAsync();
        }

        // POST: api/Language
        // Thêm ngôn ngữ mới (Chỉ Admin/Manager)
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<Language>> PostLanguage(Language language)
        {
            if (await _context.Languages.AnyAsync(l => l.LanguageCode == language.LanguageCode))
            {
                return BadRequest("Mã ngôn ngữ này đã tồn tại.");
            }

            _context.Languages.Add(language);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLanguages), new { id = language.LanguageCode }, language);
        }

        // DELETE: api/Language/en
        [HttpDelete("{code}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteLanguage(string code)
        {
            var language = await _context.Languages.FindAsync(code);
            if (language == null) return NotFound();

            _context.Languages.Remove(language);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}