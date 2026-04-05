using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace client.lib.model
{
    public partial class POI : ObservableObject
    {
        [JsonPropertyName("poiId")]
        public int PoiId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
        [JsonPropertyName("averageRating")]
        public double AverageRating { get; set; }

        [JsonPropertyName("reviewCount")]
        public int ReviewCount { get; set; }

        // Danh sách link ảnh từ API Cloudinary trả về
        [JsonPropertyName("imageUrls")]
        public List<string> ImageUrls { get; set; } = new();
        public List<Review> Reviews { get; set; } = new();

        [JsonIgnore]
        public string ImageUrl
        {
            get
            {
                if (ImageUrls == null || !ImageUrls.Any())
                    return "placeholder_img.webp"; 

                return ImageUrls.First();
            }
        }

        [JsonIgnore]
        public List<string> FullImageUrls
        {
            get
            {
                // Nếu không có ảnh nào, trả về 1 list chứa ảnh mặc định để UI không bị trống
                if (ImageUrls == null || !ImageUrls.Any())
                    return new List<string> { "placeholder_img.webp" };

                return ImageUrls;
            }
        }

        [ObservableProperty]
        private string _distanceDisplay = "Đang tính...";

        [ObservableProperty]
        private double _distanceInMeters;

        [JsonPropertyName("narrations")]
        public List<Narration> Narrations { get; set; } = new();

        [JsonPropertyName("restaurants")]
        public List<Restaurant> Restaurants { get; set; } = new();
    }
}