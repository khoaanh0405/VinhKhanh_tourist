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

        public NarrationsController(INarrationService narrationService, ILogger<NarrationsController> logger)
        {
            _narrationService = narrationService;
            _logger = logger;
        }

        // ── GET /api/Narrations ────────────────────────────────────────────
        [HttpGet]
        public async Task<ActionResult<List<NarrationDto>>> GetAll()
            => Ok(await _narrationService.GetAllNarrationsAsync());

        // ── GET /api/Narrations/{id} ───────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<ActionResult<NarrationDto>> GetById(int id)
        {
            var narration = await _narrationService.GetNarrationByIdAsync(id);
            return narration == null ? NotFound($"Narration {id} not found") : Ok(narration);
        }

        // ── GET /api/Narrations/poi/{poiId} ───────────────────────────────
        [HttpGet("poi/{poiId}")]
        public async Task<ActionResult<List<NarrationDto>>> GetByPOI(int poiId)
            => Ok(await _narrationService.GetNarrationsByPOIAsync(poiId));

        // ── GET /api/Narrations/poi/{poiId}/language/{languageCode} ───────
        [HttpGet("poi/{poiId}/language/{languageCode}")]
        public async Task<ActionResult<NarrationDto>> GetByPOIAndLanguage(int poiId, string languageCode)
        {
            var narration = await _narrationService.GetNarrationByPOIAndLanguageAsync(poiId, languageCode);
            return narration == null ? NotFound() : Ok(narration);
        }

        // ── POST /api/Narrations ───────────────────────────────────────────
        // [CHANGED] CreateNarrationDto không còn AudioUrl, UseAudioFile, DurationSeconds
        [HttpPost]
        public async Task<ActionResult<NarrationDto>> Create([FromBody] CreateNarrationDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var created = await _narrationService.CreateNarrationAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.NarrationId }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ── PUT /api/Narrations/{id} ───────────────────────────────────────
        // [CHANGED] UpdateNarrationDto không còn AudioUrl, UseAudioFile, DurationSeconds
        [HttpPut("{id}")]
        public async Task<ActionResult<NarrationDto>> Update(int id, [FromBody] UpdateNarrationDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                return Ok(await _narrationService.UpdateNarrationAsync(id, dto));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // ── DELETE /api/Narrations/{id} ────────────────────────────────────
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
            => await _narrationService.DeleteNarrationAsync(id) ? NoContent() : NotFound();
    }
}