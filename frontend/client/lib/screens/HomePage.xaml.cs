using client.lib.model;
using client.lib.screens.home;
using client.lib.screens.poi;
using client.lib.screens.search;
using client.lib.services;
using System.Collections.ObjectModel;
using System.Linq;

namespace client.lib.screens
{
    public partial class HomePage : ContentPage
    {
        private HomeViewModel _viewModel;
        private readonly LocalDbService _localDb = new LocalDbService();

        public HomePage(HomeViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        private async void OnProfileIconTapped(object sender, TappedEventArgs e)
        {
            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);

            if (isLoggedIn)
            {
                await Shell.Current.GoToAsync("ProfileScreen");
            }
            else
            {
                await Shell.Current.GoToAsync("LoginScreen");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);

            if (isLoggedIn)
            {
                ProfileIconBorder.BackgroundColor = Color.FromArgb("#2ECC71");
                ProfileIconPath.Fill = Brush.White;

                if (_viewModel != null)
                {
                    _viewModel.IsAutoNarrationEnabled = Preferences.Get("AutoNarration", false);
                }
            }
            else
            {
                ProfileIconBorder.BackgroundColor = Color.FromArgb("#000000");
                ProfileIconPath.Fill = Brush.White;

                if (_viewModel != null)
                {
                    _viewModel.IsAutoNarrationEnabled = false;
                    Preferences.Set("AutoNarration", false);
                }
            }

            if (_viewModel != null)
            {
                string savedLang = Preferences.Get("AppLanguage", "vi");

                if (_viewModel.SelectedLanguage == null || _viewModel.SelectedLanguage.LanguageCode != savedLang)
                {
                    var matchedLang = _viewModel.AvailableLanguages.FirstOrDefault(l => l.LanguageCode == savedLang);
                    if (matchedLang != null)
                    {
                        _viewModel.SelectedLanguage = matchedLang;
                    }
                }

                // ═══════════════════════════════════════════════════════════════
                // 🔥 FIX #1: OFFLINE-FIRST STARTUP
                // ═══════════════════════════════════════════════════════════════
                // NGUYÊN NHÂN LỖI CŨ:
                //   - Code cũ kiểm tra mạng rồi mới quyết định load offline hay online
                //   - Khi offline, gọi LoadDataAsync() có thể TREO vì ApiService timeout
                //   - Nếu API timeout chưa xử lý tốt → app ĐỨNG tại OnAppearing → UI KHÔNG HIỆN
                //
                // FIX MỚI:
                //   - LUÔN load SQLite trước (nhanh, không phụ thuộc mạng)
                //   - Sau đó nếu có mạng → gọi API để cập nhật dữ liệu mới nhất (background)
                //   - Geofence được init NGAY với dữ liệu offline
                // ═══════════════════════════════════════════════════════════════

                if (_viewModel.Pois == null || !_viewModel.Pois.Any())
                {
                    // BƯỚC 1: LUÔN load SQLite trước — đây là nguồn dữ liệu chính
                    await LoadDataOfflineFirst();

                    // BƯỚC 2: Nếu có mạng VÀ đã có dữ liệu offline → cập nhật API ở background
                    // Nếu chưa có dữ liệu offline (app mới cài) thì LoadDataOfflineFirst đã xử lý rồi
                    bool hasInternet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
                    if (hasInternet && _viewModel.Pois != null && _viewModel.Pois.Any())
                    {
                        // Fire-and-forget: cập nhật data mới từ server KHÔNG chặn UI
                        _ = _viewModel.LoadDataAsync();
                    }
                }
            }
        }

        private async Task LoadDataOfflineFirst()
        {
            try
            {
                // 1. Đẩy TOÀN BỘ vòng lặp nặng (đọc DB, check File IO) xuống luồng nền
                var result = await Task.Run(async () =>
                {
                    var offlinePois = await _localDb.GetAllPoisAsync();
                    var mappedPois = new List<POI>(); // Dùng List thay vì ObservableCollection khi ở luồng nền

                    if (offlinePois != null && offlinePois.Count > 0)
                    {
                        // Lấy GPS (Hàm này có thể chạy ở background)
                        Location location = null;
                        try
                        {
                            location = await Geolocation.GetLastKnownLocationAsync()
                                       ?? await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));
                        }
                        catch { /* Bỏ qua lỗi GPS */ }

                        // Vòng lặp N+1 bây giờ sẽ chạy ở luồng nền, không làm đơ giao diện
                        foreach (var lp in offlinePois)
                        {
                            var poi = new POI
                            {
                                PoiId = lp.PoiId,
                                Name = lp.Name,
                                Description = lp.Description,
                                AverageRating = lp.AverageRating,
                                ReviewCount = lp.ReviewCount,
                                Latitude = lp.Latitude,
                                Longitude = lp.Longitude,
                                ImageUrls = new List<string>
                                    {
                                        !string.IsNullOrEmpty(lp.ImageUrlsJoined) ? lp.ImageUrlsJoined.Split(',')[0] : "default_thumbnail.jpg"
                                    },
                                DistanceDisplay = "Đang tính...",

                            };

                            // Truy vấn DB từng POI
                            var localNarrations = await _localDb.GetNarrationsByPoiIdAsync(lp.PoiId);
                            poi.Narrations = new List<Narration>();

                            if (localNarrations != null && localNarrations.Any())
                            {
                                foreach (var ln in localNarrations)
                                {
                                    bool isLocalFile = !string.IsNullOrEmpty(ln.AudioUrl)
                                        && !ln.AudioUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase);

                                    // Check File I/O
                                    bool localFileExists = isLocalFile && System.IO.File.Exists(ln.AudioUrl);

                                    poi.Narrations.Add(new Narration
                                    {
                                        NarrationId = ln.NarrationId,
                                        LanguageCode = ln.LanguageCode,
                                        Text = ln.Text,
                                        AudioUrl = ln.AudioUrl,
                                        UseAudioFile = localFileExists
                                    });
                                }
                            }

                            // Tính GPS
                            if (location != null)
                            {
                                double distKm = Location.CalculateDistance(location.Latitude, location.Longitude,
                                                                           poi.Latitude, poi.Longitude, DistanceUnits.Kilometers);
                                double distMeters = distKm * 1000;
                                poi.DistanceInMeters = distMeters;

                                // Tạm dùng string format trực tiếp để tránh gọi LocalizationResourceManager ở background thread
                                if (distMeters < 1000)
                                    poi.DistanceDisplay = $"{Math.Round(distMeters)} m";
                                else
                                    poi.DistanceDisplay = $"{distKm.ToString("F1")} km";
                            }

                            mappedPois.Add(poi);
                        }

                        // Sắp xếp dữ liệu trước khi trả về
                        var sortedPois = mappedPois.OrderBy(p => p.DistanceInMeters).ToList();
                        var featured = mappedPois.OrderByDescending(p => p.AverageRating).Take(5).ToList();

                        return new { Sorted = sortedPois, Featured = featured };
                    }
                    return null; // Trả về null nếu DB rỗng
                });

                // 2. Quay lại luồng chính (Main Thread) ĐỂ CẬP NHẬT GIAO DIỆN
                if (result != null)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        _viewModel.Pois = new ObservableCollection<POI>(result.Sorted);
                        _viewModel.FeaturedPois = new ObservableCollection<POI>(result.Featured);

                        // Geofence khởi động cực mượt sau khi UI đã có data
                        await _viewModel.RefreshGeofenceWithOfflineData(_viewModel.Pois);
                    });
                }
                else
                {
                    // Trường hợp SQLite rỗng (App mới cài)
                    if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                    {
                        await _viewModel.LoadDataAsync();
                    }
                    else
                    {
                        await DisplayAlert("Lỗi mạng", "Bạn cần kết nối mạng lần đầu để tải danh sách quán ăn.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomePage] Lỗi load offline: {ex.Message}");
                if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                {
                    try
                    {
                        await _viewModel.LoadDataAsync();
                    }
                    catch (Exception innerEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[HomePage] Fallback API cũng thất bại: {innerEx.Message}");
                    }
                }
            }
        }

        private async Task NavigateToDetailAsync(object sender)
        {
            var element = sender as View;

            var selectedPOI = (element?.GestureRecognizers.FirstOrDefault() as TapGestureRecognizer)?.CommandParameter as POI
                              ?? element?.BindingContext as POI;

            if (selectedPOI != null)
            {
                await Navigation.PushAsync(new POIDetailPage(selectedPOI.PoiId, autoPlayAudio: false));
            }
        }

        public async void OnNavigateToDetail(object sender, EventArgs e)
        {
            await NavigateToDetailAsync(sender);
        }

        private async void OnSearchTapped(object sender, TappedEventArgs e)
        {
            if (_viewModel != null && _viewModel.Pois != null)
            {
                var searchViewModel = new SearchViewModel(_viewModel.Pois);
                await Navigation.PushAsync(new client.lib.screens.search.SearchPage(searchViewModel));
            }
        }
    }
}