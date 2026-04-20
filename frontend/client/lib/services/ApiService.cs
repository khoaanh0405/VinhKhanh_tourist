using client.lib.core;
using client.lib.model;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace client.lib.services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(AppConstants.ApiBaseUrl) };
            _httpClient.DefaultRequestHeaders.Add("X-Tunnel-Skip-AntiPhishing-Page", "true");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task<List<POI>?> FetchPOIsAsync(string langCode = "vi")
        {
            try
            {
                string url = $"{ApiEndpoints.Pois}?lang={langCode}&t={DateTime.Now.Ticks}";
                return await _httpClient.GetFromJsonAsync<List<POI>>(url, _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] FetchPOIs error: {ex.Message}");
                return new List<POI>();
            }
        }

        public async Task<POI?> FetchPOIByIdAsync(int poiId, string langCode = "vi")
        {
            try
            {
                string url = $"{ApiEndpoints.Pois}/{poiId}?lang={langCode}&t={DateTime.Now.Ticks}";
                var response = await _httpClient.GetFromJsonAsync<POIDetailResponse>(url, _jsonOptions);

                if (response is null) return null;

                return MapToPOI(response);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] POI {poiId} không tìm thấy.");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] FetchPOIById error: {ex.Message}");
                return null;
            }
        }

        private static POI MapToPOI(POIDetailResponse r) => new()
        {
            PoiId = r.PoiId,
            Name = r.Name,
            Latitude = r.Latitude,
            Longitude = r.Longitude,
            ImageUrls = r.ImageUrls ?? new(),
            Narrations = r.Narrations?.Select(n => MapToNarration(n)).ToList() ?? new(),
            Restaurants = r.Restaurants?
                .Select(rDto => new Restaurant
                {
                    RestaurantId = rDto.RestaurantId,
                    Name = rDto.Name,
                    Address = rDto.Address ?? string.Empty,
                    Foods = rDto.Foods?.Select(fDto => new Food
                    {
                        FoodId = fDto.FoodId,
                        Name = fDto.Name,
                        Price = (double)fDto.Price
                    }).ToList() ?? new()
                }).ToList() ?? new()
        };

        private static Narration MapToNarration(NarrationDto dto) => new()
        {
            NarrationId = dto.NarrationId,
            LanguageCode = dto.LanguageCode,
            Text = dto.Text,
            VoiceName = dto.VoiceName,
            SpeechRate = dto.SpeechRate,
            Volume = dto.Volume
        };
        public class AppLanguage
        {
            public string LanguageCode { get; set; } = "";
            public string LanguageName { get; set; } = "";
        }

        public async Task<List<AppLanguage>> GetAvailableLanguagesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<AppLanguage>>("Language") ?? new();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LỖI API NGÔN NGỮ]: {ex.Message}");
                return new();
            }
        }

        public async Task<Dictionary<string, string>> GetUITranslationsAsync(string langCode)
        {
            try
            {
                // 1. In ra đường dẫn thực tế để xem có bị dư chữ api/ không
                System.Diagnostics.Debug.WriteLine($"[TEST DỊCH] Đang gọi lấy từ điển cho: {langCode}");

                // 2. Gọi API thủ công để đọc phản hồi thật
                var response = await _httpClient.GetAsync($"UITranslations/{langCode}");
                string content = await response.Content.ReadAsStringAsync();

                // 3. In ra mã lỗi và nội dung thật từ Server
                System.Diagnostics.Debug.WriteLine($"[TEST DỊCH] Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[TEST DỊCH] Nội dung: {content}");

                if (response.IsSuccessStatusCode)
                {
                    // Trả về dữ liệu nếu thành công
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(content, _jsonOptions);
                    return dict ?? new();
                }

                return new();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TEST DỊCH] Lỗi code/mạng: {ex.Message}");
                return new();
            }
        }
    }

    public class POIDetailResponse
    {
        public int PoiId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<string>? ImageUrls { get; set; }
        public List<NarrationDto>? Narrations { get; set; }
        public List<RestaurantDto>? Restaurants { get; set; }
    }

    public class RestaurantDto
    {
        public int RestaurantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public List<FoodDto>? Foods { get; set; }
    }

    public class FoodDto
    {
        public int FoodId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class NarrationDto
    {
        public int NarrationId { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? VoiceName { get; set; }
        public double SpeechRate { get; set; } = 0.5;
        public double Volume { get; set; } = 1.0;
    }
}