using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Models;

namespace Server.Services
{
    public interface IPlaylistService
    {
        Task<List<PlaylistSummaryDto>> GetAllAsync();
        Task<List<PlaylistItemDetailDto>> GetItemsAsync(int playlistId);
        Task<Playlist> CreateAsync(CreatePlaylistDto dto);
        Task<bool> UpdateAsync(int playlistId, CreatePlaylistDto dto);
        Task<bool> DeleteAsync(int playlistId);
    }

    public class PlaylistService : IPlaylistService
    {
        private readonly AppDbContext _db;

        public PlaylistService(AppDbContext db) => _db = db;

        public async Task<List<PlaylistSummaryDto>> GetAllAsync()
            => await _db.Playlists
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PlaylistSummaryDto(
                    p.PlaylistId,
                    p.Title,
                    p.Items.Count,
                    p.CreatedAt
                ))
                .ToListAsync();
        public async Task<List<PlaylistItemDetailDto>> GetItemsAsync(int playlistId)
        {
            var exists = await _db.Playlists.AnyAsync(p => p.PlaylistId == playlistId);
            if (!exists) return new List<PlaylistItemDetailDto>();

            return await _db.PlaylistItems
                .Where(pi => pi.PlaylistId == playlistId)
                .OrderBy(pi => pi.DisplayOrder)
                .Include(pi => pi.POI)
                    .ThenInclude(poi => poi.Restaurants) // Navigation cần có trong POI model
                .Select(pi => new PlaylistItemDetailDto(
                    pi.PoiId,
                    pi.POI.Name,
                    pi.POI.Restaurants.FirstOrDefault() != null
                        ? pi.POI.Restaurants.First().Name
                        : null,
                    pi.POI.Restaurants.FirstOrDefault() != null
                        ? pi.POI.Restaurants.First().Address
                        : null,
                    pi.POI.Latitude,
                    pi.POI.Longitude,
                    pi.DisplayOrder
                ))
                .ToListAsync();
        }

        // ── Tạo Playlist mới (kèm các PlaylistItem) ──────────────────
        public async Task<Playlist> CreateAsync(CreatePlaylistDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("Tên Playlist không được để trống.");

            if (dto.PoiIds == null || dto.PoiIds.Count == 0)
                throw new ArgumentException("Playlist phải có ít nhất một địa điểm.");

            // Validate các PoiId có tồn tại không
            var validPoiIds = await _db.POIs
                .Where(p => dto.PoiIds.Contains(p.PoiId))
                .Select(p => p.PoiId)
                .ToListAsync();

            var invalidIds = dto.PoiIds.Except(validPoiIds).ToList();
            if (invalidIds.Any())
                throw new ArgumentException($"PoiId không tồn tại: {string.Join(", ", invalidIds)}");

            var playlist = new Playlist
            {
                Title = dto.Title.Trim(),
                CreatedAt = DateTime.UtcNow,
                Items = dto.PoiIds
                    .Select((poiId, index) => new PlaylistItem
                    {
                        PoiId = poiId,
                        DisplayOrder = index + 1
                    })
                    .ToList()
            };

            _db.Playlists.Add(playlist);
            await _db.SaveChangesAsync();
            return playlist;
        }

        // ── Cập nhật Playlist (Sửa tên và danh sách địa điểm) ────────────
        public async Task<bool> UpdateAsync(int playlistId, CreatePlaylistDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("Tên Playlist không được để trống.");

            if (dto.PoiIds == null || dto.PoiIds.Count == 0)
                throw new ArgumentException("Playlist phải có ít nhất một địa điểm.");

            // Lấy playlist hiện tại kèm theo danh sách các địa điểm bên trong
            var playlist = await _db.Playlists
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PlaylistId == playlistId);

            if (playlist == null)
                return false;

            // Validate các PoiId mới (giống như lúc Create)
            var validPoiIds = await _db.POIs
                .Where(p => dto.PoiIds.Contains(p.PoiId))
                .Select(p => p.PoiId)
                .ToListAsync();

            var invalidIds = dto.PoiIds.Except(validPoiIds).ToList();
            if (invalidIds.Any())
                throw new ArgumentException($"PoiId không tồn tại: {string.Join(", ", invalidIds)}");

            // 1. Cập nhật tên mới
            playlist.Title = dto.Title.Trim();

            // 2. Xóa toàn bộ các item (địa điểm) cũ của playlist này
            _db.PlaylistItems.RemoveRange(playlist.Items);

            // 3. Gắn danh sách item mới vào
            playlist.Items = dto.PoiIds
                .Select((poiId, index) => new PlaylistItem
                {
                    PoiId = poiId,
                    DisplayOrder = index + 1
                })
                .ToList();

            // 4. Lưu thay đổi xuống Database
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int playlistId)
        {
            var playlist = await _db.Playlists.FindAsync(playlistId);
            if (playlist == null) return false;

            _db.Playlists.Remove(playlist);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}