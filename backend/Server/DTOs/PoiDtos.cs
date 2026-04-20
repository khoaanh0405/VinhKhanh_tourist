using System.Collections.Generic;

namespace Server.DTOs
{
    // ─── READ ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Chi tiết đầy đủ của một POI (dùng cho trang Detail).
    /// Description đã bị xoá: bảng POIs không có cột này.
    /// </summary>
    public class POIDto
    {
        public int PoiId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public List<string> ImageUrls { get; set; } = new();
        public List<NarrationDto> Narrations { get; set; } = new();
        public List<RestaurantDto> Restaurants { get; set; } = new();
    }

    /// <summary>
    /// POI kèm một Narration cụ thể (dùng cho danh sách + bản đồ).
    /// </summary>
    public class POIWithNarrationDto
    {
        public int PoiId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? ImageUrl { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
        public NarrationDto? Narration { get; set; }
    }

    public record CreatePoiAdminRequest(string Name, double Latitude, double Longitude);

    public record UpdatePoiAdminRequest(string Name, double Latitude, double Longitude);
}