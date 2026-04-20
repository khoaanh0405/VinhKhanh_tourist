using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    public class ActiveSession
    {
        [Key]
        [MaxLength(100)]
        public string DeviceId { get; set; } = string.Empty;

        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("DeviceId")]
        public Device Device { get; set; } = null!;
    }
}