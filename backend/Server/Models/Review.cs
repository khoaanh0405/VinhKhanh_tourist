using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("Reviews")]
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }
        public int PoiId { get; set; }
        [MaxLength(100)]
        public string UserName { get; set; } = "Khách ẩn danh";
        public int Rating { get; set; }
        [MaxLength(1000)]
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("PoiId")]
        public virtual POI POI { get; set; }
    }
}