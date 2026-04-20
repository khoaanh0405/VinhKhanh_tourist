using System.Collections.Generic;

namespace Server.DTOs
{
    // ─── READ ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Thông tin quán ăn.
    /// Đã xoá: Description, IsLocked — các cột này không tồn tại trong bảng Restaurants.
    /// Đã thêm: PoiLatitude, PoiLongitude để hiển thị ra bản đồ (lấy từ POI).
    /// </summary>
    public class RestaurantDto
    {
        public int RestaurantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }

        // Quan hệ
        public int PoiId { get; set; }
        public int? ManagerUserId { get; set; }
        public string? ManagerName { get; set; } // Đã đổi tên để khớp với Service

        // Toạ độ lấy từ bảng POI qua
        public double PoiLatitude { get; set; }
        public double PoiLongitude { get; set; }

        public List<FoodDto> Foods { get; set; } = new();

        public RestaurantDto() { }

        // Constructor 8 tham số để fix lỗi CS1729
        public RestaurantDto(
            int id, string name, string? address,
            int poiId, int? managerId, string? managerDisplayName,
            double poiLat, double poiLng)
        {
            RestaurantId = id;
            Name = name;
            Address = address;
            PoiId = poiId;
            ManagerUserId = managerId;
            ManagerName = managerDisplayName;
            PoiLatitude = poiLat;
            PoiLongitude = poiLng;
        }
    }

    public class FoodDto
    {
        public int FoodId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int RestaurantId { get; set; }

        public FoodDto() { }

        public FoodDto(int id, string name, decimal price, int restaurantId)
        {
            FoodId = id;
            Name = name;
            Price = price;
            RestaurantId = restaurantId;
        }
    }

    // ─── WRITE ──────────────────────────────────────────────────────────────────

    // Dùng cho Manager tự cập nhật quán của mình
    public record UpdateRestaurantRequest(string Name, string? Address);

    // Phân quyền Manager
    public record AssignManagerRequest(int ManagerUserId);

    // Manager thêm / sửa món ăn
    public record CreateFoodRequest(string Name, decimal Price);
    public record UpdateFoodRequest(string Name, decimal Price);

    // Tourist submit món mới (chờ duyệt)
    public record FoodSubmitRequest(string Name, decimal Price, int RestaurantId);

    // ─── WRITE (Admin) ──────────────────────────────────────────────────────────

    /// <summary>
    /// Admin tạo quán mới. Không có Latitude/Longitude — Restaurants không lưu toạ độ,
    /// toạ độ thuộc về POI. PoiId bắt buộc để liên kết.
    /// </summary>
    public record CreateRestaurantReq(string Name, string? Address, int PoiId);

    /// <summary>
    /// Admin cập nhật quán. PoiId cho phép chuyển quán sang POI khác nếu cần.
    /// </summary>
    public record UpdateRestaurantReq(string Name, string? Address, int PoiId);
}