using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace client.lib.model
{
    public class Food
    {
        [JsonPropertyName("foodId")]
        public int FoodId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public double Price { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }
    }
}
