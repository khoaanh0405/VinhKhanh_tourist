using client.lib.core;
using client.lib.model;
using client.lib.services;

namespace client.lib.screens.poi;

public partial class POIDetailPage : ContentPage
{
    private POI? _poi;
    private readonly int _poiId;
    private readonly bool _autoPlayAudio;
    private bool _audioStarted = false;

    private readonly ApiService _apiService;
    private readonly AudioService _audioService;
    private readonly LocalDbService _localDbService;

    public POIDetailPage(int poiId, bool autoPlayAudio = false)
    {
        InitializeComponent();

        _poiId = poiId;
        _autoPlayAudio = autoPlayAudio;

        var services = Application.Current!.Handler!.MauiContext!.Services;
        _apiService = services.GetRequiredService<ApiService>();
        _audioService = services.GetRequiredService<AudioService>();
        _localDbService = services.GetRequiredService<LocalDbService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPoiAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_audioService.IsSpeaking)
            _audioService.Stop();
    }

    private async Task LoadPoiAsync()
    {
        string langCode = GetCurrentLanguageCode();
        SetLoadingState(true);

        try
        {
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                try
                {
                    _poi = await _apiService.FetchPOIByIdAsync(_poiId, langCode);
                }
                catch (Exception apiEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[POIDetail] Lỗi API: {apiEx.Message}");
                }
            }

            if (_poi == null)
            {
                var poiLocal = await _localDbService.GetPoiByIdAsync(_poiId);

                if (poiLocal != null)
                {
                    _poi = new POI
                    {
                        PoiId = poiLocal.PoiId,
                        Name = poiLocal.Name,
                        Latitude = poiLocal.Latitude,
                        Longitude = poiLocal.Longitude,
                        ImageUrls = string.IsNullOrEmpty(poiLocal.ImageUrlsJoined)
                                    ? new List<string>()
                                    : poiLocal.ImageUrlsJoined.Split(',').ToList()
                    };

                    var restaurantsLocal = await _localDbService.GetRestaurantsByPoiIdAsync(_poiId);
                    _poi.Restaurants = new List<Restaurant>();

                    foreach (var r in restaurantsLocal)
                    {
                        var newRest = new Restaurant { RestaurantId = r.RestaurantId, Name = r.Name, Address = r.Address };
                        var foodsLocal = await _localDbService.GetFoodsByRestaurantIdAsync(r.RestaurantId);
                        newRest.Foods = foodsLocal.Select(f => new Food { FoodId = f.FoodId, Name = f.Name, Price = f.Price }).ToList();
                        _poi.Restaurants.Add(newRest);
                    }

                    var narrationsLocal = await _localDbService.GetNarrationsByPoiIdAsync(_poiId);
                    _poi.Narrations = narrationsLocal.Select(n => new Narration { NarrationId = n.NarrationId, LanguageCode = n.LanguageCode, Text = n.Text }).ToList();
                }
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[POIDetail] Load error: {ex.Message}"); }

        SetLoadingState(false);

        if (_poi is null)
        {
            await DisplayAlert("Lỗi", "Không thể tải thông tin địa điểm này.", "OK");
            await Navigation.PopAsync();
            return;
        }

        RenderPoiData(_poi);
        UpdateAudioButtonState();

        if (_autoPlayAudio && !_audioStarted)
        {
            _audioStarted = true;
            OnMainAudioButtonClicked(this, EventArgs.Empty);
        }
    }

    private void RenderPoiData(POI poi)
    {
        LblName.Text = poi.Name;

        // Ẩn nhãn mô tả vì database đã loại bỏ trường Description
        if (LblDescription != null)
        {
            LblDescription.IsVisible = false;
        }

        ImgCarousel.ItemsSource = poi.ImageUrls != null && poi.ImageUrls.Any() ? poi.ImageUrls : new List<string> { "placeholder_img.webp" };

        RenderFoodsList(poi);
    }

    private string GetCurrentLanguageCode()
    {
        return Preferences.Get("AppLanguage", "vi");
    }

    private void SetLoadingState(bool isLoading)
    {
        LoadingIndicator.IsRunning = isLoading;
        LoadingIndicator.IsVisible = isLoading;
        ContentScrollView.IsVisible = !isLoading;
    }

    private void RenderFoodsList(POI poi)
    {
        FoodsContainer.Children.Clear();

        if (poi.Restaurants == null || !poi.Restaurants.Any())
        {
            FoodsContainer.Children.Add(new Label { Text = "Chưa có thông tin thực đơn.", TextColor = Colors.Gray, FontAttributes = FontAttributes.Italic });
            return;
        }

        foreach (var restaurant in poi.Restaurants)
        {
            if (restaurant.Foods == null || !restaurant.Foods.Any()) continue;

            FoodsContainer.Children.Add(new Label
            {
                Text = restaurant.Name,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1ABC9C"),
                Margin = new Thickness(0, 10, 0, 5)
            });

            foreach (var food in restaurant.Foods)
            {
                FoodsContainer.Children.Add(CreateFoodCard(food));
            }
        }
    }

    private View CreateFoodCard(Food food)
    {
        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            RowDefinitions = { new RowDefinition(GridLength.Auto) },
            Padding = new Thickness(0, 5)
        };

        var nameLabel = new Label { Text = food.Name, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1A1A2E") };
        grid.Add(nameLabel, 0, 0);

        var priceLabel = new Label { Text = $"{food.Price:N0}đ", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#E67E22"), HorizontalOptions = LayoutOptions.End };
        grid.Add(priceLabel, 1, 0);

        var border = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            StrokeThickness = 1,
            Stroke = Color.FromArgb("#E0E0E0"),
            BackgroundColor = Colors.White,
            Padding = 12,
            Margin = new Thickness(0, 0, 0, 8),
            Content = grid
        };

        return border;
    }

    private async void OnMainAudioButtonClicked(object sender, EventArgs e)
    {
        if (_poi == null) return;

        if (_audioService.IsSpeaking)
        {
            _audioService.Stop();
            UpdateAudioButtonState();
        }
        else
        {
            await PlayNarrationAsync(_poi);
        }
    }

    private async Task PlayNarrationAsync(POI poi)
    {
        var loc = LocalizationResourceManager.Instance;
        if (poi.Narrations == null || !poi.Narrations.Any())
        {
            await DisplayAlert(loc["Alert_Notice"], loc["Alert_NoAudio"], loc["Btn_OK"]);
            return;
        }

        string langCode = GetCurrentLanguageCode();
        var narration = poi.Narrations.FirstOrDefault(n => n.LanguageCode == langCode)
                     ?? poi.Narrations.FirstOrDefault();

        if (narration != null)
        {
            LblAudioStatus.Text = loc["Audio_Playing"]; 
            LblPlayIcon.Text = "⏸";

            try
            {
                await _audioService.SpeakAsync(narration.Text, _poi.Name ?? "Audio", langCode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioError] {ex.Message}");
            }
            finally
            {
                UpdateAudioButtonState();
            }
        }
    }

    private void UpdateAudioButtonState()
    {
        var loc = LocalizationResourceManager.Instance;
        LblPlayIcon.Text = _audioService.IsSpeaking ? "⏸" : "▶";

        var readyText = LocalizationResourceManager.Instance["DetailAudioReady"]?.ToString();
        LblAudioStatus.Text = _audioService.IsSpeaking ? loc["Audio_Playing"] : loc["Audio_Ready"];
    }

    private async void OnBackTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }
}