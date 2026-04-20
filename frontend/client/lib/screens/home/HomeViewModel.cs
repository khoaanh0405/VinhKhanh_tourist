using client.lib.core;
using client.lib.model;
using client.lib.screens.poi;
using client.lib.services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Microsoft.Maui.Devices.Sensors;
using System.Globalization;

namespace client.lib.screens.home
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly GeofenceService _geofenceService;
        private readonly AudioService _audioService;
        private readonly ApiService _apiService;
        private CancellationTokenSource? _ttsDelayCts;
        private int? _lastPlayedPoiId = null;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private ObservableCollection<POI> _pois = new();

        [ObservableProperty]
        private ObservableCollection<POI> _featuredPois = new();

        [ObservableProperty]
        private bool _isAutoNarrationEnabled;

        [ObservableProperty]
        private ObservableCollection<Language> _availableLanguages = new();

        private Language _selectedLanguage;
        public Language SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value) && value != null)
                {
                    ApplyLanguageChange(value.LanguageCode);
                }
            }
        }

        public POI? CurrentActivePOI => _geofenceService.CurrentActivePOI;
        public bool IsPlaying => CurrentActivePOI != null;
        public AudioService Audio => _audioService;
        public string? CurrentActivePOIImageUrl => _geofenceService.CurrentActivePOI?.ImageUrl;

        public string ExploreTitle => client.Resources.String.AppResources.ExploreTitle;
        public string AllLocations => client.Resources.String.AppResources.AllLocations;

        public HomeViewModel(
            GeofenceService geofenceService,
            AudioService audioService,
            ApiService apiService)
        {
            _geofenceService = geofenceService;
            _audioService = audioService;
            _apiService = apiService;

            _isAutoNarrationEnabled = Preferences.Get("AutoNarration", false);

            Task.Run(async () =>
            {
                // 1. Lấy danh sách ngôn ngữ để hiện lên Picker
                var langs = await _apiService.GetAvailableLanguagesAsync();

                if (langs != null && langs.Any())
                {
                    // Lấy mã ngôn ngữ đang lưu trong máy (mặc định là 'vi')
                    string savedLangCode = Preferences.Get("AppLanguage", "vi");

                    // 2. 🔥 QUAN TRỌNG: Gọi API lấy bộ từ điển của ngôn ngữ đó ngay và luôn
                    var translations = await _apiService.GetUITranslationsAsync(savedLangCode);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // Nạp danh sách vào Picker
                        AvailableLanguages.Clear();
                        foreach (var l in langs)
                        {
                            AvailableLanguages.Add(new Language { LanguageCode = l.LanguageCode, LanguageName = l.LanguageName });
                        }

                        // Nạp từ điển vào bộ quản lý để UI hiện chữ thay vì hiện mã [Key]
                        LocalizationResourceManager.Instance.SetTranslations(translations);

                        // Chọn ngôn ngữ đang dùng
                        SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.LanguageCode == savedLangCode)
                                           ?? AvailableLanguages.FirstOrDefault();
                    });
                }
            });

            // 3. ĐOẠN CODE LẮNG NGHE GEOFENCE
            // 3. ĐOẠN CODE LẮNG NGHE GEOFENCE
            _geofenceService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(GeofenceService.CurrentActivePOI))
                {
                    OnPropertyChanged(nameof(CurrentActivePOI));
                    OnPropertyChanged(nameof(IsPlaying));
                    OnPropertyChanged(nameof(CurrentActivePOIImageUrl));

                    var activePoi = CurrentActivePOI;

                    // 1. TRƯỜNG HỢP ĐI RA KHỎI QUÁN (Hoặc app vừa khởi động chưa có vị trí)
                    if (activePoi == null)
                    {
                        _lastPlayedPoiId = null;        // Xóa cờ để lần sau quay lại đọc tiếp
                        _ttsDelayCts?.Cancel();         // Hủy ngay lệnh đếm ngược (nếu đang chạy)
                        return;
                    }

                    // 2. TRƯỜNG HỢP ĐI VÀO QUÁN
                    // 2. TRƯỜNG HỢP ĐI VÀO QUÁN
                    if (IsAutoNarrationEnabled)
                    {
                        // KIỂM TRA: Nếu quán này vừa được ra lệnh đọc rồi -> BỎ QUA 
                        if (_lastPlayedPoiId == activePoi.PoiId)
                            return;

                        // 🔥 FIX: Cắm cờ NGAY LẬP TỨC ở đây thay vì chờ 1.5s.
                        // Khóa cổng lại, nếu 1.5s tới GPS có giật/nháy sinh ra sự kiện ảo thì cũng bị chặn lại.
                        _lastPlayedPoiId = activePoi.PoiId;

                        // HỦY BỎ LỆNH DELAY TRƯỚC ĐÓ
                        _ttsDelayCts?.Cancel();
                        _ttsDelayCts = new CancellationTokenSource();
                        var token = _ttsDelayCts.Token;

                        Task.Run(async () =>
                        {
                            try
                            {
                                // Đợi 1.5s xác nhận người dùng thật sự đứng ở quán
                                await Task.Delay(1500, token);

                                // Nếu qua 1.5s mà người dùng không bấm Tắt và vẫn đang ở trong quán đó
                                // Nếu qua 1.5s mà người dùng không bấm Tắt và vẫn đang ở trong quán đó
                                if (CurrentActivePOI?.PoiId == activePoi.PoiId && IsAutoNarrationEnabled)
                                {
                                    try
                                    {
                                        string currentLang = Preferences.Get("AppLanguage", "vi");

                                        // 🔥 GỌI API LẤY "NÓNG" DATA MỚI NHẤT TỪ ADMIN
                                        var freshPoiData = await _apiService.FetchPOIByIdAsync(activePoi.PoiId, currentLang);

                                        // Kiểm tra xem có lấy được data mới không
                                        if (freshPoiData != null && freshPoiData.Narrations != null && freshPoiData.Narrations.Any())
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[TTS] Có mạng -> Đọc text MỚI TỪ SERVER!");
                                            await PlayNarrationAsync(freshPoiData); // Quăng data mới vào cho hàm đọc
                                        }
                                        else
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[TTS] Không lấy được text mới -> Đọc text CŨ trong máy!");
                                            await PlayNarrationAsync(activePoi); // Dự phòng: Dùng data cũ
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[HomeViewModel] Rớt mạng/Lỗi -> Đọc text CŨ: {ex.Message}");
                                        // Dự phòng khi rớt mạng hoặc lỗi API: Vẫn đọc text cũ trong RAM để không bị im lặng
                                        await PlayNarrationAsync(activePoi);
                                    }
                                }
                            }
                            catch (TaskCanceledException)
                            {
                                // Lệnh chờ 1.5s bị hủy -> Im lặng bỏ qua
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[HomeViewModel] ❌ PlayNarrationAsync FAILED ngầm: {ex.Message}");
                            }
                        });
                    }
                }
            };
        }

        private void UpdateCulture(string langCode)
        {
            var culture = new CultureInfo(langCode);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            client.Resources.String.AppResources.Culture = culture;
        }

        private void ApplyLanguageChange(string langCode)
        {
            Preferences.Set("AppLanguage", langCode);
            UpdateCulture(langCode);

            // 🔥 ĐÃ XÓA: _appViewModel.RefreshTranslations();
            OnPropertyChanged(string.Empty);

            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)

            {
                Task.Run(async () => await LoadDataAsync());
            }
        }

        [RelayCommand]
        public async Task NavigateToActivePOIAsync()
        {
            if (_geofenceService.CurrentActivePOI != null)
            {
                await Application.Current.MainPage.Navigation.PushAsync(
                    new POIDetailPage(_geofenceService.CurrentActivePOI.PoiId, autoPlayAudio: false));
            }
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    System.Diagnostics.Debug.WriteLine("[HomeViewModel] Không có mạng → bỏ qua LoadDataAsync");
                    return;
                }

                string currentLang = Preferences.Get("AppLanguage", "vi");
                var fetchedPois = await _apiService.FetchPOIsAsync(currentLang);

                if (fetchedPois != null && fetchedPois.Any())
                {
                    try
                    {
                        var location = await Geolocation.GetLastKnownLocationAsync()
                                       ?? await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));

                        if (location != null)
                        {
                            foreach (var poi in fetchedPois)
                            {
                                double distKm = Location.CalculateDistance(location.Latitude, location.Longitude,
                                                                           poi.Latitude, poi.Longitude, DistanceUnits.Kilometers);
                                double distMeters = distKm * 1000;
                                poi.DistanceInMeters = distMeters;

                                string formatM = client.lib.core.LocalizationResourceManager.Instance["HomeDistanceMeters"];
                                string formatKm = client.lib.core.LocalizationResourceManager.Instance["HomeDistanceKm"];

                                if (distMeters < 1000)
                                    poi.DistanceDisplay = string.Format(formatM, Math.Round(distMeters));
                                else
                                    poi.DistanceDisplay = string.Format(formatKm, distKm.ToString("F1"));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[HomeViewModel] GPS error trong LoadDataAsync: {ex.Message}");
                    }

                    Pois = new ObservableCollection<POI>(fetchedPois.OrderBy(p => p.DistanceInMeters));
                    _lastPlayedPoiId = null;
                    _geofenceService.SetPois(fetchedPois);

                    FeaturedPois = new ObservableCollection<POI>(fetchedPois.Take(3));

                    await _geofenceService.InitializeAsync();
                    _geofenceService.StartMonitoring();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomeViewModel] ❌ LoadDataAsync FAILED: {ex.Message}");
            }
            finally { IsLoading = false; }
        }

        private async Task PlayNarrationAsync(POI poi)
        {
            if (poi.Narrations == null || !poi.Narrations.Any()) return;

            string langCode = Preferences.Get("AppLanguage", "vi");
            var narration = poi.Narrations.FirstOrDefault(n => n.LanguageCode == langCode)
                         ?? poi.Narrations.FirstOrDefault();

            if (narration != null && !string.IsNullOrWhiteSpace(narration.Text))
            {
                // SỬA DÒNG DƯỚI ĐÂY: Dùng narration.LanguageCode thay vì langCode
                string speakLang = narration.LanguageCode ?? langCode;
                await _audioService.SpeakAsync(narration.Text, poi.Name ?? "Thuyết minh", speakLang);
            }
        }

        [RelayCommand] // (Giữ nguyên Attribute này nếu ViewModel của bạn đang dùng CommunityToolkit.Mvvm)
        public async Task ToggleAutoNarrationAsync()
        {
            // Đưa biến loc lên đầu để xài chung cho toàn bộ hàm
            var loc = client.lib.core.LocalizationResourceManager.Instance;

            if (!IsAutoNarrationEnabled)
            {
                // Hỏi xác nhận BẬT
                bool confirm = await Application.Current.MainPage.DisplayAlert(
                    loc["Alert_AutoAudioTitle"],
                    loc["Alert_AutoAudioDesc"],
                    loc["Btn_TurnOn"],
                    loc["Btn_Cancel"]
                );

                if (confirm)
                {
                    IsAutoNarrationEnabled = true;
                    Preferences.Set("AutoNarration", true);

                    // Thông báo BẬT thành công
                    await Application.Current.MainPage.DisplayAlert(
                        loc["Alert_Success"],
                        loc["Alert_AutoAudioOnSuccess"],
                        loc["Btn_OK"]
                    );

                    if (CurrentActivePOI != null) await PlayNarrationAsync(CurrentActivePOI);
                }
            }
            else
            {
                // Hỏi xác nhận TẮT
                bool confirm = await Application.Current.MainPage.DisplayAlert(
                    loc["Alert_TurnOffAudioTitle"],
                    loc["Alert_TurnOffAudioDesc"],
                    loc["Btn_TurnOff"],
                    loc["Btn_Cancel"]
                );

                if (confirm)
                {
                    IsAutoNarrationEnabled = false;
                    Preferences.Set("AutoNarration", false);

                    if (Audio.IsSpeaking) Audio.Stop();

                    // Thông báo TẮT thành công
                    await Application.Current.MainPage.DisplayAlert(
                        loc["Alert_Success"],
                        loc["Alert_AutoAudioOffSuccess"],
                        loc["Btn_OK"]
                    );
                }
            }
        }

        public async Task RefreshGeofenceWithOfflineData(IEnumerable<POI> pois)
        {
            if (pois == null || !pois.Any()) return;

            _lastPlayedPoiId = null;

            _geofenceService.SetPois(pois.ToList());
            await _geofenceService.InitializeAsync();
            _geofenceService.StartMonitoring();
        }
    }
}