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
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        if (status == PermissionStatus.Granted)
        {
            // Lấy vị trí lần đầu tiên
            _currentUserLocation = await Geolocation.GetLocationAsync() ?? await Geolocation.GetLastKnownLocationAsync();

            if (_currentUserLocation != null)
            {
                MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(_currentUserLocation, Distance.FromKilometers(0.5)));
                LoadVinhKhanhRestaurants();
            }

            // Bắt đầu lắng nghe vị trí thay đổi liên tục
            StartRealTimeLocationTracking();
        }
    }

    // --- LOGIC THEO DÕI VỊ TRÍ THỜI GIAN THỰC ---
    private async void StartRealTimeLocationTracking()
    {
        if (_isTrackingStarted) return;

        try
        {
            Geolocation.LocationChanged += OnLocationChanged;
            // Lấy vị trí mỗi 5 giây với độ chính xác cao
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

        // Cập nhật lại khoảng cách trong danh sách
        UpdateDistances();

        // Nếu đang trong chế độ dẫn đường, vẽ lại đường đi từ vị trí mới
        if (_isNavigating && _targetLocation != null)
        {
            // moveCamera = false để không giật khung hình bản đồ liên tục khi người dùng đang xem
            await DrawRouteAsync(_currentUserLocation, _targetLocation, moveCamera: false);
        }
    }

    private void UpdateDistances()
    {
        if (_currentUserLocation == null || _allRestaurants == null) return;

        foreach (var res in _allRestaurants)
        {
            var resLoc = new Location(res.Lat, res.Lng);
            double dist = Location.CalculateDistance(_currentUserLocation, resLoc, DistanceUnits.Kilometers);
            res.DistanceStr = dist < 1 ? $"{Math.Round(dist * 1000)} m" : $"{Math.Round(dist, 1)} km";
        }

        // Cập nhật lại UI nếu cần thiết (sắp xếp lại)
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Tạm thời tắt để tránh giật list khi đang cuộn
            // var sorted = _allRestaurants.OrderBy(r => r.DistanceStr).ToList();
            // RestaurantList.ItemsSource = sorted;
        });
    }
    // ---------------------------------------------

    private async void OnGoToMyLocationClicked(object sender, EventArgs e)
    {
        _currentUserLocation = await Geolocation.GetLocationAsync() ?? await Geolocation.GetLastKnownLocationAsync();
        if (_currentUserLocation != null)
        {
            MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(_currentUserLocation, Distance.FromKilometers(0.5)));
        }
    }

    private void LoadVinhKhanhRestaurants()
    {
        _allRestaurants = new List<RestaurantPoi>
        {
            new RestaurantPoi { PoiId = 1, Name = "Ốc Oanh Vĩnh Khánh", Description = "Quán ốc nổi tiếng và đông khách nhất khu phố...", Lat = 10.761500, Lng = 106.704200, AverageRating = 4.8, ImageUrl = "https://res.cloudinary.com/dfxbdpxkc/image/upload/v1772255164/placeholder_img_ypdb0p.webp" },
            new RestaurantPoi { PoiId = 2, Name = "Ốc Vũ", Description = "Không gian thoáng mát, menu đa dạng...", Lat = 10.762000, Lng = 106.704500, AverageRating = 4.5, ImageUrl = "https://res.cloudinary.com/dfxbdpxkc/image/upload/v1772255164/haisanvk1_tank_oag0wf.jpg" },
            new RestaurantPoi { PoiId = 3, Name = "Lẩu Bò Khu Nhà Cháy", Description = "Lẩu bò truyền thống với nước dùng đậm đà...", Lat = 10.761000, Lng = 106.703800, AverageRating = 4.2, ImageUrl = "https://res.cloudinary.com/dfxbdpxkc/image/upload/v1772255164/monanmau_xaj5lo.jpg" },
            new RestaurantPoi { PoiId = 4, Name = "Sườn Nướng Đảo", Description = "Chuyên các món sườn nướng BBQ xốt cay ngọt...", Lat = 10.762500, Lng = 106.705000, AverageRating = 4.7, ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?q=80&w=1000" }
        };

        UpdateDistances();
        _allRestaurants = _allRestaurants.OrderBy(r => r.DistanceStr).ToList();
        RestaurantList.ItemsSource = _allRestaurants;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = e.NewTextValue?.ToLower() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(keyword))
        {
            SearchResultsContainer.IsVisible = false;
            RestaurantList.ItemsSource = _allRestaurants;

            _isNavigating = false;
            _targetLocation = null;
            MyMap.MapElements.Clear();
        }
        else
        {
            var filtered = _allRestaurants
                .Where(r => r.Name.ToLower().Contains(keyword) ||
                            r.Description.ToLower().Contains(keyword)).ToList();

            SearchList.ItemsSource = filtered;
            RestaurantList.ItemsSource = filtered;
            SearchResultsContainer.IsVisible = filtered.Any();
        }
    }

    private void StartNavigationTo(RestaurantPoi selectedRes)
    {
        if (selectedRes != null && _currentUserLocation != null)
        {
            // Bật chế độ dẫn đường và lưu tọa độ đích
            _targetLocation = new Location(selectedRes.Lat, selectedRes.Lng);
            _isNavigating = true;

            // Vẽ đường ngay lập tức (cho phép camera dịch chuyển ở lần chọn đầu tiên)
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

    private void OnRestaurantTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is RestaurantPoi selectedRes)
        {
            StartNavigationTo(selectedRes);
            SearchResultsContainer.IsVisible = false;

            // Cách an toàn tuyệt đối 100% để chặn sự kiện tìm kiếm: Tháo event ra trước khi gán text
            SearchEntry.TextChanged -= OnSearchTextChanged;
            SearchEntry.Text = selectedRes.Name;
            SearchEntry.TextChanged += OnSearchTextChanged;
        }
    }

    // Thêm tham số moveCamera để tránh giật bản đồ khi cập nhật ngầm
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
                // Sử dụng MainThread để update UI an toàn khi chạy ngầm
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
        catch (Exception) { /* Bỏ qua lỗi mạng dể app không bị crash */ }
    }

    // Dừng lắng nghe vị trí khi tắt trang để tiết kiệm pin
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
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
    public double AverageRating { get; set; }
    public string ImageUrl { get; set; }
}