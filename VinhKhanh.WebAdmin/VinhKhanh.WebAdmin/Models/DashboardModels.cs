namespace VinhKhanh.WebAdmin.Models
{
    public class DashboardStatsDto
    {
        public int TotalVisits { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalQrScans { get; set; }

        // Các chỉ số cũ (Sẽ hiển thị nhỏ lại)
        public int TotalPois { get; set; }
        public int TotalRestaurants { get; set; }
        public int TotalFoods { get; set; }
        public List<TopPoiDto> TopPois { get; set; } = new();
        public List<PoiScanStatDto> PoiScanStats { get; set; } = new();
    }

    public class TopPoiDto
    {
        public int PoiId { get; set; }
        public string Name { get; set; } = "";
    }

    public class PoiScanStatDto
    {
        public string PoiName { get; set; } = "";
        public int ScanCount { get; set; }
    }
}