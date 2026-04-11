using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using VinhKhanh.WebAdmin.Models;

namespace VinhKhanh.WebAdmin.Services
{
	public class DashboardService
	{
		private readonly HttpClient _http;
		private readonly ILocalStorageService _localStorage;

		public DashboardService(HttpClient http, ILocalStorageService localStorage)
		{
			_http = http; _localStorage = localStorage;
		}

		public async Task<DashboardStatsDto> GetStatsAsync()
		{
			var token = await _localStorage.GetItemAsync<string>("authToken");
			if (!string.IsNullOrEmpty(token))
				_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			try
			{
				return await _http.GetFromJsonAsync<DashboardStatsDto>("api/Dashboard/stats") ?? new();
			}
			catch
			{
				return new DashboardStatsDto();
			}
		}
	}
}