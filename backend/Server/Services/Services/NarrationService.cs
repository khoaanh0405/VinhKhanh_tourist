using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Models;

namespace Server.Services
{
    public interface INarrationService
    {
        Task<List<NarrationDto>> GetAllNarrationsAsync();
        Task<NarrationDto> GetNarrationByIdAsync(int id);
        Task<List<NarrationDto>> GetNarrationsByPOIAsync(int poiId);
        Task<NarrationDto> GetNarrationByPOIAndLanguageAsync(int poiId, string languageCode);
        Task<NarrationDto> CreateNarrationAsync(CreateNarrationDto dto);
        Task<NarrationDto> UpdateNarrationAsync(int id, UpdateNarrationDto dto);
        Task<bool> DeleteNarrationAsync(int id);
    }

    public class NarrationService : INarrationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<NarrationService> _logger;

        public NarrationService(
            AppDbContext context,
            ILogger<NarrationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<NarrationDto>> GetAllNarrationsAsync()
        {
            return await _context.Narrations
                .Include(n => n.POI)
                .Include(n => n.Language)
                .Select(n => MapToDto(n))
                .ToListAsync();
        }

        public async Task<NarrationDto> GetNarrationByIdAsync(int id)
        {
            var narration = await _context.Narrations
                .Include(n => n.POI)
                .Include(n => n.Language)
                .FirstOrDefaultAsync(n => n.NarrationId == id);

            return narration == null ? null : MapToDto(narration);
        }

        public async Task<List<NarrationDto>> GetNarrationsByPOIAsync(int poiId)
        {
            return await _context.Narrations
                .Include(n => n.POI)
                .Include(n => n.Language)
                .Where(n => n.PoiId == poiId)
                .Select(n => MapToDto(n))
                .ToListAsync();
        }

        public async Task<NarrationDto> GetNarrationByPOIAndLanguageAsync(int poiId, string languageCode)
        {
            var narration = await _context.Narrations
                .Include(n => n.POI)
                .Include(n => n.Language)
                .FirstOrDefaultAsync(n => n.PoiId == poiId && n.LanguageCode == languageCode);

            return narration == null ? null : MapToDto(narration);
        }

        public async Task<NarrationDto> CreateNarrationAsync(CreateNarrationDto dto)
        {
            try
            {
                if (!await _context.POIs.AnyAsync(p => p.PoiId == dto.PoiId))
                    throw new ArgumentException("POI not found");

                if (!await _context.Languages.AnyAsync(l => l.LanguageCode == dto.LanguageCode))
                    throw new ArgumentException("Language not found");

                if (await _context.Narrations
                    .AnyAsync(n => n.PoiId == dto.PoiId && n.LanguageCode == dto.LanguageCode))
                    throw new InvalidOperationException("Narration already exists for this POI and language");

                var narration = new Narration
                {
                    Text = dto.Text,
                    PoiId = dto.PoiId,
                    LanguageCode = dto.LanguageCode,
                    VoiceName = dto.VoiceName,
                    SpeechRate = dto.SpeechRate,
                    Volume = dto.Volume,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Narrations.Add(narration);
                await _context.SaveChangesAsync();

                return await GetNarrationByIdAsync(narration.NarrationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating narration");
                throw;
            }
        }

        public async Task<NarrationDto> UpdateNarrationAsync(int id, UpdateNarrationDto dto)
        {
            try
            {
                var narration = await _context.Narrations.FindAsync(id);
                if (narration == null)
                    throw new ArgumentException("Narration not found");

                narration.Text = dto.Text ?? narration.Text;
                narration.VoiceName = dto.VoiceName ?? narration.VoiceName;
                narration.SpeechRate = dto.SpeechRate;
                narration.Volume = dto.Volume;
                narration.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return await GetNarrationByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating narration {id}");
                throw;
            }
        }

        public async Task<bool> DeleteNarrationAsync(int id)
        {
            try
            {
                var narration = await _context.Narrations.FindAsync(id);
                if (narration == null)
                    return false;

                _context.Narrations.Remove(narration);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting narration {id}");
                return false;
            }
        }

        private NarrationDto MapToDto(Narration narration)
        {
            return new NarrationDto
            {
                NarrationId = narration.NarrationId,
                Text = narration.Text,
                PoiId = narration.PoiId,
                PoiName = narration.POI?.Name,
                LanguageCode = narration.LanguageCode,
                LanguageName = narration.Language?.LanguageName,
                VoiceName = narration.VoiceName,
                SpeechRate = narration.SpeechRate,
                Volume = narration.Volume
            };
        }
    }
}
