using System.Net.Http.Json;
using System.Net.Http.Headers; // 👈 Thêm thư viện này để gắn Header
using Blazored.LocalStorage; // 👈 Thêm thư viện để lấy Token từ LocalStorage
using VinhKhanh.WebAdmin.Models;

namespace VinhKhanh.WebAdmin.Services
{
	public interface IPoiService
	{
		Task<List<POIDto>> GetAllAsync();
		Task<bool> CreateAsync(CreatePoiAdminRequest req);
		Task<bool> UpdateAsync(int id, UpdatePoiAdminRequest req);
		Task<bool> DeleteAsync(int id);
	}

	public class PoiService : IPoiService
	{
		private readonly HttpClient _http;
		private readonly ILocalStorageService _localStorage;

		// Tiêm thêm ILocalStorageService vào đây
		public PoiService(HttpClient http, ILocalStorageService localStorage)
		{
			_http = http;
			_localStorage = localStorage;
		}

		// 👇 HÀM BÍ MẬT: Giúp lấy Token từ LocalStorage và gắn vào HttpClient
		private async Task SetAuthHeader()
		{
			var token = await _localStorage.GetItemAsync<string>("authToken");
			if (!string.IsNullOrEmpty(token))
			{
				_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			}
		}

		public async Task<List<POIDto>> GetAllAsync()
		{
			try
			{
				return await _http.GetFromJsonAsync<List<POIDto>>("api/POIs") ?? new List<POIDto>();
			}
			catch { return new List<POIDto>(); }
		}

		public async Task<bool> CreateAsync(CreatePoiAdminRequest req)
		{
			await SetAuthHeader(); // 👈 Gắn token trước khi gửi
			var res = await _http.PostAsJsonAsync("api/POIs", req);
			return res.IsSuccessStatusCode;
		}

		public async Task<bool> UpdateAsync(int id, UpdatePoiAdminRequest req)
		{
			await SetAuthHeader(); // 👈 Gắn token trước khi gửi
			var res = await _http.PutAsJsonAsync($"api/POIs/{id}", req);
			return res.IsSuccessStatusCode;
		}

		public async Task<bool> DeleteAsync(int id)
		{
			await SetAuthHeader(); // 👈 Gắn token trước khi gửi
			var res = await _http.DeleteAsync($"api/POIs/{id}");
			return res.IsSuccessStatusCode;
		}
	}
}