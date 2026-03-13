using client.lib.model;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Devices.Sensors;

namespace client.lib.services
{
    public partial class GeofenceService : ObservableObject
    {
        private readonly AudioService _audioService;

        [ObservableProperty]
        private Location? _currentLocation;

        [ObservableProperty]
        private POI? _currentActivePOI;

        private List<POI> _allPOIs = new();

        // Dùng CancellationToken thay vì Timer để quản lý luồng nền
        private CancellationTokenSource? _monitoringCts;

        public GeofenceService(AudioService audioService)
        {
            _audioService = audioService;
        }

        public void SetPois(List<POI> pois)
        {
            _allPOIs = pois ?? new List<POI>();
            System.Diagnostics.Debug.WriteLine($"GeofenceService: Đã nạp {_allPOIs.Count} địa điểm.");
        }

        public async Task InitializeAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            // Gọi init Audio ở đây để đảm bảo TTS sẵn sàng trước khi đi vào vùng
            await _audioService.InitializeAsync();
        }

        public void StartMonitoring()
        {
            if (_monitoringCts != null && !_monitoringCts.IsCancellationRequested) return;

            _monitoringCts = new CancellationTokenSource();

            // CHẠY TRÊN BACKGROUND THREAD ĐỂ KHÔNG ĐƠ UI
            Task.Run(async () => await MonitoringLoopAsync(_monitoringCts.Token));
        }

        public void StopMonitoring()
        {
            _monitoringCts?.Cancel();
        }

        private async Task MonitoringLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Tăng Timeout lên 10 giây để máy ảo có đủ thời gian phản hồi
                    var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
                    var location = await Geolocation.Default.GetLocationAsync(request);

                    if (location != null)
                    {
                        // Thêm dòng log này để nhìn vào cửa sổ Output (Debug) xem tọa độ có nhảy không
                        System.Diagnostics.Debug.WriteLine($"📍 [GPS] Nhận tọa độ: {location.Latitude}, {location.Longitude}");

                        MainThread.BeginInvokeOnMainThread(() => CurrentLocation = location);

                        if (_allPOIs.Any())
                        {
                            UpdateRealtimeDistances(location);
                            CheckGeofences(location);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ GPS Loop Error: {ex.Message}");
                }

                await Task.Delay(3000, token);
            }
        }

        private void UpdateRealtimeDistances(Location currentLoc)
        {
            foreach (var poi in _allPOIs)
            {
                double distKm = Location.CalculateDistance(
                    currentLoc.Latitude, currentLoc.Longitude,
                    poi.Latitude, poi.Longitude, DistanceUnits.Kilometers);

                double distMeters = distKm * 1000;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    poi.DistanceInMeters = distMeters;

                    if (distMeters < 1000)
                    {
                        poi.DistanceDisplay = $"📍 Cách bạn {Math.Round(distMeters)}m";
                    }
                    else
                    {
                        poi.DistanceDisplay = $"📍 Cách bạn {distKm:F1}km";
                    }
                });
            }
        }

        private void CheckGeofences(Location currentLoc)
        {
            // --- THUẬT TOÁN MỚI: NEAREST POI (QUÁN GẦN NHẤT) ---
            if (_allPOIs == null || !_allPOIs.Any()) return;

            // 1. Tìm ra quán ăn đang có khoảng cách GẦN VỚI NGƯỜI DÙNG NHẤT
            var nearestPoi = _allPOIs.OrderBy(p =>
                Location.CalculateDistance(
                    currentLoc.Latitude, currentLoc.Longitude,
                    p.Latitude, p.Longitude, DistanceUnits.Kilometers)
            ).FirstOrDefault();

            if (nearestPoi != null)
            {
                // Tính khoảng cách ra mét
                double distanceInMeters = Location.CalculateDistance(
                    currentLoc.Latitude, currentLoc.Longitude,
                    nearestPoi.Latitude, nearestPoi.Longitude, DistanceUnits.Kilometers) * 1000;

                // 2. CHỈNH BÁN KÍNH KÍCH HOẠT XUỐNG CÒN 20 MÉT
                if (distanceInMeters <= 20)
                {
                    // 3. Nếu quán gần nhất này KHÁC với quán đang đọc -> Lập tức đổi quán!
                    if (CurrentActivePOI == null || CurrentActivePOI.PoiId != nearestPoi.PoiId)
                    {
                        System.Diagnostics.Debug.WriteLine($"!!! VÀO VÙNG QUÁN MỚI: {nearestPoi.Name} (Cách {distanceInMeters:F1}m)");

                        // Đẩy lên UI Thread để kích hoạt sự kiện PropertyChanged (bật Player)
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            CurrentActivePOI = nearestPoi;
                        });
                    }
                }
                else
                {
                    // Nếu khoảng cách đến quán gần nhất vẫn lớn hơn 20m -> Đang đi ngoài đường trống -> Tắt Player
                    if (CurrentActivePOI != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"!!! ĐÃ ĐI RA KHỎI KHU VỰC QUÁN (Cách {distanceInMeters:F1}m) -> TẮT PLAYER");
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            CurrentActivePOI = null;
                        });
                    }
                }
            }
        }
    }
}