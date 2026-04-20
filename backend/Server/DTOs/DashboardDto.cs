namespace Server.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalVisits { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalQrScans { get; set; }
        public int TotalPois { get; set; }
        public int TotalRestaurants { get; set; }
        public int TotalFoods { get; set; }

        // Thêm trường này cho DashboardController
        public int TotalNarrations { get; set; }

        public List<PoiScanStatDto> PoiScanStats { get; set; } = new();

        public class PoiScanStatDto
        {
            public string PoiName { get; set; } = "";
            public int ScanCount { get; set; }
        }
    }
}