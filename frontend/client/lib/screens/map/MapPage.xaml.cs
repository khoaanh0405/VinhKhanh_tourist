using client.lib.core;
using client.lib.model;
using client.lib.screens.poi;
using client.lib.services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace client.lib.screens.map;

public partial class MapPage : ContentPage
{
    private Location _currentUserLocation;
    private List<RestaurantPoi> _allRestaurants = new();
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
                _currentUserLocation = await Geolocation.GetLastKnownLocationAsync();

                if (_currentUserLocation == null)
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
                    _currentUserLocation = await Geolocation.GetLocationAsync(request);
                }

                if (_currentUserLocation != null)
                {
                    MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(_currentUserLocation, Distance.FromKilometers(0.5)));
                }

                LoadVinhKhanhRestaurants();
                StartRealTimeLocationTracking();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MapPage] Lỗi khởi tạo map/GPS: {ex.Message}");
        }
    }

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
        catch (Exception ex) { Console.WriteLine($"Lỗi tracking GPS: {ex.Message}"); }
    }

    private void OnLocationChanged(object sender, GeolocationLocationChangedEventArgs e)
    {
        _currentUserLocation = e.Location;
        UpdateDistances();
    }

    private void UpdateDistances()
    {
        if (_currentUserLocation == null || _allRestaurants == null) return;

        foreach (var res in _allRestaurants)
        {
            var resLoc = new Location(res.Lat, res.Lng);
            double distKm = Location.CalculateDistance(_currentUserLocation, resLoc, DistanceUnits.Kilometers);
            res.DistanceKm = distKm;
            res.DistanceStr = distKm < 1 ? $"{Math.Round(distKm * 1000)} m" : $"{Math.Round(distKm, 1)} km";
        }
    }

    private async void OnGoToMyLocationClicked(object sender, EventArgs e)
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
            _currentUserLocation = await Geolocation.GetLocationAsync(request) ?? await Geolocation.GetLastKnownLocationAsync();

            if (_currentUserLocation != null)
            {
                MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(_currentUserLocation, Distance.FromKilometers(0.5)));
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MapPage] Lỗi khi bấm nút định vị: {ex.Message}"); }
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
                Lat = p.Latitude,
                Lng = p.Longitude,
                ImageUrl = p.ImageUrls?.FirstOrDefault() ?? "placeholder_img.webp"
            }).ToList();
        }

        UpdateDistances();
        _allRestaurants = _allRestaurants.OrderBy(r => r.DistanceKm).ToList();
        RestaurantList.ItemsSource = _allRestaurants;

        MyMap.Pins.Clear();
        foreach (var res in _allRestaurants)
        {
            var pin = new Pin
            {
                Label = res.Name,
                Address = LocalizationResourceManager.Instance["Map_PinDetailHint"],
                Type = PinType.Place,
                Location = new Location(res.Lat, res.Lng)
            };

            pin.InfoWindowClicked += async (s, args) =>
            {
                await Navigation.PushAsync(new POIDetailPage(res.PoiId, autoPlayAudio: false));
            };

            MyMap.Pins.Add(pin);
        }
    }

    private async void OnDetailButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is RestaurantPoi selectedRes)
        {
            await Navigation.PushAsync(new POIDetailPage(selectedRes.PoiId, autoPlayAudio: false));
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }

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
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string DistanceStr { get; set; }
    public double DistanceKm { get; set; } = double.MaxValue;
    public string ImageUrl { get; set; }
}