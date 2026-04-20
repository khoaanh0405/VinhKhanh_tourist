namespace Server.DTOs
{
    public class HeartbeatRequest
    {
        public string DeviceId { get; set; } = string.Empty;
    }

    public class ScanQrRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public int PoiId { get; set; }
    }
}