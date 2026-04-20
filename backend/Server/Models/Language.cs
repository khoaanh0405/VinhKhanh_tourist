using Server.Models;
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

        [Required]
        [MaxLength(100)]
        public string LanguageName { get; set; } = string.Empty;

        // Navigation
        public virtual ICollection<Narration> Narrations { get; set; } = new List<Narration>();
        public virtual ICollection<FoodTranslation> FoodTranslations { get; set; } = new List<FoodTranslation>();
        public virtual ICollection<UITranslation> UITranslations { get; set; } = new List<UITranslation>();
    }
}