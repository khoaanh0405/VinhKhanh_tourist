using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("UITranslations")]
    public class UITranslation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TranslationId { get; set; }

        [Required]
        [MaxLength(10)]
        public string LanguageCode { get; set; }

        [Required]
        [MaxLength(100)]
        public string ResourceKey { get; set; }

        [Required]
        [MaxLength(500)]
        public string ResourceValue { get; set; }

        // Navigation
        [ForeignKey("LanguageCode")]
        public virtual Language Language { get; set; }
    }
}