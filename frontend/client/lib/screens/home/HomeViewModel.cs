using client.lib.core;
using client.lib.model;
using client.lib.screens.poi;
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

            AvailableLanguages = new ObservableCollection<Language>
            {
                new Language { LanguageCode = "vi", LanguageName = "Tiếng Việt 🇻🇳" },
                new Language { LanguageCode = "en", LanguageName = "English 🇬🇧" },
                new Language { LanguageCode = "ko", LanguageName = "한국어 🇰🇷" }
            };

            string savedLang = Preferences.Get("AppLanguage", "vi");
            _selectedLanguage = AvailableLanguages.FirstOrDefault(l => l.LanguageCode == savedLang) ?? AvailableLanguages.First();
            UpdateCulture(savedLang);

            _geofenceService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(GeofenceService.CurrentActivePOI))
                {
                    // 1. Cập nhật UI (vẫn chạy trên Main Thread)
                    OnPropertyChanged(nameof(CurrentActivePOI));
                    OnPropertyChanged(nameof(IsPlaying));
                    OnPropertyChanged(nameof(CurrentActivePOIImageUrl));

                    var activePoi = CurrentActivePOI;
                    if (activePoi != null)
                    {
                        bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
                        if (isLoggedIn && IsAutoNarrationEnabled)
                        {
                            // 2. 🔥 Đẩy việc phát âm thanh xuống luồng nền để tránh lỗi NetworkOnMainThread
                            Task.Run(async () =>
                            {
                                try
                                {
                                    if (activePoi.Narrations != null && activePoi.Narrations.Any())
                                    {
                                        // PlayNarrationAsync sẽ tự xử lý logic File vs TTS bên trong
                                        await PlayNarrationAsync(activePoi);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine(
                                        $"[HomeViewModel] ❌ PlayNarrationAsync FAILED ngầm: {ex.Message}");
                                }
                            });
                        }
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

            if (_appViewModel != null)
            {
                _appViewModel.CurrentLanguageCode = langCode;
                _appViewModel.RefreshTranslations();
            }

            OnPropertyChanged(string.Empty);

            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                Task.Run(async () => await LoadDataAsync());
            }
        }

        private CancellationTokenSource? _searchCts;

        partial void OnSearchTextChanged(string value)
        {
            ExecuteDebouncedSearch(value);
        }

        // ══════════════════════════════════════════════════════════════
        // 🔥 NÂNG CẤP: Sử dụng FuzzySearchHelper thay vì ContainsIgnoreCase
        // ══════════════════════════════════════════════════════════════
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
                    string normalizedQuery = FuzzySearchHelper.NormalizeText(query);

                    return Pois
                        .Select(p => new
                        {
                            Poi = p,
                            Score = FuzzySearchHelper.CalculateRelevanceScore(
                                name: p.Name,
                                description: null, // HomePage chỉ tìm theo tên + món ăn
                                foodNames: p.Restaurants?
                                    .Where(r => r.Foods != null)
                                    .SelectMany(r => r.Foods.Select(f => f.Name)),
                                normalizedQuery: normalizedQuery
                            )
                        })
                        .Where(x => x.Score > 0)
                        .OrderByDescending(x => x.Score)
                        .ThenByDescending(x => x.Poi.AverageRating)
                        .Select(x => x.Poi)
                        .ToList();
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
                    _geofenceService.SetPois(fetchedPois);

                    _appViewModel.UpdateFavoritesTranslations(fetchedPois);

                    var topPois = fetchedPois.OrderByDescending(p => p.AverageRating).Take(5).ToList();
                    FeaturedPois = new ObservableCollection<POI>(topPois);

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
            if (poi.Narrations == null || !poi.Narrations.Any())
            {
                System.Diagnostics.Debug.WriteLine($"[PlayNarration] ❌ POI '{poi.Name}' không có Narrations");
                return;
            }

            string langCode = SelectedLanguage?.LanguageCode ?? "vi";
            var narration = poi.Narrations.FirstOrDefault(n => n.LanguageCode == langCode)
                            ?? poi.Narrations.FirstOrDefault();

            if (narration == null)
            {
                System.Diagnostics.Debug.WriteLine($"[PlayNarration] ❌ Không tìm thấy narration cho POI '{poi.Name}' lang='{langCode}'");
                return;
            }

            System.Diagnostics.Debug.WriteLine(
                $"[PlayNarration] ▶ POI='{poi.Name}' | UseAudioFile={narration.UseAudioFile} | AudioUrl='{narration.AudioUrl}' | TextLen={narration.Text?.Length ?? 0}");

            if (narration.UseAudioFile && !string.IsNullOrEmpty(narration.AudioUrl))
            {
                bool isHttpUrl = narration.AudioUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase);
                bool hasInternet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

                if (isHttpUrl && !hasInternet)
                {
                    System.Diagnostics.Debug.WriteLine("[PlayNarration] Offline + HTTP URL → fallback TTS");
                    await FallbackToTts(narration, poi.Name, langCode);
                    return;
                }

                if (!isHttpUrl && !System.IO.File.Exists(narration.AudioUrl))
                {
                    System.Diagnostics.Debug.WriteLine($"[PlayNarration] File local không tồn tại: {narration.AudioUrl} → fallback TTS");
                    await FallbackToTts(narration, poi.Name, langCode);
                    return;
                }

                bool isSuccess = await _audioService.PlayAudioFromUrlAsync(narration.AudioUrl, poi.Name);

                if (!isSuccess)
                {
                    System.Diagnostics.Debug.WriteLine("[PlayNarration] PlayAudio thất bại → fallback TTS");
                    await FallbackToTts(narration, poi.Name, langCode);
                }
            }
            else
            {
                await FallbackToTts(narration, poi.Name, langCode);
            }
        }

        private async Task FallbackToTts(Narration narration, string poiName, string langCode)
        {
            if (string.IsNullOrWhiteSpace(narration.Text))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[PlayNarration] ❌ narration.Text RỖNG cho POI '{poiName}' → KHÔNG THỂ PHÁT TTS");
                return;
            }

            System.Diagnostics.Debug.WriteLine(
                $"[PlayNarration] 🔊 TTS cho POI '{poiName}' | lang='{langCode}' | text length={narration.Text.Length}");
            await _audioService.SpeakAsync(narration.Text, poiName, langCode);
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

        public async Task RefreshGeofenceWithOfflineData(IEnumerable<POI> pois)
        {
            if (pois == null || !pois.Any()) return;

            _geofenceService.SetPois(pois.ToList());
            await _geofenceService.InitializeAsync();
            _geofenceService.StartMonitoring();
        }
    }
}