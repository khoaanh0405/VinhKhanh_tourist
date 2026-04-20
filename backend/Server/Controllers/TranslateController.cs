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

        // DTOs
        public class TranslateRequest { public string Text { get; set; } }
        public class TranslateFoodRequest { public string Name { get; set; } }
        [HttpPost("auto-audio/{poiId}")]
        public async Task<IActionResult> AutoTranslateAudio(int poiId, [FromBody] TranslateRequest req)
        {
            var languages = await _context.Languages
                .Where(l => l.LanguageCode != "vi")
                .ToListAsync();

            foreach (var lang in languages)
            {
                string translatedText = await GoogleTranslate(req.Text, "vi", lang.LanguageCode);

                var existing = await _context.Narrations
                    .FirstOrDefaultAsync(n => n.PoiId == poiId && n.LanguageCode == lang.LanguageCode);

                if (existing != null)
                {
                    existing.Text = translatedText;
                }
                else
                {
                    // Gán tên giọng TTS theo ngôn ngữ
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
                        VoiceName = voiceName,
                        SpeechRate = 0.5,
                        Volume = 1.0
                        // [REMOVED] UseAudioFile — cột đã xóa khỏi Narrations
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpPost("auto-food/{foodId}")]
        public async Task<IActionResult> AutoTranslateFood(int foodId, [FromBody] TranslateFoodRequest req)
        {
            var languages = await _context.Languages
                .Where(l => l.LanguageCode != "vi")
                .ToListAsync();

            foreach (var lang in languages)
            {
                string translatedName = await GoogleTranslate(req.Name, "vi", lang.LanguageCode);

                var existing = await _context.FoodTranslations
                    .FirstOrDefaultAsync(t => t.FoodId == foodId && t.LanguageCode == lang.LanguageCode);

                if (existing != null)
                {
                    existing.Name = translatedName;
                    // [REMOVED] existing.Description — cột đã xóa
                }
                else
                {
                    _context.FoodTranslations.Add(new FoodTranslation
                    {
                        FoodId = foodId,
                        LanguageCode = lang.LanguageCode,
                        Name = translatedName
                        // [REMOVED] Description — cột đã xóa khỏi FoodTranslations
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("auto-ui/{targetLang}")]
        public async Task<IActionResult> AutoTranslateUI(string targetLang)
        {
            string sourceLang = "vi";

            if (targetLang.ToLower() == sourceLang)
            {
                return BadRequest("Không thể dịch từ Tiếng Việt sang Tiếng Việt.");
            }

            try
            {
                // 1. Lấy toàn bộ bộ từ điển gốc (Tiếng Việt)
                var sourceTranslations = await _context.UITranslations
                    .Where(t => t.LanguageCode == sourceLang)
                    .ToListAsync();

                if (!sourceTranslations.Any())
                {
                    return BadRequest("Chưa có dữ liệu gốc (vi) trong database để làm mẫu dịch.");
                }

                // 2. Lấy danh sách các Key đã được dịch sang targetLang (để tránh dịch đè)
                var existingKeys = await _context.UITranslations
                    .Where(t => t.LanguageCode == targetLang)
                    .Select(t => t.ResourceKey)
                    .ToListAsync();

                // 3. Lọc ra những dòng Tiếng Việt chưa được dịch sang ngôn ngữ mới
                var keysToTranslate = sourceTranslations
                    .Where(t => !existingKeys.Contains(t.ResourceKey))
                    .ToList();

                if (!keysToTranslate.Any())
                {
                    return Ok($"Ngôn ngữ '{targetLang}' đã được dịch đầy đủ UI.");
                }

                var newTranslations = new List<UITranslation>();

                // 4. Vòng lặp gọi GoogleTranslate có sẵn trong file của bạn
                foreach (var item in keysToTranslate)
                {
                    // Truyền vào: Chữ cần dịch, ngôn ngữ gốc (vi), ngôn ngữ đích (targetLang)
                    string translatedText = await GoogleTranslate(item.ResourceValue, sourceLang, targetLang);

                    newTranslations.Add(new UITranslation
                    {
                        LanguageCode = targetLang,
                        ResourceKey = item.ResourceKey,
                        ResourceValue = translatedText
                    });
                }

                // 5. Lưu vào Database
                _context.UITranslations.AddRange(newTranslations);
                await _context.SaveChangesAsync();

                return Ok($"Tuyệt vời! Đã dịch tự động thành công {newTranslations.Count} từ khóa UI sang '{targetLang}'.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi Server khi dịch UI tự động: " + ex.Message);
            }
        }

        // ── Internal: Gọi Google Translate miễn phí ────────────────────────
        private async Task<string> GoogleTranslate(string text, string fromLang, string toLang)
        {
            try
            {
                using var client = new HttpClient();
                string url = $"https://translate.googleapis.com/translate_a/single" +
                             $"?client=gtx&sl={fromLang}&tl={toLang}&dt=t&q={Uri.EscapeDataString(text)}";

                var response = await client.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);

                string fullTranslation = "";
                foreach (var chunk in doc.RootElement[0].EnumerateArray())
                    fullTranslation += chunk[0].GetString();

                return string.IsNullOrWhiteSpace(fullTranslation) ? text : fullTranslation;
            }
            catch
            {
                return text; // fallback: trả về nguyên bản
            }
        }

        [HttpPost("auto-all/{targetLang}")]
        public async Task<IActionResult> AutoTranslateAll(string targetLang)
        {
            string sourceLang = "vi";

            if (targetLang.ToLower() == sourceLang)
                return BadRequest("Không thể dịch từ Tiếng Việt sang Tiếng Việt.");

            try
            {
                int countUI = 0, countFood = 0, countNarration = 0;
                string targetLangLower = targetLang.ToLower();

                // Lấy mã giọng đọc chuẩn từ file Dictionary, nếu không có thì gán mặc định để hệ thống app tự bỏ qua
                if (!GoogleLanguages.VoiceMappings.TryGetValue(targetLangLower, out string voiceName))
                {
                    voiceName = "en-US-Standard-A"; // Fallback an toàn
                }

                // ==========================================
                // 1. DỊCH UI (GIAO DIỆN)
                // ==========================================
                var sourceUI = await _context.UITranslations.Where(t => t.LanguageCode == sourceLang).ToListAsync();
                var existingUIKeys = await _context.UITranslations.Where(t => t.LanguageCode == targetLang).Select(t => t.ResourceKey).ToListAsync();

                var uiToTranslate = sourceUI.Where(t => !existingUIKeys.Contains(t.ResourceKey)).ToList();
                foreach (var item in uiToTranslate)
                {
                    _context.UITranslations.Add(new UITranslation
                    {
                        LanguageCode = targetLang,
                        ResourceKey = item.ResourceKey,
                        ResourceValue = await GoogleTranslate(item.ResourceValue, sourceLang, targetLang)
                    });
                    countUI++;
                }

                // ==========================================
                // 2. DỊCH TÊN MÓN ĂN
                // ==========================================
                var allFoods = await _context.Foods.ToListAsync();
                var existingFoodIds = await _context.FoodTranslations.Where(t => t.LanguageCode == targetLang).Select(t => t.FoodId).ToListAsync();

                var foodsToTranslate = allFoods.Where(f => !existingFoodIds.Contains(f.FoodId)).ToList();
                foreach (var food in foodsToTranslate)
                {
                    _context.FoodTranslations.Add(new FoodTranslation
                    {
                        FoodId = food.FoodId,
                        LanguageCode = targetLang,
                        Name = await GoogleTranslate(food.Name, sourceLang, targetLang)
                        // Đã tuân thủ schema chuẩn: không lưu cột Description
                    });
                    countFood++;
                }

                // ==========================================
                // 3. DỊCH BÀI THUYẾT MINH (NARRATIONS PURE-TTS)
                // ==========================================
                var sourceNarrations = await _context.Narrations.Where(n => n.LanguageCode == sourceLang).ToListAsync();
                var existingNarrationPoiIds = await _context.Narrations.Where(n => n.LanguageCode == targetLang).Select(n => n.PoiId).ToListAsync();

                var narrationsToTranslate = sourceNarrations.Where(n => !existingNarrationPoiIds.Contains(n.PoiId)).ToList();
                foreach (var nar in narrationsToTranslate)
                {
                    _context.Narrations.Add(new Narration
                    {
                        PoiId = nar.PoiId,
                        LanguageCode = targetLang,
                        Text = await GoogleTranslate(nar.Text, sourceLang, targetLang),
                        VoiceName = voiceName, // Gán chuẩn giọng từ file GoogleLanguages
                        SpeechRate = nar.SpeechRate,
                        Volume = nar.Volume,
                        CreatedAt = DateTime.UtcNow
                        // Đã tuân thủ schema chuẩn: Bỏ AudioUrl, UseAudioFile
                    });
                    countNarration++;
                }

                await _context.SaveChangesAsync();

                return Ok($"Đồng bộ thành công! Đã dịch {countUI} từ khóa UI, {countFood} món ăn, và {countNarration} bài thuyết minh sang mã '{targetLang}'.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi Server khi dịch toàn tập: " + ex.Message);
            }
        }
    }
}