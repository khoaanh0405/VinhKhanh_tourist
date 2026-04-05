using client.lib.model;
using client.lib.services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Devices.Sensors;
using System.Globalization;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace client.lib.screens.home
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly GeofenceService _geofenceService;
        private readonly AudioService _audioService;
        private readonly ApiService _apiService;
        private readonly AppViewModel _appViewModel;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private ObservableCollection<POI> _pois = new();

        [ObservableProperty]
        private ObservableCollection<POI> _filteredPois = new();

        [ObservableProperty]
        private bool _isSearching;

        [ObservableProperty]
        private ObservableCollection<POI> _featuredPois = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isAutoNarrationEnabled;

        // [THÊM MỚI 1] Danh sách ngôn ngữ cho giao diện chọn
        [ObservableProperty]
        private ObservableCollection<Language> _availableLanguages = new();

        // [THÊM MỚI 2] Xử lý khi người dùng chọn ngôn ngữ mới
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

        // Bọc các chuỗi Resource tĩnh thành các property của ViewModel để UI có thể Binding tự động cập nhật
        public string ExploreTitle => client.Resources.String.AppResources.ExploreTitle;
        public string SearchPlaceholder => client.Resources.String.AppResources.SearchPlaceholder;
        public string TopFavorites => client.Resources.String.AppResources.TopFavorites;
        public string AllLocations => client.Resources.String.AppResources.AllLocations;
        public string NotFoundText => client.Resources.String.AppResources.NotFoundText;

        public HomeViewModel(
            GeofenceService geofenceService,
            AudioService audioService,
            ApiService apiService,
            AppViewModel appViewModel)
        {
            _geofenceService = geofenceService;
            _audioService = audioService;
            _apiService = apiService;
            _appViewModel = appViewModel;

            _isAutoNarrationEnabled = Preferences.Get("AutoNarration", false);

            // [THÊM MỚI 3] Khởi tạo danh sách ngôn ngữ
            AvailableLanguages = new ObservableCollection<Language>
            {
                new Language { LanguageCode = "vi", LanguageName = "Tiếng Việt 🇻🇳" },
                new Language { LanguageCode = "en", LanguageName = "English 🇬🇧" },
                new Language { LanguageCode = "ko", LanguageName = "한국어 🇰🇷" }
            };

            // Lấy ngôn ngữ đã lưu hoặc mặc định là "vi"
            string savedLang = Preferences.Get("AppLanguage", "vi");
            _selectedLanguage = AvailableLanguages.FirstOrDefault(l => l.LanguageCode == savedLang) ?? AvailableLanguages.First();
            UpdateCulture(savedLang); // Chỉ set Culture, chưa gọi API (API sẽ được gọi bên HomePage.xaml.cs OnAppearing)

            _geofenceService.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(GeofenceService.CurrentActivePOI))
                {
                    OnPropertyChanged(nameof(CurrentActivePOI));
                    OnPropertyChanged(nameof(IsPlaying));
                    OnPropertyChanged(nameof(CurrentActivePOIImageUrl));

                    if (CurrentActivePOI != null)
                    {
                        bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
                        if (isLoggedIn && IsAutoNarrationEnabled)
                        {
                            await PlayNarrationAsync(CurrentActivePOI);
                        }
                    }
                }
            };
        }

        // [THÊM MỚI 4] Cập nhật Culture cho file .resx
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

            if (_appViewModel != null)
            {
                _appViewModel.CurrentLanguageCode = langCode;
                _appViewModel.RefreshTranslations();
            }

            OnPropertyChanged(string.Empty);

            // ĐÃ XÓA ĐOẠN GỌI shell.UpdateTabsLanguage() Ở ĐÂY VÌ APP CHỈ CÒN DÙNG ICON

            Task.Run(async () => await LoadDataAsync());
        }

        private CancellationTokenSource? _searchCts;

        partial void OnSearchTextChanged(string value)
        {
            ExecuteDebouncedSearch(value);
        }

        private async void ExecuteDebouncedSearch(string query)
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                await Task.Delay(800, token);
                if (token.IsCancellationRequested) return;

                bool hasText = !string.IsNullOrWhiteSpace(query);

                if (!hasText)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        IsSearching = false;
                        FilteredPois.Clear();
                    });
                    return;
                }

                var result = await Task.Run(() =>
                {
                    return Pois.Where(p =>
                        ContainsIgnoreCase(p.Name, query) ||
                        (p.Restaurants != null && p.Restaurants.Any(r =>
                            r.Foods != null && r.Foods.Any(f => ContainsIgnoreCase(f.Name, query))
                        ))
                    ).ToList();
                });

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    FilteredPois.Clear();
                    foreach (var item in result) FilteredPois.Add(item);
                    IsSearching = true;
                });
            }
            catch (TaskCanceledException) { }
        }

        private bool ContainsIgnoreCase(string? source, string query)
        {
            if (string.IsNullOrEmpty(source)) return false;
            return CultureInfo.CurrentCulture.CompareInfo.IndexOf(
                source, query,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
        }

        [RelayCommand]
        public async Task NavigateToActivePOIAsync()
        {
            if (_geofenceService.CurrentActivePOI != null)
            {
                await Application.Current.MainPage.Navigation.PushAsync(new DetailScreen(_geofenceService.CurrentActivePOI));
            }
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                // [SỬA ĐỔI] Truyền mã ngôn ngữ vào hàm FetchPOIsAsync
                string currentLang = SelectedLanguage?.LanguageCode ?? "vi";
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

                                // Lấy chuỗi định dạng từ file ngôn ngữ hiện tại
                                string formatM = client.lib.core.LocalizationResourceManager.Instance["HomeDistanceMeters"];
                                string formatKm = client.lib.core.LocalizationResourceManager.Instance["HomeDistanceKm"];

                                if (distMeters < 1000)
                                    poi.DistanceDisplay = string.Format(formatM, Math.Round(distMeters));
                                else
                                    // Dùng ToString("F1") để lấy 1 chữ số thập phân
                                    poi.DistanceDisplay = string.Format(formatKm, distKm.ToString("F1"));
                            }
                        }
                    }
                    catch (Exception) { /* Bỏ qua nếu tắt GPS */ }

                    Pois = new ObservableCollection<POI>(fetchedPois.OrderBy(p => p.DistanceInMeters));
                    _geofenceService.SetPois(fetchedPois);

                    _appViewModel.UpdateFavoritesTranslations(fetchedPois);

                    var topPois = fetchedPois.OrderByDescending(p => p.AverageRating).Take(5).ToList();
                    FeaturedPois = new ObservableCollection<POI>(topPois);

                    await _geofenceService.InitializeAsync();
                    _geofenceService.StartMonitoring();
                }
            }
            finally { IsLoading = false; }
        }

        private async Task PlayNarrationAsync(POI poi)
        {
            if (poi.Narrations == null || !poi.Narrations.Any()) return;

            // [SỬA ĐỔI] Lấy đúng ngôn ngữ đang được chọn hiện tại
            string langCode = SelectedLanguage?.LanguageCode ?? "vi";
            var narration = poi.Narrations.FirstOrDefault(n => n.LanguageCode == langCode)
                            ?? poi.Narrations.FirstOrDefault();

            if (narration != null)
            {
                if (narration.UseAudioFile && !string.IsNullOrEmpty(narration.AudioUrl))
                {
                    await _audioService.PlayAudioFromUrlAsync(narration.AudioUrl, poi.Name);
                }
                else
                {
                    // [SỬA ĐỔI] Truyền langCode vào hàm SpeakAsync để máy đọc giọng Anh/Hàn/Việt tương ứng
                    await _audioService.SpeakAsync(narration.Text, poi.Name, langCode);
                }
            }
        }

        [RelayCommand]
        public async Task ToggleAutoNarrationAsync()
        {
            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
            if (!isLoggedIn)
            {
                bool wantToLogin = await Application.Current.MainPage.DisplayAlert(
                    "Tính năng giới hạn",
                    "Bạn cần đăng nhập để sử dụng tính năng tự động thuyết minh khu vực. Đăng nhập ngay?",
                    "Đồng ý", "Để sau");

                if (wantToLogin) await Shell.Current.GoToAsync("LoginScreen");
                return;
            }

            if (!IsAutoNarrationEnabled)
            {
                bool confirm = await Application.Current.MainPage.DisplayAlert(
                    "Bật tự động thuyết minh",
                    "Ứng dụng sẽ tự động phát âm thanh giới thiệu chi tiết khi bạn đi ngang qua các địa điểm. Bạn có muốn kích hoạt?",
                    "Bật", "Hủy");

                if (confirm)
                {
                    IsAutoNarrationEnabled = true;
                    Preferences.Set("AutoNarration", true);
                    await Application.Current.MainPage.DisplayAlert("Thành công", "Đã BẬT thuyết minh tự động 🎧", "OK");

                    if (CurrentActivePOI != null) await PlayNarrationAsync(CurrentActivePOI);
                }
            }
            else
            {
                bool confirm = await Application.Current.MainPage.DisplayAlert(
                    "Tắt tự động thuyết minh",
                    "Bạn có muốn tắt tính năng tự động phát âm thanh giới thiệu không?",
                    "Tắt", "Hủy");

                if (confirm)
                {
                    IsAutoNarrationEnabled = false;
                    Preferences.Set("AutoNarration", false);
                    if (Audio.IsSpeaking) Audio.Stop();
                    await Application.Current.MainPage.DisplayAlert("Thành công", "Đã TẮT thuyết minh tự động 🔇", "OK");
                }
            }
        }
    }
}