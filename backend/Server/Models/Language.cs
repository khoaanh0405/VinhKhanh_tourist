using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("Languages")]
    public class Language
    {
        [Key]
        [MaxLength(10)]
        public string LanguageCode { get; set; }

        [MaxLength(100)]
        public string LanguageName { get; set; } = string.Empty;

        // Navigation properties (Cũ)
        public virtual ICollection<Narration> Narrations { get; set; } = new List<Narration>();

        // Navigation properties (Mới thêm)
        public virtual ICollection<PoiTranslation> PoiTranslations { get; set; } = new List<PoiTranslation>();
        public virtual ICollection<RestaurantTranslation> RestaurantTranslations { get; set; } = new List<RestaurantTranslation>();
        public virtual ICollection<FoodTranslation> FoodTranslations { get; set; } = new List<FoodTranslation>();

		[NotMapped]
		public DateTime CreatedAt { get; set; }
	}
}