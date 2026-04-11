using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using VinhKhanh.WebAdmin.Models;

namespace VinhKhanh.WebAdmin.Services
{
	public interface IUserService
	{
		Task<List<UserModel>> GetUsersAsync();
		Task<bool> CreateUserAsync(RegisterRequest request);

		// ĐÃ SỬA: Chuyển dòng này lên đúng vị trí của Interface
		Task<bool> ChangePasswordAsync(int userId, string newPassword);
	}

	public class UserService : IUserService
	{
		private readonly HttpClient _http;
		private readonly ILocalStorageService _localStorage;

		public UserService(HttpClient http, ILocalStorageService localStorage)
		{
			_http = http;
			_localStorage = localStorage;
		}

		public async Task<List<UserModel>> GetUsersAsync()
		{
			await AttachTokenAsync();
			var result = await _http.GetFromJsonAsync<List<UserModel>>("api/user");
			return result ?? new List<UserModel>();
		}

		public async Task<bool> CreateUserAsync(RegisterRequest request)
		{
			await AttachTokenAsync();
			var response = await _http.PostAsJsonAsync("api/user/admin-create", request);
			return response.IsSuccessStatusCode;
		}

		// Hàm đổi mật khẩu (đã xóa dòng thừa ở trên đi)
		public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
		{
			await AttachTokenAsync();
			var req = new ChangePasswordRequest { NewPassword = newPassword };
			var response = await _http.PutAsJsonAsync($"api/user/admin-change-password/{userId}", req);
			return response.IsSuccessStatusCode;
		}

		// Hàm phụ móc thẻ VIP (Token)
		private async Task AttachTokenAsync()
		{
			var token = await _localStorage.GetItemAsync<string>("authToken");
			if (!string.IsNullOrEmpty(token))
				_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		}
	}
}