using System.Text.Json.Serialization;

namespace VinhKhanh.WebAdmin.Models
{
    public class Restaurant
    {
        [JsonPropertyName("restaurantId")]
        public int RestaurantId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("managerUserId")]
        public int? ManagerUserId { get; set; }

        [JsonPropertyName("managerName")]
        public string? ManagerName { get; set; }

        [JsonPropertyName("poiId")]
        public int PoiId { get; set; }
    }

    public class AssignManagerRequest
    {
        public int ManagerUserId { get; set; }
    }

    public record CreateRestaurantReq(string Name, string Address, int PoiId);

    public record UpdateRestaurantReq(string Name, string Address, int PoiId);
}