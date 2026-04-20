using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace client.lib.model
{
    public class Restaurant
    {
        [JsonPropertyName("restaurantId")]
        public int RestaurantId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("foods")]
        public List<Food> Foods { get; set; } = new();
    }
}