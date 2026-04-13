using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Models;

namespace Server.Services
{
	public interface IRestaurantService
	{
		Task<List<RestaurantDto>> GetAllAsync();
		Task<RestaurantDto?> GetMyAsync(int managerUserId);
		Task<RestaurantDto?> UpdateMyAsync(int managerUserId, UpdateRestaurantRequest req);
		Task<bool> AssignManagerAsync(int restaurantId, int managerUserId);
        Task<bool> ToggleLockAsync(int restaurantId);
    }

    public class RestaurantService : IRestaurantService
    {
        private readonly AppDbContext _db;
        public RestaurantService(AppDbContext db) => _db = db;

        // 1. Cập nhật GetAllAsync: Thêm r.IsLocked vào Constructor (Tham số thứ 10)
        public async Task<List<RestaurantDto>> GetAllAsync()
            => await _db.Restaurants
                .Include(r => r.Manager)
                .Include(r => r.POI)
                .Select(r => new RestaurantDto(
                    r.RestaurantId,
                    r.Name,
                    r.Address,
                    r.Description,
                    r.PoiId,
                    r.ManagerUserId,
                    r.Manager != null ? r.Manager.DisplayName : null,
                    r.POI != null ? r.POI.Latitude : 0,
                    r.POI != null ? r.POI.Longitude : 0,
                    r.IsLocked // 🔥 THÊM: Tham số thứ 10
                ))
                .ToListAsync();

        // 2. Cập nhật GetMyAsync: Thêm r.IsLocked vào kết quả trả về
        public async Task<RestaurantDto?> GetMyAsync(int userId)
        {
            var r = await _db.Restaurants
                .Include(x => x.Manager)
                .Include(x => x.POI)
                .FirstOrDefaultAsync(x => x.ManagerUserId == userId);

            if (r == null) return null;

            return new RestaurantDto(
                r.RestaurantId, r.Name, r.Address, r.Description, r.PoiId,
                r.ManagerUserId, r.Manager != null ? r.Manager.DisplayName : null,
                r.POI != null ? r.POI.Latitude : 0, r.POI != null ? r.POI.Longitude : 0,
                r.IsLocked // 🔥 THÊM
            );
        }

        // 3. Cập nhật UpdateMyAsync
        public async Task<RestaurantDto?> UpdateMyAsync(int managerUserId, UpdateRestaurantRequest req)
        {
            var r = await _db.Restaurants
                .Include(x => x.Manager)
                .Include(x => x.POI)
                .Where(res => res.ManagerUserId == managerUserId)
                .FirstOrDefaultAsync();

            if (r == null) return null;

            r.Name = req.Name;
            r.Address = req.Address;
            r.Description = req.Description;
            await _db.SaveChangesAsync();

            return new RestaurantDto(
                r.RestaurantId, r.Name, r.Address, r.Description, r.PoiId,
                r.ManagerUserId, r.Manager != null ? r.Manager.DisplayName : null,
                r.POI != null ? r.POI.Latitude : 0, r.POI != null ? r.POI.Longitude : 0,
                r.IsLocked // 🔥 THÊM
            );
        }

        // 4. 🔥 TRIỂN KHAI HÀM MỚI: ToggleLockAsync
        public async Task<bool> ToggleLockAsync(int restaurantId)
        {
            var restaurant = await _db.Restaurants.FindAsync(restaurantId);
            if (restaurant == null) return false;

            restaurant.IsLocked = !restaurant.IsLocked;
            await _db.SaveChangesAsync();
            return true;
        }

        // 4. Hàm Gán quản lý
        public async Task<bool> AssignManagerAsync(int restaurantId, int managerUserId)
		{
			var user = await _db.Users.FindAsync(managerUserId);
			if (user == null || user.Role != "Manager") return false;

			var alreadyManages = await _db.Restaurants
				.AnyAsync(r => r.ManagerUserId == managerUserId && r.RestaurantId != restaurantId);
			if (alreadyManages) return false;

			var restaurant = await _db.Restaurants.FindAsync(restaurantId);
			if (restaurant == null) return false;

			restaurant.ManagerUserId = managerUserId;
			await _db.SaveChangesAsync();
			return true;
		}
	}
}