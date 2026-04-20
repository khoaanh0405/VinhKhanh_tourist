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

        [Required]
        [ForeignKey("POI")]
        public int PoiId { get; set; }

        [Required]
        public double Radius { get; set; }

        // Navigation
        public virtual POI POI { get; set; }
    }
}