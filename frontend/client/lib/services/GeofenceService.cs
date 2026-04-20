using client.lib.model;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Devices.Sensors;

namespace client.lib.services
{
    public partial class GeofenceService : ObservableObject
    {
        private readonly AudioService _audioService;

        private int? _lastPlayedPoiId = null;

        [ObservableProperty]
        private Location? _currentLocation;

        [ObservableProperty]
        private POI? _currentActivePOI;

        private List<POI> _allPOIs = new();

        public List<POI> Pois => _allPOIs;

        private CancellationTokenSource? _monitoringCts;

        public GeofenceService(AudioService audioService)
        {
            _audioService = audioService;
        }

        // 🔥 2. THÊM HÀM NÀY: Tự động chạy khi biến CurrentActivePOI có sự thay đổi
        partial void OnCurrentActivePOIChanged(POI? value)
        {
            if (value != null)
            {
                // Kiểm tra: Nếu ID quán mới trùng với quán đang đọc -> Đang đứng im một chỗ, KHÔNG phát lại
                if (_lastPlayedPoiId == value.PoiId)
                    return;

                // Cập nhật cờ sang quán mới
                _lastPlayedPoiId = value.PoiId;

                // Dừng ngay âm thanh của quán trước đó (nếu lỡ đang phát)
                _audioService.Stop();

                // ❌ ĐÃ XÓA LỆNH PLAY TTS Ở ĐÂY.
                // Giao lại quyền phát âm thanh cho HomeViewModel xử lý để nó kiểm tra được nút Bật/Tắt
            }
            else
            {
                // TRƯỜNG HỢP: Đã đi ra khỏi vùng quán (CurrentActivePOI bị gán bằng null)
                _audioService.Stop();     // Tắt âm thanh
                _lastPlayedPoiId = null;  // Xóa cờ để lần sau quay lại quán này nó đọc lại từ đầu
            }
        }

        public void SetPois(List<POI> pois)
        {
            _allPOIs = pois ?? new List<POI>();
            System.Diagnostics.Debug.WriteLine($"[GeofenceService] Đã nạp {_allPOIs.Count} địa điểm.");

            if (_currentLocation != null)
            {
                UpdateRealtimeDistances(_currentLocation);
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                // ═══════════════════════════════════════════════════════════════
                // 🔥 FIX #14: INIT AUDIO AN TOÀN
                // ═══════════════════════════════════════════════════════════════
                // Bọc trong try-catch riêng để lỗi Audio KHÔNG chặn Geofence start
                // ═══════════════════════════════════════════════════════════════
                try
                {
                    await _audioService.InitializeAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GeofenceService] Audio init lỗi (Geofence vẫn chạy): {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GeofenceService] ❌ InitializeAsync error: {ex.Message}");
            }
        }

        public void StartMonitoring()
        {
            if (_monitoringCts != null && !_monitoringCts.IsCancellationRequested) return;

            _monitoringCts = new CancellationTokenSource();
            Task.Run(async () => await MonitoringLoopAsync(_monitoringCts.Token));

            System.Diagnostics.Debug.WriteLine("[GeofenceService] ✅ Monitoring STARTED");
        }

        public void StopMonitoring()
        {
            _monitoringCts?.Cancel();
            System.Diagnostics.Debug.WriteLine("[GeofenceService] ⏹ Monitoring STOPPED");
        }

        private async Task MonitoringLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(15));

                    // Ưu tiên lấy tọa độ mới, nếu timeout/lỗi thì lấy tọa độ cache gần nhất
                    var location = await Geolocation.Default.GetLocationAsync(request)
                                   ?? await Geolocation.Default.GetLastKnownLocationAsync();

                    if (location != null)
                    {
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
                    System.Diagnostics.Debug.WriteLine($"GPS Loop Error: {ex.Message}");
                }

                // ═══════════════════════════════════════════════════════════════
                // 🔥 FIX #15: CATCH OperationCanceledException KHI DELAY
                // ═══════════════════════════════════════════════════════════════
                // Khi StopMonitoring gọi Cancel() trong lúc Task.Delay đang chờ
                // → throw OperationCanceledException → loop thoát sạch
                // ═══════════════════════════════════════════════════════════════
                try
                {
                    await Task.Delay(3000, token);
                }
                catch (OperationCanceledException)
                {
                    break; // Thoát loop sạch sẽ
                }
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

                    string formatMeters = client.Resources.String.AppResources.HomeDistanceMeters ?? "Cách bạn {0}m";
                    string formatKm = client.Resources.String.AppResources.HomeDistanceKm ?? "Cách bạn {0}km";

                    if (distMeters < 1000)
                    {
                        poi.DistanceDisplay = string.Format(formatMeters, Math.Round(distMeters));
                    }
                    else
                    {
                        poi.DistanceDisplay = string.Format(formatKm, distKm.ToString("F1"));
                    }
                });
            }
        }

        private void CheckGeofences(Location currentLoc)
        {
            if (_allPOIs == null || !_allPOIs.Any()) return;

            var nearestPoi = _allPOIs.OrderBy(p =>
                Location.CalculateDistance(
                    currentLoc.Latitude, currentLoc.Longitude,
                    p.Latitude, p.Longitude, DistanceUnits.Kilometers)
            ).FirstOrDefault();

            if (nearestPoi != null)
            {
                double distanceInMeters = Location.CalculateDistance(
                    currentLoc.Latitude, currentLoc.Longitude,
                    nearestPoi.Latitude, nearestPoi.Longitude, DistanceUnits.Kilometers) * 1000;

                // 1. Logic khi ĐANG KHÔNG Ở TRONG QUÁN NÀO (Tìm quán để vào)
                if (CurrentActivePOI == null)
                {
                    if (distanceInMeters <= 20) // Ngưỡng vào: 20 mét
                    {
                        System.Diagnostics.Debug.WriteLine($"!!! VÀO VÙNG QUÁN MỚI: {nearestPoi.Name} (Cách {distanceInMeters:F1}m)");
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            CurrentActivePOI = nearestPoi;
                        });
                    }
                }
                // 2. Logic khi ĐANG Ở TRONG MỘT QUÁN (Kiểm tra xem đã thực sự ra khỏi quán chưa)
                // 2. Logic khi ĐANG Ở TRONG MỘT QUÁN (Kiểm tra xem đã thực sự ra khỏi quán chưa)
                else
                {
                    // Nếu quán gần nhất đo được vẫn là quán đang active
                    if (CurrentActivePOI.PoiId == nearestPoi.PoiId)
                    {
                        if (distanceInMeters > 30) // Ngưỡng thoát: 30 mét
                        {
                            System.Diagnostics.Debug.WriteLine($"!!! ĐÃ ĐI RA KHỎI KHU VỰC QUÁN {CurrentActivePOI.Name}");
                            MainThread.BeginInvokeOnMainThread(() => CurrentActivePOI = null);
                        }
                    }
                    // Trường hợp hệ thống nhận diện một quán MỚI nằm sát quán cũ (chuyển quán)
                    else
                    {
                        // TÍNH KHOẢNG CÁCH CỦA QUÁN HIỆN TẠI ĐỂ LÀM MỐC SO SÁNH
                        double currentActiveDist = Location.CalculateDistance(
                            currentLoc.Latitude, currentLoc.Longitude,
                            CurrentActivePOI.Latitude, CurrentActivePOI.Longitude, DistanceUnits.Kilometers) * 1000;

                        // 🔥 VÙNG ĐỆM: Quán mới phải <= 20m VÀ phải gần hơn quán cũ ít nhất 3 mét mới cho đổi
                        if (distanceInMeters <= 20 && distanceInMeters < (currentActiveDist - 3))
                        {
                            System.Diagnostics.Debug.WriteLine($"!!! CHUYỂN SANG VÙNG QUÁN KHÁC: {nearestPoi.Name} (Cách {distanceInMeters:F1}m)");
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                CurrentActivePOI = nearestPoi;
                            });
                        }
                    }
                }
            }
        }
    }
}