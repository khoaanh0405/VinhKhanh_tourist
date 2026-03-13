using Server.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("Restaurants")]
    public class Restaurant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RestaurantId { get; set; }

        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [ForeignKey("POI")]
        public int PoiId { get; set; }

        // Navigation properties
        public virtual POI POI { get; set; }
        public virtual ICollection<Food> Foods { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}