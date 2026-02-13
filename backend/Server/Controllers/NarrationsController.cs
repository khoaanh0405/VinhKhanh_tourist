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

        public NarrationsController(
            INarrationService narrationService,
            ILogger<NarrationsController> logger)
        {
            _narrationService = narrationService;
            _logger = logger;
        }

        /// <summary>
        /// Get all narrations
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<NarrationDto>>> GetAll()
        {
            try
            {
                var narrations = await _narrationService.GetAllNarrationsAsync();
                return Ok(narrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all narrations");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get narration by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<NarrationDto>> GetById(int id)
        {
            try
            {
                var narration = await _narrationService.GetNarrationByIdAsync(id);
                if (narration == null)
                {
                    return NotFound($"Narration with ID {id} not found");
                }
                return Ok(narration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting narration {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get all narrations for a POI
        /// </summary>
        [HttpGet("poi/{poiId}")]
        public async Task<ActionResult<List<NarrationDto>>> GetByPOI(int poiId)
        {
            try
            {
                var narrations = await _narrationService.GetNarrationsByPOIAsync(poiId);
                return Ok(narrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting narrations for POI {poiId}");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get narration by POI and language
        /// </summary>
        [HttpGet("poi/{poiId}/language/{languageCode}")]
        public async Task<ActionResult<NarrationDto>> GetByPOIAndLanguage(
            int poiId,
            string languageCode)
        {
            try
            {
                var narration = await _narrationService
                    .GetNarrationByPOIAndLanguageAsync(poiId, languageCode);

                if (narration == null)
                {
                    return NotFound(
                        $"Narration not found for POI {poiId} and language {languageCode}");
                }

                return Ok(narration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"Error getting narration for POI {poiId} and language {languageCode}");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Create new narration
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<NarrationDto>> Create(
            [FromBody] CreateNarrationDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var narration = await _narrationService.CreateNarrationAsync(dto);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = narration.NarrationId },
                    narration);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating narration");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update narration
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<NarrationDto>> Update(
            int id,
            [FromBody] UpdateNarrationDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var narration = await _narrationService.UpdateNarrationAsync(id, dto);
                return Ok(narration);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating narration {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete narration
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var result = await _narrationService.DeleteNarrationAsync(id);
                if (!result)
                {
                    return NotFound($"Narration with ID {id} not found");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting narration {id}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}