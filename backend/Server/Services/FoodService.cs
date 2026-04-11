using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Models;

namespace Server.Services
{
	public interface IFoodService
	{
		Task<List<FoodDto>> GetAllAsync();
		Task<List<FoodDto>> GetMyAsync(int managerUserId);
		Task<FoodDto?> CreateMyAsync(int managerUserId, CreateFoodRequest req);
		Task<bool> UpdateAsync(int foodId, int managerUserId, UpdateFoodRequest req, bool isAdmin);
		Task<bool> DeleteAsync(int foodId, int managerUserId, bool isAdmin);

		Task<bool> CreateAdminAsync(Food req);
	}

	public class FoodService : IFoodService
	{
		private readonly AppDbContext _db;

		public FoodService(AppDbContext db) => _db = db;

		public async Task<List<FoodDto>> GetAllAsync()
			=> await _db.Foods
				.Select(f => new FoodDto(f.FoodId, f.Name, f.Price, f.Description, f.RestaurantId))
				.ToListAsync();

		public async Task<List<FoodDto>> GetMyAsync(int managerUserId)
		{
			return await _db.Foods
				.Where(f => f.Restaurant.ManagerUserId == managerUserId)
				.Select(f => new FoodDto(f.FoodId, f.Name, f.Price, f.Description, f.RestaurantId))
				.ToListAsync();
		}

		public async Task<FoodDto?> CreateMyAsync(int managerUserId, CreateFoodRequest req)
		{
			var restaurant = await _db.Restaurants
				.FirstOrDefaultAsync(r => r.ManagerUserId == managerUserId);
			if (restaurant == null) return null;

			var food = new Food
			{
				Name = req.Name,
				Price = req.Price,
				Description = req.Description,
				RestaurantId = restaurant.RestaurantId
			};
			_db.Foods.Add(food);
			await _db.SaveChangesAsync();
			return new FoodDto(food.FoodId, food.Name, food.Price, food.Description, food.RestaurantId);
		}

		public async Task<bool> UpdateAsync(int foodId, int managerUserId, UpdateFoodRequest req, bool isAdmin)
		{
			var query = _db.Foods.Include(f => f.Restaurant).AsQueryable();

			if (!isAdmin)
				query = query.Where(f => f.Restaurant.ManagerUserId == managerUserId);

			var food = await query.FirstOrDefaultAsync(f => f.FoodId == foodId);
			if (food == null) return false;

			food.Name = req.Name;
			food.Price = req.Price;
			food.Description = req.Description;
			await _db.SaveChangesAsync();
			return true;
		}

		public async Task<bool> DeleteAsync(int foodId, int managerUserId, bool isAdmin)
		{
			var query = _db.Foods.Include(f => f.Restaurant).AsQueryable();

			if (!isAdmin)
				query = query.Where(f => f.Restaurant.ManagerUserId == managerUserId);

			var food = await query.FirstOrDefaultAsync(f => f.FoodId == foodId);
			if (food == null) return false;

			_db.Foods.Remove(food);
			await _db.SaveChangesAsync();
			return true;
		}
		public async Task<bool> CreateAdminAsync(Food req)
		{
			_db.Foods.Add(req);
			await _db.SaveChangesAsync();
			return true;
		}

	}
}