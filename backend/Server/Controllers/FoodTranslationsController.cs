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
	public class FoodTranslationsController : ControllerBase
	{
		private readonly AppDbContext _context;
		public FoodTranslationsController(AppDbContext context) => _context = context;

		[HttpGet("food/{foodId}")]
		public async Task<IActionResult> GetByFoodId(int foodId)
		{
			var list = await _context.FoodTranslations
				.Where(t => t.FoodId == foodId)
				.Select(t => new FoodTranslationDto
				{
					TranslationId = t.TranslationId,
					FoodId = t.FoodId,
					LanguageCode = t.LanguageCode,
					Name = t.Name,
					Description = t.Description
				}).ToListAsync();
			return Ok(list);
		}

		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CreateFoodTranslationReq req)
		{
			if (await _context.FoodTranslations.AnyAsync(t => t.FoodId == req.FoodId && t.LanguageCode == req.LanguageCode))
				return BadRequest("Ngôn ngữ này đã có bản dịch cho món ăn này!");

			var trans = new FoodTranslation
			{
				FoodId = req.FoodId,
				LanguageCode = req.LanguageCode,
				Name = req.Name,
				Description = req.Description ?? ""
			};
			_context.FoodTranslations.Add(trans);
			await _context.SaveChangesAsync();
			return Ok();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			var trans = await _context.FoodTranslations.FindAsync(id);
			if (trans != null)
			{
				_context.FoodTranslations.Remove(trans);
				await _context.SaveChangesAsync();
			}
			return NoContent();
		}
	}
}