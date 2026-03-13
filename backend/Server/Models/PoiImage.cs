using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("PoiImages")]
    public class PoiImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ImageId { get; set; }

        [Required]
        public int PoiId { get; set; }

        [Required]
        [MaxLength(1000)] // Cập nhật độ dài để chứa URL Cloudinary dài
        public string ImageUrl { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? PublicId { get; set; } 

        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        [ForeignKey("PoiId")]
        public virtual POI? POI { get; set; }
    }
}