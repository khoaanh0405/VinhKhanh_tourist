using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    public class QRScanLog
    {
        [Key]
        public int LogId { get; set; }

        [MaxLength(100)]
        public string DeviceId { get; set; } = string.Empty;

        public int PoiId { get; set; }

        public DateTime ScannedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("DeviceId")]
        public Device Device { get; set; } = null!;
    }
}