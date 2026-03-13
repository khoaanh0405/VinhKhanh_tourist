using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("Foods")]
    public class Food
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FoodId { get; set; }

        [MaxLength(200)]
        public string Name { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Price { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [ForeignKey("Restaurant")]
        public int RestaurantId { get; set; }

        // Navigation properties
        public virtual Restaurant Restaurant { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}