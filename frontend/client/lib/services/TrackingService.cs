using System.Net.Http.Json;
using Microsoft.Maui.Storage; // Dùng để lưu trữ Device ID vĩnh viễn trên máy

namespace client.lib.services
{
    public class TrackingService
    {
        private readonly HttpClient _http;
        private System.Threading.Timer? _heartbeatTimer;

        private const string BaseApiUrl = "https://sln71gls-7284.asse.devtunnels.ms/api/Tracking";

        public TrackingService(HttpClient http)
        {
            _http = http;
        }

        // 1. Hàm khởi tạo và lấy Device ID (Mỗi máy 1 mã duy nhất)
        public string GetDeviceId()
        {
            // Tìm trong bộ nhớ máy xem có ID chưa
            var deviceId = Preferences.Get("device_id", string.Empty);

            if (string.IsNullOrEmpty(deviceId))
            {
                // Nếu là lần đầu tải app -> Sinh mã mới và lưu lại
                deviceId = Guid.NewGuid().ToString();
                Preferences.Set("device_id", deviceId);
            }
            return deviceId;
        }

        // 2. Bắt đầu gửi Heartbeat (Khi đang mở App)
        public void StartHeartbeat()
        {
            var deviceId = GetDeviceId();

            _heartbeatTimer = new System.Threading.Timer(async _ =>
            {
                try
                {
                    var req = new { DeviceId = deviceId };

                    // 👇 TẠO REQUEST MESSAGE ĐỂ NHÉT THÊM HEADER VƯỢT RÀO
                    var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseApiUrl}/heartbeat")
                    {
                        Content = JsonContent.Create(req)
                    };

                    // Header giúp vượt qua trang cảnh báo của Dev Tunnel / Ngrok
                    request.Headers.Add("X-Tunnel-Skip-AntiPhishing-Page", "true");
                    request.Headers.Add("ngrok-skip-browser-warning", "true");

                    var response = await _http.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        var err = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"[TRACKING ERROR] Lỗi Server: {response.StatusCode} - {err}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[TRACKING OK] Đã gửi Heartbeat cho máy: {deviceId}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TRACKING CRASH] Lỗi Code/Mạng: {ex.Message}");
                }
            }, null, 0, 5000);
        }

        // 3. Dừng gửi Heartbeat (Khi khách thoát ra màn hình chính điện thoại)
        public void StopHeartbeat()
        {
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
        }

        // 4. Gửi tín hiệu khi quét QR thành công
        public async Task LogQrScanAsync(int poiId)
        {
            try
            {
                var req = new
                {
                    DeviceId = GetDeviceId(),
                    PoiId = poiId
                };

                // Gửi lên Backend (Backend đã có logic chống trùng lặp 3 phút)
                await _http.PostAsJsonAsync($"{BaseApiUrl}/scan-qr", req);
            }
            catch { }
        }

        // 5. Gửi tín hiệu khi quét QR Playlist thành công
        public async Task LogPlaylistScanAsync(int playlistId)
        {
            try
            {
                // Gửi cả DeviceId và PlaylistId lên Backend
                var req = new
                {
                    DeviceId = GetDeviceId(),
                    PlaylistId = playlistId,
                    PoiId = (int?)null // Để trống PoiId vì ta đang quét Playlist
                };

                // Header vượt rào cho Dev Tunnel (nếu cần)
                var response = await _http.PostAsJsonAsync($"{BaseApiUrl}/scan-qr", req);

                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[TRACKING OK] Đã ghi nhận quét Playlist #{playlistId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TRACKING ERROR] Lỗi log Playlist: {ex.Message}");
            }
        }
    }
}