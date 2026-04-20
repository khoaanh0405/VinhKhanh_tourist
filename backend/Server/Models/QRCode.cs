namespace Server.Models
{
    public class QRCode
    {
        public int QRCodeId { get; set; }
        public int? PoiId { get; set; }          // <-- ĐÃ ĐỔI THÀNH NULLABLE
        public int? PlaylistId { get; set; }      // <-- MỚI
        public string CodeValue { get; set; } = string.Empty;

        // Navigation
        public POI? POI { get; set; }
        public Playlist? Playlist { get; set; }   // <-- MỚI
    }
}