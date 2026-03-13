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

        [Column(TypeName = "nvarchar(max)")]
        public string Text { get; set; }

        [Required]
        [ForeignKey("POI")]
        public int PoiId { get; set; }

        [Required]
        [MaxLength(10)]
        [ForeignKey("Language")]
        public string LanguageCode { get; set; }

        [MaxLength(1000)] 
        public string? AudioUrl { get; set; }

        [MaxLength(255)]
        public string? AudioPublicId { get; set; } 

        public int? DurationSeconds { get; set; }

        public bool UseAudioFile { get; set; } = false;

        [MaxLength(100)]
        public string? VoiceName { get; set; }

        public double SpeechRate { get; set; } = 0.5;

        public double Volume { get; set; } = 1.0;

        // Navigation
        public virtual POI POI { get; set; }
        public virtual Language Language { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}