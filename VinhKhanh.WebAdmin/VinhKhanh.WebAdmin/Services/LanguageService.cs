using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using VinhKhanh.WebAdmin.Models;

namespace VinhKhanh.WebAdmin.Services
{
	public interface ILanguageService
	{
		Task<List<LanguageDto>> GetAllAsync();
		Task<bool> CreateAsync(LanguageDto dto);
		Task<bool> DeleteAsync(string code);
	}

	public class LanguageService : ILanguageService
	{
		private readonly HttpClient _http;
		private readonly ILocalStorageService _localStorage;

		public LanguageService(HttpClient http, ILocalStorageService localStorage)
		{
			_http = http; _localStorage = localStorage;
		}

		private async Task AttachTokenAsync()
		{
			var token = await _localStorage.GetItemAsync<string>("authToken");
			if (!string.IsNullOrEmpty(token))
				_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		}

		public async Task<List<LanguageDto>> GetAllAsync()
		{
			await AttachTokenAsync();
			try { return await _http.GetFromJsonAsync<List<LanguageDto>>("api/Language") ?? new(); }
			catch { return new(); }
		}

		public async Task<bool> CreateAsync(LanguageDto dto)
		{
			await AttachTokenAsync();
			var res = await _http.PostAsJsonAsync("api/Language", dto);
			return res.IsSuccessStatusCode;
		}

		public async Task<bool> DeleteAsync(string code)
		{
			await AttachTokenAsync();
			var res = await _http.DeleteAsync($"api/Language/{code}");
			return res.IsSuccessStatusCode;
		}
	}
}