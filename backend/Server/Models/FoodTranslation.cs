using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("FoodTranslations")]
    public class FoodTranslation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TranslationId { get; set; }

        [Required]
        public int FoodId { get; set; }

        [Required]
        [MaxLength(10)]
        public string LanguageCode { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        // Navigation
        [ForeignKey("FoodId")]
        public virtual Food Food { get; set; }

        [ForeignKey("LanguageCode")]
        public virtual Language Language { get; set; }
    }
}