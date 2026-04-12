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
        public int UserId { get; set; }
        [Range(1, 5)]
        public int Rating { get; set; }
        [MaxLength(1000)]
        public string? Comment { get; set; }
		public bool IsHidden { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("PoiId")]
        public virtual POI POI { get; set; }
    }
}