using System.Collections.Generic;

namespace Server.DTOs
{
	public class RestaurantDto
	{
		// === ĐỒ CŨ CỦA BẠN (Giữ nguyên không đổi) ===
		public int RestaurantId { get; set; }
		public string Name { get; set; }
		public string? Address { get; set; }
		public string? Description { get; set; }
		public List<FoodDto> Foods { get; set; }

        public bool IsLocked { get; set; }

        // === ĐỒ MỚI THÊM VÀO (Dành cho chức năng Phân quyền) ===
        public int PoiId { get; set; }
		public int? ManagerUserId { get; set; }
		public string? ManagerName { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }

		// Khởi tạo mặc định để code cũ của bạn không bị lỗi
		public RestaurantDto() { }

		// 👇 ĐÃ SỬA Ở ĐÂY: Thêm double lat, double lng (Đủ 9 tham số) 👇
		public RestaurantDto(int id, string name, string? address, string? desc, int poiId, int? managerId, string? managerName, double lat, double lng, bool isLocked)
		{
			RestaurantId = id;
			Name = name;
			Address = address;
			Description = desc;
			PoiId = poiId;
			ManagerUserId = managerId;
			ManagerName = managerName;
			Latitude = lat;  // Nhận tọa độ
			Longitude = lng; // Nhận tọa độ
            IsLocked = isLocked;
        }
	}

	public class FoodDto
	{
		// === ĐỒ CŨ CỦA BẠN ===
		public int FoodId { get; set; }
		public string Name { get; set; }
		public decimal? Price { get; set; }
		public string? Description { get; set; }

		// === ĐỒ MỚI THÊM VÀO ===
		public int RestaurantId { get; set; }

		public FoodDto() { }

		public FoodDto(int id, string name, decimal? price, string? desc, int restaurantId)
		{
			FoodId = id;
			Name = name;
			Price = price;
			Description = desc;
			RestaurantId = restaurantId;
		}
	}

	// === CÁC HỘP ĐỰNG REQUEST MỚI CHO TÍNH NĂNG PHÂN QUYỀN ===
	public record UpdateRestaurantRequest(string Name, string? Address, string? Description);
	public record AssignManagerRequest(int ManagerUserId);
	public record CreateFoodRequest(string Name, decimal Price, string? Description);
	public record UpdateFoodRequest(string Name, decimal Price, string? Description);
	public record FoodSubmitRequest(string Name, decimal Price, string? Description, int RestaurantId);

	// === KHUÔN MỚI CHO ADMIN (GỘP POI + QUÁN ĂN) ===
	public record CreateRestaurantReq(string Name, string Address, string? Description, double Latitude, double Longitude);
	public record UpdateRestaurantReq(string Name, string Address, string? Description, double Latitude, double Longitude, int PoiId);
}