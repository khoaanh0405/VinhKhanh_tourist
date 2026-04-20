using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("Narrations")]
    public class Narration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NarrationId { get; set; }

        [Required]
        [ForeignKey("POI")]
        public int PoiId { get; set; }

        [Required]
        [MaxLength(10)]
        [ForeignKey("Language")]
        public string LanguageCode { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Text { get; set; }

        [MaxLength(100)]
        public string? VoiceName { get; set; }

        public double SpeechRate { get; set; } = 0.5;

        public double Volume { get; set; } = 1.0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual POI POI { get; set; }
        public virtual Language Language { get; set; }
    }
}