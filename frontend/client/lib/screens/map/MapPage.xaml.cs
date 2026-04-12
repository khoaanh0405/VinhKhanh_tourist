using client.lib.core; // FuzzySearchHelper
using client.lib.model;
using client.lib.screens.home; // DetailScreen
using client.lib.screens.poi;
using client.lib.services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Text.Json;

namespace client.lib.screens.map;

public partial class MapPage : ContentPage
{
    private Location _currentUserLocation;
    private List<RestaurantPoi> _allRestaurants = new();

    // Các biến phục vụ dẫn đường thời gian thực
    private Location _targetLocation;
    private bool _isNavigating = false;
    private bool _isTrackingStarted = false;

    public MapPage()
    {
        InitializeComponent();
        _ = InitializeMapAsync();
    }

    private async Task InitializeMapAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status == PermissionStatus.Granted)
            {
                // 🔥 FIX #1: Luôn ưu tiên lấy vị trí đã lưu gần nhất (Cực nhanh, không cần mạng)
                _currentUserLocation = await Geolocation.GetLastKnownLocationAsync();

                // 🔥 FIX #2: Nếu chưa có vị trí cũ, yêu cầu GPS tìm kiếm nhưng ÉP TIMEOUT 5 GIÂY
                if (_currentUserLocation == null)
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
                    _currentUserLocation = await Geolocation.GetLocationAsync(request);
                }

                if (_currentUserLocation != null)
                {
                    MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(_currentUserLocation, Distance.FromKilometers(0.5)));
                    LoadVinhKhanhRestaurants();
                }

                StartRealTimeLocationTracking();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MapPage] Lỗi khởi tạo map/GPS: {ex.Message}");
            // Bỏ qua lỗi nếu timeout hoặc máy không bật GPS để app vẫn tiếp tục chạy mượt
        }
    }

    // --- LOGIC THEO DÕI VỊ TRÍ THỜI GIAN THỰC ---
    private async void StartRealTimeLocationTracking()
    {
        if (_isTrackingStarted) return;

        try
        {
            Geolocation.LocationChanged += OnLocationChanged;
            var request = new GeolocationListeningRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(5));
            await Geolocation.StartListeningForegroundAsync(request);
            _isTrackingStarted = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi tracking GPS: {ex.Message}");
        }
    }

    private async void OnLocationChanged(object sender, GeolocationLocationChangedEventArgs e)
    {
        _currentUserLocation = e.Location;
        UpdateDistances();

        if (_isNavigating && _targetLocation != null)
        {
            await DrawRouteAsync(_currentUserLocation, _targetLocation, moveCamera: false);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // 🔥 FIX: Tính khoảng cách bằng DOUBLE thay vì string
    // ══════════════════════════════════════════════════════════════
    private void UpdateDistances()
    {
        if (_currentUserLocation == null || _allRestaurants == null) return;

        foreach (var res in _allRestaurants)
        {
            var resLoc = new Location(res.Lat, res.Lng);
            double distKm = Location.CalculateDistance(_currentUserLocation, resLoc, DistanceUnits.Kilometers);

            // Lưu giá trị double để sort chính xác
            res.DistanceKm = distKm;

            // Tạo string hiển thị
            res.DistanceStr = distKm < 1
                ? $"{Math.Round(distKm * 1000)} m"
                : $"{Math.Round(distKm, 1)} km";
        }
    }

    private async void OnGoToMyLocationClicked(object sender, EventArgs e)
    {
        try
        {
            // Tương tự, dùng Timeout cho nút bấm định vị
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
            _currentUserLocation = await Geolocation.GetLocationAsync(request) ?? await Geolocation.GetLastKnownLocationAsync();

            if (_currentUserLocation != null)
            {
                MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(_currentUserLocation, Distance.FromKilometers(0.5)));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MapPage] Lỗi khi bấm nút định vị: {ex.Message}");
        }
    }

    private void LoadVinhKhanhRestaurants()
    {
        var geofenceService = Application.Current.Handler.MauiContext.Services.GetService<GeofenceService>();

        if (geofenceService != null && geofenceService.Pois != null && geofenceService.Pois.Any())
        {
            _allRestaurants = geofenceService.Pois.Select(p => new RestaurantPoi
            {
                PoiId = p.PoiId,
                Name = p.Name,
                Description = p.Description,
                Lat = p.Latitude,
                Lng = p.Longitude,
                AverageRating = p.AverageRating,
                ImageUrl = p.ImageUrls?.FirstOrDefault() ?? "placeholder_img.webp"
            }).ToList();
        }
        else
        {
            _allRestaurants = new List<RestaurantPoi>();
        }

        UpdateDistances();

        // 🔥 FIX: Sort theo DistanceKm (double) thay vì DistanceStr (string)
        _allRestaurants = _allRestaurants.OrderBy(r => r.DistanceKm).ToList();
        RestaurantList.ItemsSource = _allRestaurants;
    }

    // ══════════════════════════════════════════════════════════════
    // 🔥 NÂNG CẤP: Fuzzy search cho MapPage
    // ══════════════════════════════════════════════════════════════
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = e.NewTextValue?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(keyword))
        {
            SearchResultsContainer.IsVisible = false;
            // Sort lại theo khoảng cách khi xóa search
            RestaurantList.ItemsSource = _allRestaurants.OrderBy(r => r.DistanceKm).ToList();

            _isNavigating = false;
            _targetLocation = null;
            MyMap.MapElements.Clear();
        }
        else
        {
            string normalizedQuery = FuzzySearchHelper.NormalizeText(keyword);

            var filtered = _allRestaurants
                .Select(r => new
                {
                    Restaurant = r,
                    Score = CalculateMapSearchScore(r, normalizedQuery)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Restaurant.DistanceKm)
                .Select(x => x.Restaurant)
                .ToList();

            SearchList.ItemsSource = filtered;
            RestaurantList.ItemsSource = filtered;
            SearchResultsContainer.IsVisible = filtered.Any();
        }
    }

    /// <summary>
    /// Tính điểm search riêng cho MapPage (chỉ có Name + Description)
    /// </summary>
    private static int CalculateMapSearchScore(RestaurantPoi r, string normalizedQuery)
    {
        int score = 0;

        if (FuzzySearchHelper.StartsWithNormalized(r.Name, normalizedQuery))
            score = Math.Max(score, 100);
        else if (FuzzySearchHelper.AnyWordStartsWith(r.Name, normalizedQuery))
            score = Math.Max(score, 90);
        else if (FuzzySearchHelper.ContainsNormalized(r.Name, normalizedQuery))
            score = Math.Max(score, 80);

        if (FuzzySearchHelper.ContainsNormalized(r.Description, normalizedQuery))
            score = Math.Max(score, 60);

        // Fuzzy fallback
        if (score == 0)
        {
            if (FuzzySearchHelper.FuzzyContains(r.Name, normalizedQuery))
                score = 25;
            else if (FuzzySearchHelper.FuzzyContains(r.Description, normalizedQuery))
                score = 15;
        }

        return score;
    }

    private void StartNavigationTo(RestaurantPoi selectedRes)
    {
        if (selectedRes != null && _currentUserLocation != null)
        {
            _targetLocation = new Location(selectedRes.Lat, selectedRes.Lng);
            _isNavigating = true;
            _ = DrawRouteAsync(_currentUserLocation, _targetLocation, moveCamera: true);
        }
    }

    private void OnSearchResultSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is RestaurantPoi selectedRes)
        {
            StartNavigationTo(selectedRes);
            SearchResultsContainer.IsVisible = false;
            SearchEntry.Unfocus();

            SearchEntry.TextChanged -= OnSearchTextChanged;
            SearchEntry.Text = selectedRes.Name;
            SearchEntry.TextChanged += OnSearchTextChanged;

            ((CollectionView)sender).SelectedItem = null;
            RestaurantList.ScrollTo(selectedRes, position: ScrollToPosition.Center);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // 🔥 MỚI: Xử lý nút "Dẫn đường" trên mỗi card
    // ══════════════════════════════════════════════════════════════
    private void OnNavigateButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is RestaurantPoi selectedRes)
        {
            StartNavigationTo(selectedRes);
            SearchResultsContainer.IsVisible = false;

            SearchEntry.TextChanged -= OnSearchTextChanged;
            SearchEntry.Text = selectedRes.Name;
            SearchEntry.TextChanged += OnSearchTextChanged;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // 🔥 MỚI: Xử lý nút "Xem chi tiết" trên mỗi card
    // ══════════════════════════════════════════════════════════════
    private async void OnDetailButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is RestaurantPoi selectedRes)
        {
            // Tìm POI đầy đủ từ GeofenceService để có đủ data cho DetailScreen
            var geofenceService = Application.Current.Handler.MauiContext.Services.GetService<GeofenceService>();
            var fullPoi = geofenceService?.Pois?.FirstOrDefault(p => p.PoiId == selectedRes.PoiId);

            if (fullPoi != null)
            {
                await Navigation.PushAsync(new POIDetailPage(selectedRes.PoiId, autoPlayAudio: false));
            }
            else
            {
                // Fallback: tạo POI cơ bản từ RestaurantPoi
                var basicPoi = new POI
                {
                    PoiId = selectedRes.PoiId,
                    Name = selectedRes.Name,
                    Description = selectedRes.Description,
                    Latitude = selectedRes.Lat,
                    Longitude = selectedRes.Lng,
                    AverageRating = selectedRes.AverageRating,
                    ImageUrls = new List<string> { selectedRes.ImageUrl }
                };
                await Navigation.PushAsync(new DetailScreen(basicPoi));
            }
        }
    }

    private void OnRestaurantTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is RestaurantPoi selectedRes)
        {
            StartNavigationTo(selectedRes);
            SearchResultsContainer.IsVisible = false;

            SearchEntry.TextChanged -= OnSearchTextChanged;
            SearchEntry.Text = selectedRes.Name;
            SearchEntry.TextChanged += OnSearchTextChanged;
        }
    }

    private async Task DrawRouteAsync(Location start, Location end, bool moveCamera = true)
    {
        try
        {
            string startCoords = $"{start.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{start.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            string endCoords = $"{end.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{end.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

            string url = $"http://router.project-osrm.org/route/v1/driving/{startCoords};{endCoords}?geometries=geojson";

            using var client = new HttpClient();
            var response = await client.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            var routes = doc.RootElement.GetProperty("routes");

            if (routes.GetArrayLength() > 0)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MyMap.MapElements.Clear();
                    var polyline = new Polyline { StrokeColor = Color.FromArgb("#4285F4"), StrokeWidth = 6 };
                    var coordinates = routes[0].GetProperty("geometry").GetProperty("coordinates");

                    foreach (var coord in coordinates.EnumerateArray())
                    {
                        polyline.Geopath.Add(new Location(coord[1].GetDouble(), coord[0].GetDouble()));
                    }
                    MyMap.MapElements.Add(polyline);

                    if (moveCamera)
                    {
                        var centerLat = (start.Latitude + end.Latitude) / 2;
                        var centerLng = (start.Longitude + end.Longitude) / 2;
                        double distance = Location.CalculateDistance(start, end, DistanceUnits.Kilometers);
                        MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(centerLat, centerLng), Distance.FromKilometers(distance + 0.2)));
                    }
                });
            }
        }
        catch (Exception) { /* Bỏ qua lỗi mạng */ }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        SearchEntry.Placeholder = client.Resources.String.AppResources.SearchMapPlaceholder;
        LoadVinhKhanhRestaurants();

        if (_isTrackingStarted)
        {
            Geolocation.StopListeningForeground();
            Geolocation.LocationChanged -= OnLocationChanged;
            _isTrackingStarted = false;
        }
    }
}

public class RestaurantPoi
{
    public int PoiId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string DistanceStr { get; set; }

    // 🔥 MỚI: Khoảng cách dạng double để sort chính xác
    public double DistanceKm { get; set; } = double.MaxValue;

    public double AverageRating { get; set; }
    public string ImageUrl { get; set; }
}