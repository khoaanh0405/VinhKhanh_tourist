using client.lib.core;
using client.lib.model;
using System.Net.Http.Json;
using System.Text.Json;

namespace client.lib.services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(AppConstants.ApiBaseUrl) };
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<List<POI>> FetchPOIsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<POI>>(ApiEndpoints.Pois, _jsonOptions) ?? new();
            }
            catch { return new List<POI>(); }
        }

        public async Task<POI?> FetchPOIByIdAsync(int id, string? languageCode = null)
        {
            string url = $"{ApiEndpoints.Pois}/{id}";
            if (!string.IsNullOrEmpty(languageCode)) url += $"/language/{languageCode}";

            return await _httpClient.GetFromJsonAsync<POI>(url, _jsonOptions);
        }

        public async Task<List<Language>> FetchLanguagesAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<Language>>(ApiEndpoints.Languages, _jsonOptions) ?? new();
        }
    }
}