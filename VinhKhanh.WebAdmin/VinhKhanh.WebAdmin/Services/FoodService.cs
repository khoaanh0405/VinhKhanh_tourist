using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using VinhKhanh.WebAdmin.Models;

namespace VinhKhanh.WebAdmin.Services
{
	public interface IFoodService
	{
		Task<List<Food>> GetAllAsync();
		Task<List<Food>> GetMyFoodsAsync();
		Task<bool> CreateFoodAsync(Food food);
		Task<bool> UpdateFoodAsync(int id, Food food);
		Task<bool> DeleteFoodAsync(int id);
	}

	public class FoodService : IFoodService
	{
		private readonly HttpClient _http;
		private readonly ILocalStorageService _localStorage;

		public FoodService(HttpClient http, ILocalStorageService localStorage)
		{
			_http = http;
			_localStorage = localStorage;
		}

		public async Task<List<Food>> GetAllAsync()
		{
			await AttachTokenAsync();
			var result = await _http.GetFromJsonAsync<List<Food>>("api/Food");
			return result ?? new List<Food>();
		}

		public async Task<List<Food>> GetMyFoodsAsync()
		{
			await AttachTokenAsync();
			var result = await _http.GetFromJsonAsync<List<Food>>("api/Food/my");
			return result ?? new List<Food>();
		}

		// ĐÃ SỬA URL Ở ĐÂY THÀNH "api/Food/my" ĐỂ KHỚP VỚI BACKEND
		public async Task<bool> CreateFoodAsync(Food food)
		{
			await AttachTokenAsync();
			// Đổi lại thành api/Food để gọi trúng cái cổng chung mới viết
			var response = await _http.PostAsJsonAsync("api/Food", food);
			return response.IsSuccessStatusCode;
		}

		public async Task<bool> UpdateFoodAsync(int id, Food food)
		{
			await AttachTokenAsync();
			var response = await _http.PutAsJsonAsync($"api/Food/{id}", food);
			return response.IsSuccessStatusCode;
		}

		public async Task<bool> DeleteFoodAsync(int id)
		{
			await AttachTokenAsync();
			var response = await _http.DeleteAsync($"api/Food/{id}");
			return response.IsSuccessStatusCode;
		}

		private async Task AttachTokenAsync()
		{
			var token = await _localStorage.GetItemAsync<string>("authToken");
			if (!string.IsNullOrEmpty(token))
				_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		}
		public async Task<List<Food>> GetByRestaurantIdAsync(int restaurantId)
		{
			await AttachTokenAsync();
			return await _http.GetFromJsonAsync<List<Food>>($"api/Food/restaurant/{restaurantId}") ?? new List<Food>();
		}
	}
}