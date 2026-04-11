using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using VinhKhanh.WebAdmin.Models; // Đảm bảo bạn đã định nghĩa PoiTranslationDto ở Frontend

namespace VinhKhanh.WebAdmin.Services
{
	public interface IPoiTranslationService
	{
		Task<List<PoiTranslationDto>> GetByPoiIdAsync(int poiId);
		Task<bool> SaveAsync(int poiId, string lang, string desc);
		Task<bool> DeleteAsync(int id);
	}

	public class PoiTranslationService : IPoiTranslationService
	{
		private readonly HttpClient _http;
		private readonly ILocalStorageService _localStorage;

		public PoiTranslationService(HttpClient http, ILocalStorageService localStorage)
		{
			_http = http; _localStorage = localStorage;
		}

		private async Task AttachTokenAsync()
		{
			var token = await _localStorage.GetItemAsync<string>("authToken");
			if (!string.IsNullOrEmpty(token))
				_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		}

		public async Task<List<PoiTranslationDto>> GetByPoiIdAsync(int poiId)
		{
			await AttachTokenAsync();
			return await _http.GetFromJsonAsync<List<PoiTranslationDto>>($"api/PoiTranslations/poi/{poiId}") ?? new();
		}

		public async Task<bool> SaveAsync(int poiId, string lang, string desc)
		{
			await AttachTokenAsync();
			var res = await _http.PostAsJsonAsync("api/PoiTranslations", new { PoiId = poiId, LanguageCode = lang, Description = desc });
			return res.IsSuccessStatusCode;
		}

		public async Task<bool> DeleteAsync(int id)
		{
			await AttachTokenAsync();
			var res = await _http.DeleteAsync($"api/PoiTranslations/{id}");
			return res.IsSuccessStatusCode;
		}
	}
}