using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("Geofences")]
    public class Geofence
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GeofenceId { get; set; }

        [ForeignKey("POI")]
        public int? PoiId { get; set; }

        public double? Radius { get; set; } // Radius in meters

        // Navigation properties
        public virtual POI POI { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}