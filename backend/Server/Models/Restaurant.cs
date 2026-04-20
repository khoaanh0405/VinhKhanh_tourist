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

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [Required]
        [ForeignKey("POI")]
        public int PoiId { get; set; }

        public int? ManagerUserId { get; set; }

        // Navigation
        public virtual POI POI { get; set; }
        public virtual ICollection<Food> Foods { get; set; } = new List<Food>();

        [ForeignKey("ManagerUserId")]
        public virtual User? Manager { get; set; }
    }
}