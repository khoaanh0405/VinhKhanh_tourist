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

        // 1. PROPERTY RÀNG BUỘC CHO THANH TÌM KIẾM
        [ObservableProperty]
        private string _searchText = string.Empty;

        public POI? CurrentActivePOI => _geofenceService.CurrentActivePOI;
        public bool IsPlaying => CurrentActivePOI != null;
        public AudioService Audio => _audioService;

        public string? CurrentActivePOIImageUrl => _geofenceService.CurrentActivePOI?.ImageUrl;

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

            _geofenceService.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(GeofenceService.CurrentActivePOI))
                {
                    OnPropertyChanged(nameof(CurrentActivePOI));
                    OnPropertyChanged(nameof(IsPlaying));
                    OnPropertyChanged(nameof(CurrentActivePOIImageUrl));
                    if (CurrentActivePOI != null)
                    {
                        await PlayNarrationAsync(CurrentActivePOI);
                    }
                }
            };
        }

        private CancellationTokenSource? _searchCts;

        // 2. HOOK TỰ ĐỘNG ĐƯỢC GỌI KHI SEARCHTEXT THAY ĐỔI
        partial void OnSearchTextChanged(string value)
        {
            ExecuteDebouncedSearch(value);
        }

        // 3. HÀM TÌM KIẾM VỚI DEBOUNCE AN TOÀN VÀ KHÔNG REPLACE COLLECTION
        private async void ExecuteDebouncedSearch(string query)
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                // Debounce 800ms để người dùng gõ xong chữ tiếng Việt
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
                        ContainsVietnamese(p.Name, query) ||
                        (p.Restaurants != null && p.Restaurants.Any(r =>
                            r.Foods != null && r.Foods.Any(f => ContainsVietnamese(f.Name, query))
                        ))
                    ).ToList();
                });

                // Cập nhật Collection thay vì tạo mới liên tục
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    FilteredPois.Clear();
                    foreach (var item in result)
                    {
                        FilteredPois.Add(item);
                    }
                    IsSearching = true;
                });
            }
            catch (TaskCanceledException) { }
        }

        private bool ContainsVietnamese(string? source, string query)
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
                var fetchedPois = await _apiService.FetchPOIsAsync();
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

                                if (distMeters < 1000)
                                    poi.DistanceDisplay = $"📍 Cách bạn {Math.Round(distMeters)}m";
                                else
                                    poi.DistanceDisplay = $"📍 Cách bạn {distKm:F1}km";
                            }
                        }
                    }
                    catch (Exception) { /* Bỏ qua nếu tắt GPS */ }

                    Pois = new ObservableCollection<POI>(fetchedPois.OrderBy(p => p.DistanceInMeters));

                    _geofenceService.SetPois(fetchedPois);

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
            System.Diagnostics.Debug.WriteLine($"[AUDIO TEST] Bắt đầu kiểm tra Audio cho quán: {poi.Name}");

            // 1. Kiểm tra xem API có trả về Narrations không
            if (poi.Narrations == null || !poi.Narrations.Any())
            {
                System.Diagnostics.Debug.WriteLine("[AUDIO TEST] LỖI: Dữ liệu Narrations bị rỗng (Kiểm tra lại Backend API có Include Narrations chưa)!");
                return;
            }

            string langCode = _appViewModel.CurrentLanguageCode;
            var narration = poi.Narrations.FirstOrDefault(n => n.LanguageCode == langCode)
                            ?? poi.Narrations.FirstOrDefault();

            if (narration != null)
            {
                System.Diagnostics.Debug.WriteLine($"[AUDIO TEST] UseAudioFile = {narration.UseAudioFile}");

                if (narration.UseAudioFile && !string.IsNullOrEmpty(narration.AudioUrl))
                {
                    System.Diagnostics.Debug.WriteLine("[AUDIO TEST] Đang phát Audio từ URL...");
                    await _audioService.PlayAudioFromUrlAsync(narration.AudioUrl, poi.Name);
                }
                else
                {
                    // 2. Nếu log chạy đến đây mà vẫn không có tiếng -> Lỗi do bộ TTS của máy ảo
                    System.Diagnostics.Debug.WriteLine("[AUDIO TEST] Đang dùng Text-to-Speech để đọc chữ...");
                    await _audioService.SpeakAsync(narration.Text, poi.Name);
                }
            }
        }
    }
}