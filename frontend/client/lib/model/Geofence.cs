using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace client.lib.model
{
    public class Geofence
    {
        [JsonPropertyName("geofenceId")]
        public int GeofenceId { get; set; }

        [JsonPropertyName("poiId")]
        public int PoiId { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("radius")]
        public double Radius { get; set; }
    }
}
