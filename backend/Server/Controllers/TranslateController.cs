using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Server.Data;
using Server.Models;

namespace Server.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize(Roles = "Admin, Manager")]
	public class TranslateController : ControllerBase
	{
		private readonly AppDbContext _context;

		public TranslateController(AppDbContext context)
		{
			_context = context;
		}

		// DTOs dùng để nhận dữ liệu từ Frontend
		public class TranslateRequest { public string Text { get; set; } }
		public class TranslateFoodRequest { public string Name { get; set; } public string Description { get; set; } }

		// ==========================================
		// Dịch Tự Động cho Tab Giới Thiệu (PoiTranslations)
		// ==========================================
		[HttpPost("auto-intro/{poiId}")]
		public async Task<IActionResult> AutoTranslateIntro(int poiId, [FromBody] TranslateRequest req)
		{
			// Lấy tất cả ngôn ngữ trừ tiếng Việt (vi)
			var languages = await _context.Languages.Where(l => l.LanguageCode != "vi").ToListAsync();

			foreach (var lang in languages)
			{
				string translatedText = await GoogleTranslate(req.Text, "vi", lang.LanguageCode);

				var existing = await _context.PoiTranslations.FirstOrDefaultAsync(p => p.PoiId == poiId && p.LanguageCode == lang.LanguageCode);
				if (existing != null)
				{
					existing.Description = translatedText;
				}
				else
				{
					_context.PoiTranslations.Add(new PoiTranslation
					{
						PoiId = poiId,
						LanguageCode = lang.LanguageCode,
						Description = translatedText
					});
				}
			}
			await _context.SaveChangesAsync();
			return Ok();
		}

		// ==========================================
		// Dịch Tự Động cho Tab Thuyết minh (Narrations)
		// ==========================================
		[HttpPost("auto-audio/{poiId}")]
		public async Task<IActionResult> AutoTranslateAudio(int poiId, [FromBody] TranslateRequest req)
		{
			var languages = await _context.Languages.Where(l => l.LanguageCode != "vi").ToListAsync();

			foreach (var lang in languages)
			{
				string translatedText = await GoogleTranslate(req.Text, "vi", lang.LanguageCode);

				var existing = await _context.Narrations.FirstOrDefaultAsync(n => n.PoiId == poiId && n.LanguageCode == lang.LanguageCode);
				if (existing != null)
				{
					existing.Text = translatedText;
				}
				else
				{
					// Tự động gán tên giọng đọc TTS AI theo ngôn ngữ
					string voiceName = lang.LanguageCode.ToLower() switch
					{
						"en" => "en-US-Standard-D",
						"ko" => "ko-KR-Standard-A",
						"ja" => "ja-JP-Standard-A",
						"zh" => "cmn-CN-Standard-A",
						_ => "default-voice"
					};

					_context.Narrations.Add(new Narration
					{
						PoiId = poiId,
						LanguageCode = lang.LanguageCode,
						Text = translatedText,
						UseAudioFile = false,
						VoiceName = voiceName
					});
				}
			}
			await _context.SaveChangesAsync();
			return Ok();
		}

		// ==========================================
		// Dịch Tự Động cho Món ăn (FoodTranslations)
		// ==========================================
		[HttpPost("auto-food/{foodId}")]
		public async Task<IActionResult> AutoTranslateFood(int foodId, [FromBody] TranslateFoodRequest req)
		{
			var languages = await _context.Languages.Where(l => l.LanguageCode != "vi").ToListAsync();

			foreach (var lang in languages)
			{
				string translatedName = await GoogleTranslate(req.Name, "vi", lang.LanguageCode);

				string translatedDesc = "";
				if (!string.IsNullOrWhiteSpace(req.Description))
				{
					translatedDesc = await GoogleTranslate(req.Description, "vi", lang.LanguageCode);
				}

				var existing = await _context.FoodTranslations
					.FirstOrDefaultAsync(t => t.FoodId == foodId && t.LanguageCode == lang.LanguageCode);

				if (existing != null)
				{
					existing.Name = translatedName;
					existing.Description = translatedDesc;
				}
				else
				{
					_context.FoodTranslations.Add(new FoodTranslation
					{
						FoodId = foodId,
						LanguageCode = lang.LanguageCode,
						Name = translatedName,
						Description = translatedDesc
					});
				}
			}

			await _context.SaveChangesAsync();
			return Ok();
		}

		// ==========================================
		// Hàm gọi API nội bộ tới Google Translate (Miễn phí hoàn toàn)
		// ==========================================
		private async Task<string> GoogleTranslate(string text, string fromLang, string toLang)
		{
			try
			{
				using var client = new HttpClient();
				string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLang}&tl={toLang}&dt=t&q={Uri.EscapeDataString(text)}";

				var response = await client.GetStringAsync(url);
				using var doc = JsonDocument.Parse(response);

				string fullTranslation = "";
				foreach (var chunk in doc.RootElement[0].EnumerateArray())
				{
					fullTranslation += chunk[0].GetString();
				}

				return string.IsNullOrWhiteSpace(fullTranslation) ? text : fullTranslation;
			}
			catch
			{
				return text; // Lỗi mạng thì trả về nguyên bản tiếng việt
			}
		}
	}
}