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

        // ── READ ────────────────────────────────────────────────────────────

        public async Task<List<NarrationDto>> GetAllNarrationsAsync() =>
            await _context.Narrations
                .Include(n => n.POI)
                .Include(n => n.Language)
                .Select(n => MapToDto(n))
                .ToListAsync();

        public async Task<NarrationDto> GetNarrationByIdAsync(int id)
        {
            var n = await _context.Narrations
                .Include(n => n.POI)
                .Include(n => n.Language)
                .FirstOrDefaultAsync(n => n.NarrationId == id);
            return n == null ? null : MapToDto(n);
        }

        public async Task<List<NarrationDto>> GetNarrationsByPOIAsync(int poiId) =>
            await _context.Narrations
                .Include(n => n.POI)
                .Include(n => n.Language)
                .Where(n => n.PoiId == poiId)
                .Select(n => MapToDto(n))
                .ToListAsync();

        public async Task<NarrationDto> GetNarrationByPOIAndLanguageAsync(int poiId, string languageCode)
        {
            var n = await _context.Narrations
                .Include(n => n.POI)
                .Include(n => n.Language)
                .FirstOrDefaultAsync(n => n.PoiId == poiId && n.LanguageCode == languageCode);
            return n == null ? null : MapToDto(n);
        }

        // ── CREATE ───────────────────────────────────────────────────────────

        public async Task<NarrationDto> CreateNarrationAsync(CreateNarrationDto dto)
        {
            if (!await _context.POIs.AnyAsync(p => p.PoiId == dto.PoiId))
                throw new ArgumentException("POI not found");

            if (!await _context.Languages.AnyAsync(l => l.LanguageCode == dto.LanguageCode))
                throw new ArgumentException("Language not found");

            if (await _context.Narrations.AnyAsync(n => n.PoiId == dto.PoiId && n.LanguageCode == dto.LanguageCode))
                throw new InvalidOperationException("Narration already exists for this POI and language");

            var narration = new Narration
            {
                PoiId = dto.PoiId,
                LanguageCode = dto.LanguageCode,
                Text = dto.Text,
                VoiceName = dto.VoiceName,
                SpeechRate = dto.SpeechRate,
                Volume = dto.Volume,
                CreatedAt = DateTime.UtcNow
            };

            _context.Narrations.Add(narration);
            await _context.SaveChangesAsync();
            return await GetNarrationByIdAsync(narration.NarrationId);
        }

        // ── UPDATE ───────────────────────────────────────────────────────────

        public async Task<NarrationDto> UpdateNarrationAsync(int id, UpdateNarrationDto dto)
        {
            var narration = await _context.Narrations.FindAsync(id)
                ?? throw new ArgumentException("Narration not found");

            narration.Text = dto.Text ?? narration.Text;
            narration.VoiceName = dto.VoiceName ?? narration.VoiceName;
            narration.SpeechRate = dto.SpeechRate;
            narration.Volume = dto.Volume;
            // narration.UpdatedAt = DateTime.UtcNow; // Nếu bạn thêm UpdateAt trong DB thì mở ra, hiện Data ko có

            await _context.SaveChangesAsync();
            return await GetNarrationByIdAsync(id);
        }

        // ── DELETE ───────────────────────────────────────────────────────────

        public async Task<bool> DeleteNarrationAsync(int id)
        {
            var narration = await _context.Narrations.FindAsync(id);
            if (narration == null) return false;

            _context.Narrations.Remove(narration);
            await _context.SaveChangesAsync();
            return true;
        }

        // ── HELPER ───────────────────────────────────────────────────────────

        private static NarrationDto MapToDto(Narration n) => new()
        {
            NarrationId = n.NarrationId,
            PoiId = n.PoiId,
            PoiName = n.POI?.Name,
            Text = n.Text,
            LanguageCode = n.LanguageCode,
            LanguageName = n.Language?.LanguageName,
            VoiceName = n.VoiceName,
            SpeechRate = n.SpeechRate,
            Volume = n.Volume
        };
    }
}