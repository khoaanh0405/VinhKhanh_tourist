using System;
using System.Text.Json.Serialization;

namespace client.lib.model
{
    public class Review
    {
        [JsonPropertyName("reviewId")]
        public int ReviewId { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; } = string.Empty;

        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}