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

        // ── Constructor khôi phục lại như cũ để các trang Login dùng được ──
        public ApiService()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(AppConstants.ApiBaseUrl) };

            // Header cực kỳ quan trọng để đi xuyên qua trang xanh của Dev Tunnels
            _httpClient.DefaultRequestHeaders.Add("X-Tunnel-Skip-AntiPhishing-Page", "true");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        // ── 1. Lấy danh sách tất cả POI (Dành cho trang chủ) ───────────────
        public async Task<List<POI>?> FetchPOIsAsync(string langCode = "vi")
        {
            try
            {
                string url = $"{ApiEndpoints.Pois}?lang={langCode}";
                return await _httpClient.GetFromJsonAsync<List<POI>>(url, _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] FetchPOIs error: {ex.Message}");
                return new List<POI>();
            }
        }

        // ── 2. Lấy chi tiết 1 POI (Dành cho quét QR) ───────────────────────
        public async Task<POI?> FetchPOIByIdAsync(int poiId, string langCode = "vi")
        {
            try
            {
                string url = $"{ApiEndpoints.Pois}/{poiId}?lang={langCode}";
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

        // ── 3. Authentication (Đăng ký / Đăng nhập) ────────────────────────
        public async Task<AuthResponse> RegisterAsync(string displayName, string username, string password)
        {
            try
            {
                var request = new { DisplayName = displayName, Username = username, Password = password, Role = "Tourist" };
                var response = await _httpClient.PostAsJsonAsync("user/register", request);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions) ?? new AuthResponse();
                }

                var error = await response.Content.ReadAsStringAsync();
                return new AuthResponse { Message = string.IsNullOrWhiteSpace(error) ? $"Lỗi Server: {response.StatusCode}" : error };
            }
            catch (Exception ex)
            {
                return new AuthResponse { Message = $"Lỗi kết nối: {ex.Message}" };
            }
        }

        public async Task<AuthResponse> LoginAsync(string username, string password)
        {
            try
            {
                var request = new { Username = username, Password = password };
                var response = await _httpClient.PostAsJsonAsync("user/login", request);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions) ?? new AuthResponse();
                }

                var error = await response.Content.ReadAsStringAsync();
                return new AuthResponse { Message = error };
            }
            catch (Exception ex)
            {
                return new AuthResponse { Message = $"Lỗi kết nối: {ex.Message}" };
            }
        }

        // ── 4. Mapping DTO → Model ─────────────────────────────────────────
        private static POI MapToPOI(POIDetailResponse r) => new()
        {
            PoiId = r.PoiId,
            Name = r.Name,
            Latitude = r.Latitude,
            Longitude = r.Longitude,
            Description = r.Description,
            AverageRating = r.AverageRating,
            ReviewCount = r.ReviewCount,

            // Backend trả thẳng List<string> nên ta nhận luôn
            ImageUrls = r.ImageUrls ?? new(),

            // Sửa lại cho khớp với JSON trả về là 1 List Narrations
            Narrations = r.Narrations?.Select(n => MapToNarration(n)).ToList() ?? new(),

            // Map lại danh sách Nhà hàng & Món ăn theo cấu trúc lồng nhau mới
            Restaurants = r.Restaurants?.Select(rDto => new Restaurant
            {
                RestaurantId = rDto.RestaurantId,
                Name = rDto.Name,
                Description = rDto.Description ?? string.Empty,
                Foods = rDto.Foods?.Select(fDto => new Food
                {
                    FoodId = fDto.FoodId,
                    Name = fDto.Name,
                    Price = (double)fDto.Price, // Ép kiểu an toàn
                    Description = fDto.Description ?? string.Empty
                }).ToList() ?? new()
            }).ToList() ?? new()
        };

        private static Narration MapToNarration(NarrationDto dto) => new()
        {
            NarrationId = dto.NarrationId,
            LanguageCode = dto.LanguageCode,
            Text = dto.Text,
            AudioUrl = dto.AudioUrl,
            UseAudioFile = dto.UseAudioFile,
            VoiceName = dto.VoiceName,
            SpeechRate = dto.SpeechRate,
            Volume = dto.Volume,
            DurationSeconds = dto.DurationSeconds
        };
    }

    // ── DTOs (Cấu trúc nhận JSON cho khớp 100% với Backend POIsController) ───

    public class POIDetailResponse
    {
        public int PoiId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Description { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }

        public List<string>? ImageUrls { get; set; } // Phải là ImageUrls
        public List<NarrationDto>? Narrations { get; set; } // Phải là dạng List
        public List<RestaurantDto>? Restaurants { get; set; } // Cấu trúc phân tầng
    }

    public class RestaurantDto
    {
        public int RestaurantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Description { get; set; }
        public List<FoodDto>? Foods { get; set; }
    }

    public class FoodDto
    {
        public int FoodId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
    }

    public class NarrationDto
    {
        public int NarrationId { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? AudioUrl { get; set; }
        public bool UseAudioFile { get; set; }
        public string? VoiceName { get; set; }
        public double SpeechRate { get; set; } = 0.5;
        public double Volume { get; set; } = 1.0;
        public int? DurationSeconds { get; set; }
    }

    public class AuthResponse
    {
        public string? Token { get; set; }
        public string? Message { get; set; }
        public int? UserId { get; set; }
        public string? Role { get; set; }
    }
}