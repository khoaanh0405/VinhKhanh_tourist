using System.Collections.Generic;

namespace Server.DTOs
{
    public class POIDto
    {
        public int PoiId { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<string> ImageUrls { get; set; }
        public List<NarrationDto> Narrations { get; set; }
        public List<RestaurantDto> Restaurants { get; set; }
    }

    public class POIWithNarrationDto
    {
        public int PoiId { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? ImageUrl { get; set; }
        public string LanguageCode { get; set; }
        public NarrationDto Narration { get; set; }
    }
}
