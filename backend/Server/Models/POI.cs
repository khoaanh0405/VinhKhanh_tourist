using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Server.Models;

namespace Server.Models
{
    [Table("POIs")]
    public class POI
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PoiId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }
        public double AverageRating { get; set; } = 0.0;
        public int ReviewCount { get; set; } = 0;

        // 1 - N Images
        public virtual ICollection<PoiImage> PoiImages { get; set; } = new List<PoiImage>();

        // Navigation properties
        public virtual ICollection<Restaurant> Restaurants { get; set; } = new List<Restaurant>();
        public virtual ICollection<Narration> Narrations { get; set; } = new List<Narration>();
        public virtual ICollection<Geofence> Geofences { get; set; } = new List<Geofence>();
        public virtual ICollection<QRCode> QRCodes { get; set; } = new List<QRCode>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
