using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.DTOs;

namespace Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Roles = "Admin, Manager")]
	public class PoiTranslationsController : ControllerBase
	{
		private readonly AppDbContext _context;
		public PoiTranslationsController(AppDbContext context) => _context = context;

		// Lấy danh sách giới thiệu của 1 quán (POI)
		[HttpGet("poi/{poiId}")]
		public async Task<IActionResult> GetByPoiId(int poiId)
		{
			var list = await _context.PoiTranslations
				.Where(t => t.PoiId == poiId)
				.Select(t => new PoiTranslationDto
				{
					TranslationId = t.TranslationId,
					PoiId = t.PoiId,
					LanguageCode = t.LanguageCode,
					Description = t.Description
				}).ToListAsync();
			return Ok(list);
		}

		// Lưu bản dịch (Nếu có rồi thì cập nhật, chưa có thì thêm mới)
		[HttpPost]
		public async Task<IActionResult> Upsert([FromBody] CreatePoiTranslationReq req)
		{
			var existing = await _context.PoiTranslations
				.FirstOrDefaultAsync(t => t.PoiId == req.PoiId && t.LanguageCode == req.LanguageCode);

			if (existing != null)
			{
				existing.Description = req.Description;
			}
			else
			{
				_context.PoiTranslations.Add(new PoiTranslation
				{
					PoiId = req.PoiId,
					LanguageCode = req.LanguageCode,
					Description = req.Description
				});
			}

			await _context.SaveChangesAsync();
			return Ok();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			var trans = await _context.PoiTranslations.FindAsync(id);
			if (trans != null)
			{
				_context.PoiTranslations.Remove(trans);
				await _context.SaveChangesAsync();
			}
			return NoContent();
		}
	}
}