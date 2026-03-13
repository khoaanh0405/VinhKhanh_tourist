using Microsoft.AspNetCore.Mvc;
using Server.DTOs;
using Server.Services;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NarrationsController : ControllerBase
    {
        private readonly INarrationService _narrationService;
        private readonly ILogger<NarrationsController> _logger;

        private static readonly HashSet<string> AllowedAudioExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".ogg", ".m4a", ".aac"
        };

        private const long MaxAudioSizeBytes = 50 * 1024 * 1024; // 50 MB

        public NarrationsController(INarrationService narrationService, ILogger<NarrationsController> logger)
        {
            _narrationService = narrationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<NarrationDto>>> GetAll() => Ok(await _narrationService.GetAllNarrationsAsync());

        [HttpGet("{id}")]
        public async Task<ActionResult<NarrationDto>> GetById(int id)
        {
            var narration = await _narrationService.GetNarrationByIdAsync(id);
            return narration == null ? NotFound($"Narration {id} not found") : Ok(narration);
        }

        [HttpGet("poi/{poiId}")]
        public async Task<ActionResult<List<NarrationDto>>> GetByPOI(int poiId) => Ok(await _narrationService.GetNarrationsByPOIAsync(poiId));

        [HttpGet("poi/{poiId}/language/{languageCode}")]
        public async Task<ActionResult<NarrationDto>> GetByPOIAndLanguage(int poiId, string languageCode)
        {
            var narration = await _narrationService.GetNarrationByPOIAndLanguageAsync(poiId, languageCode);
            return narration == null ? NotFound() : Ok(narration);
        }

        [HttpPost]
        public async Task<ActionResult<NarrationDto>> Create([FromBody] CreateNarrationDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try { return CreatedAtAction(nameof(GetById), new { id = (await _narrationService.CreateNarrationAsync(dto)).NarrationId }, dto); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<NarrationDto>> Update(int id, [FromBody] UpdateNarrationDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try { return Ok(await _narrationService.UpdateNarrationAsync(id, dto)); }
            catch (ArgumentException ex) { return NotFound(ex.Message); }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id) => await _narrationService.DeleteNarrationAsync(id) ? NoContent() : NotFound();

        /// <summary>
        /// Upload file audio lên Cloudinary.
        /// Server tự động bật UseAudioFile = true sau khi upload thành công.
        /// (Cần đảm bảo NarrationService xử lý việc lưu AudioPublicId và xóa file cũ trên Cloudinary).
        /// </summary>
        [HttpPost("{id}/audio")]
        [RequestSizeLimit(MaxAudioSizeBytes)]
        public async Task<ActionResult<NarrationDto>> UploadAudio(int id, [FromForm] IFormFile file, [FromForm] int? durationSeconds)
        {
            if (file == null || file.Length == 0) return BadRequest("Vui lòng chọn file audio.");
            if (file.Length > MaxAudioSizeBytes) return BadRequest($"File vượt quá {MaxAudioSizeBytes / 1024 / 1024} MB.");

            var ext = Path.GetExtension(file.FileName);
            if (!AllowedAudioExtensions.Contains(ext)) return BadRequest($"Định dạng không hỗ trợ.");

            try
            {
                var dto = new NarrationAudioUploadDto
                {
                    NarrationId = id,
                    File = file,
                    DurationSeconds = durationSeconds
                };
                return Ok(await _narrationService.UploadAudioAsync(dto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload audio cho narration {Id}", id);
                return StatusCode(500, "Lỗi server khi upload audio.");
            }
        }

        [HttpPatch("{id}/use-tts")]
        public async Task<ActionResult<NarrationDto>> SwitchToTts(int id)
        {
            try
            {
                var narration = await _narrationService.GetNarrationByIdAsync(id);
                if (narration == null) return NotFound();

                var dto = new UpdateNarrationDto
                {
                    Text = narration.Text,
                    AudioUrl = narration.AudioUrl,
                    // Thêm property AudioPublicId vào UpdateNarrationDto để khi update không bị mất
                    DurationSeconds = narration.DurationSeconds,
                    UseAudioFile = false, // TTS
                    VoiceName = narration.VoiceName,
                    SpeechRate = narration.SpeechRate,
                    Volume = narration.Volume
                };
                return Ok(await _narrationService.UpdateNarrationAsync(id, dto));
            }
            catch (ArgumentException ex) { return NotFound(ex.Message); }
        }

        [HttpPatch("{id}/use-audio")]
        public async Task<ActionResult<NarrationDto>> SwitchToAudio(int id)
        {
            try
            {
                var narration = await _narrationService.GetNarrationByIdAsync(id);
                if (narration == null) return NotFound();
                if (string.IsNullOrEmpty(narration.AudioUrl)) return BadRequest("Chưa có audio file.");

                var dto = new UpdateNarrationDto
                {
                    Text = narration.Text,
                    AudioUrl = narration.AudioUrl,
                    DurationSeconds = narration.DurationSeconds,
                    UseAudioFile = true, // Audio Cloudinary
                    VoiceName = narration.VoiceName,
                    SpeechRate = narration.SpeechRate,
                    Volume = narration.Volume
                };
                return Ok(await _narrationService.UpdateNarrationAsync(id, dto));
            }
            catch (ArgumentException ex) { return NotFound(ex.Message); }
        }
    }
}