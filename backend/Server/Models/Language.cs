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

        [MaxLength(100)]
        public string LanguageName { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Narration> Narrations { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}