using System.Text.Json.Serialization;

namespace VinhKhanh.WebAdmin.Models
{
	// 1. Model chính của Quán ăn (Đã bổ sung PoiId, Latitude, Longitude)
	public class Restaurant
	{
		[JsonPropertyName("restaurantId")]
		public int RestaurantId { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("address")]
		public string Address { get; set; } = string.Empty;

		[JsonPropertyName("description")]
		public string Description { get; set; } = string.Empty;

		[JsonPropertyName("managerUserId")]
		public int? ManagerUserId { get; set; }

		[JsonPropertyName("managerName")]
		public string? ManagerName { get; set; }

		[JsonPropertyName("poiId")]
		public int PoiId { get; set; }

		[JsonPropertyName("latitude")]
		public double Latitude { get; set; }

		[JsonPropertyName("longitude")]
		public double Longitude { get; set; }
	}

	// 2. Request để gán Quản lý
	public class AssignManagerRequest
	{
		public int ManagerUserId { get; set; }
	}

	// =========================================================
	// 👇 CHÍNH LÀ 2 DÒNG NÀY ĐỂ FIX LỖI BẠN ĐANG GẶP 👇
	// =========================================================

	public record CreateRestaurantReq(string Name, string Address, string Description, double Latitude, double Longitude);

	public record UpdateRestaurantReq(string Name, string Address, string Description, double Latitude, double Longitude, int PoiId);
}