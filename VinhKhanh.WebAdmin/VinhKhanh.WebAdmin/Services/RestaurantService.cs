using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using VinhKhanh.WebAdmin.Models;

namespace VinhKhanh.WebAdmin.Services
{
	public interface IRestaurantService
	{
		Task<List<Restaurant>> GetAllAsync(); // Dành cho Admin
		Task<List<Restaurant>> GetMyAsync();  // Dành cho Manager
		Task<bool> AssignManagerAsync(int restaurantId, int managerUserId);
		Task<Restaurant?> GetByIdAsync(int id);
		// 👇 BỔ SUNG 3 HÀM NÀY ĐỂ THÊM/SỬA/XÓA GỘP POI
		Task<bool> CreateAsync(CreateRestaurantReq req);
		Task<bool> UpdateAsync(int id, UpdateRestaurantReq req);
		Task<bool> DeleteAsync(int id);
	}

	public class RestaurantService : IRestaurantService
	{
		private readonly HttpClient _http;
		private readonly ILocalStorageService _localStorage;

		public RestaurantService(HttpClient http, ILocalStorageService localStorage)
		{
			_http = http;
			_localStorage = localStorage;
		}
		public async Task<Restaurant?> GetByIdAsync(int id)
		{
			await AttachTokenAsync();
			try
			{
				return await _http.GetFromJsonAsync<Restaurant>($"api/Restaurant/{id}");
			}
			catch { return null; }
		}
		// Hàm 1: Gọi API lấy tất cả quán (Dành cho Admin)
		public async Task<List<Restaurant>> GetAllAsync()
		{
			await AttachTokenAsync();
			var result = await _http.GetFromJsonAsync<List<Restaurant>>("api/Restaurant");
			return result ?? new List<Restaurant>();
		}

		// Hàm 2: Gọi API lấy quán của mình (Dành cho Manager)
		public async Task<List<Restaurant>> GetMyAsync()
		{
			await AttachTokenAsync();
			var result = await _http.GetFromJsonAsync<List<Restaurant>>("api/Restaurant/my");
			return result ?? new List<Restaurant>();
		}

		public async Task<bool> AssignManagerAsync(int restaurantId, int managerUserId)
		{
			await AttachTokenAsync();
			var req = new AssignManagerRequest { ManagerUserId = managerUserId };
			// Gọi API của Backend: POST api/Restaurant/{id}/assign-manager
			var response = await _http.PostAsJsonAsync($"api/Restaurant/{restaurantId}/assign-manager", req);
			return response.IsSuccessStatusCode;
		}


		public async Task<bool> CreateAsync(CreateRestaurantReq req)
		{
			await AttachTokenAsync();
			var res = await _http.PostAsJsonAsync("api/Restaurant", req);
			return res.IsSuccessStatusCode;
		}

		public async Task<bool> UpdateAsync(int id, UpdateRestaurantReq req)
		{
			await AttachTokenAsync();
			var res = await _http.PutAsJsonAsync($"api/Restaurant/{id}", req);
			return res.IsSuccessStatusCode;
		}

		public async Task<bool> DeleteAsync(int id)
		{
			await AttachTokenAsync();
			var res = await _http.DeleteAsync($"api/Restaurant/{id}");
			return res.IsSuccessStatusCode;
		}

		// ==========================================================

		// Hàm phụ: Tự động móc Token gắn vào Header
		private async Task AttachTokenAsync()
		{
			var token = await _localStorage.GetItemAsync<string>("authToken");
			if (!string.IsNullOrEmpty(token))
			{
				_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			}
		}
	}
}