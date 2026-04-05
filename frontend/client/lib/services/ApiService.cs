using client.lib.core;
using client.lib.model;
using client.lib.models;
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

        // 1. [ĐÃ SỬA] Thêm tham số lang và nối vào chuỗi URL
        public async Task<List<POI>> FetchPOIsAsync(string lang = "vi")
        {
            try
            {
                string url = $"{ApiEndpoints.Pois}?lang={lang}";
                return await _httpClient.GetFromJsonAsync<List<POI>>(url, _jsonOptions) ?? new();
            }
            catch { return new List<POI>(); }
        }

        // 2. [ĐÃ SỬA LẠI CHO ĐỒNG BỘ VỚI BACKEND MỚI]
        public async Task<POI?> FetchPOIByIdAsync(int id, string? languageCode = null)
        {
            string url = $"{ApiEndpoints.Pois}/{id}";

            // Backend mới của chúng ta nhận ?lang=en thay vì /language/en
            if (!string.IsNullOrEmpty(languageCode))
            {
                url += $"?lang={languageCode}";
            }

            return await _httpClient.GetFromJsonAsync<POI>(url, _jsonOptions);
        }

        public async Task<List<Language>> FetchLanguagesAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<Language>>(ApiEndpoints.Languages, _jsonOptions) ?? new();
        }

        public async Task<AuthResponse> RegisterAsync(string displayName, string username, string password)
        {
            try
            {
                var request = new RegisterRequest { DisplayName = displayName, Username = username, Password = password };

                // Gọi lên API: http://10.0.2.2:5280/api/user/register
                var response = await _httpClient.PostAsJsonAsync("user/register", request);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
                }

                // Nếu Server trả về lỗi (Ví dụ: Trùng tên đăng nhập - lỗi 400)
                var error = await response.Content.ReadAsStringAsync();
                return new AuthResponse { Message = string.IsNullOrWhiteSpace(error) ? $"Lỗi Server: {response.StatusCode}" : error };
            }
            catch (Exception ex)
            {
                // Nếu App không thể kết nối được tới Server
                return new AuthResponse { Message = $"Lỗi kết nối: {ex.Message}" };
            }
        }

        public async Task<AuthResponse> LoginAsync(string username, string password)
        {
            var request = new LoginRequest { Username = username, Password = password };
            var response = await _httpClient.PostAsJsonAsync("user/login", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
            }

            var error = await response.Content.ReadAsStringAsync();
            return new AuthResponse { Message = error };
        }
    }
}