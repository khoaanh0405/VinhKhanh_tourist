using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("PoiTranslations")]
    public class PoiTranslation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TranslationId { get; set; }

        [Required]
        public int PoiId { get; set; }

        [Required]
        [MaxLength(10)]
        public string LanguageCode { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; }

        // Navigation properties
        [ForeignKey("PoiId")]
        public virtual POI POI { get; set; }

        [ForeignKey("LanguageCode")]
        public virtual Language Language { get; set; }
    }
}