using System;
using System.Text.Json.Serialization;

namespace client.lib.model
{
    public class PoiImage
    {
        [JsonPropertyName("imageId")]
        public int ImageId { get; set; }

        [JsonPropertyName("poiId")]
        public int PoiId { get; set; }

        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonPropertyName("publicId")]
        public string? PublicId { get; set; } // [Thêm mới] Đồng bộ với Backend

        [JsonPropertyName("displayOrder")]
        public int DisplayOrder { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        public string FullImageUrl =>
            string.IsNullOrWhiteSpace(ImageUrl)
                ? "placeholder_img.webp"
                : ImageUrl;
    }
}