using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using VinhKhanh.WebAdmin.Models;

namespace VinhKhanh.WebAdmin.Services
{
	public interface IFoodTranslationService
	{
		Task<List<FoodTranslationDto>> GetByFoodIdAsync(int foodId);
		Task<(bool Success, string Msg)> CreateAsync(CreateFoodTranslationReq req);
		Task<bool> DeleteAsync(int id);
	}

	public class FoodTranslationService : IFoodTranslationService
	{
		private readonly HttpClient _http;
		private readonly ILocalStorageService _localStorage;

		public FoodTranslationService(HttpClient http, ILocalStorageService localStorage)
		{
			_http = http; _localStorage = localStorage;
		}

		private async Task AttachTokenAsync()
		{
			var token = await _localStorage.GetItemAsync<string>("authToken");
			if (!string.IsNullOrEmpty(token))
				_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		}

		public async Task<List<FoodTranslationDto>> GetByFoodIdAsync(int foodId)
		{
			await AttachTokenAsync();
			try { return await _http.GetFromJsonAsync<List<FoodTranslationDto>>($"api/FoodTranslations/food/{foodId}") ?? new(); }
			catch { return new(); }
		}

		public async Task<(bool Success, string Msg)> CreateAsync(CreateFoodTranslationReq req)
		{
			await AttachTokenAsync();
			var res = await _http.PostAsJsonAsync("api/FoodTranslations", req);
			if (res.IsSuccessStatusCode) return (true, "OK");
			return (false, await res.Content.ReadAsStringAsync());
		}

		public async Task<bool> DeleteAsync(int id)
		{
			await AttachTokenAsync();
			var res = await _http.DeleteAsync($"api/FoodTranslations/{id}");
			return res.IsSuccessStatusCode;
		}
	}
}