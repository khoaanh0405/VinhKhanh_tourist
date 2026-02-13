using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("QRCodes")]
    public class QRCode
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int QRCodeId { get; set; }

        [ForeignKey("POI")]
        public int? PoiId { get; set; }

        [MaxLength(200)]
        public string CodeValue { get; set; }

        // Navigation properties
        public virtual POI POI { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}    