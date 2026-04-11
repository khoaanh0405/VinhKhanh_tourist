using System.Text.Json.Serialization;

namespace VinhKhanh.WebAdmin.Models
{
	public class FoodModel
	{
		[JsonPropertyName("foodId")]
		public int FoodId { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("price")]
		public decimal Price { get; set; }

		[JsonPropertyName("description")]
		public string Description { get; set; } = string.Empty;

		[JsonPropertyName("restaurantId")]
		public int RestaurantId { get; set; }
	}
}