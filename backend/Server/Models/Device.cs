using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class Device
    {
        [Key]
        [MaxLength(100)]
        public string DeviceId { get; set; } = string.Empty;

        public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ActiveSession? ActiveSession { get; set; }
        public ICollection<QRScanLog> QRScanLogs { get; set; } = new List<QRScanLog>();
    }
}