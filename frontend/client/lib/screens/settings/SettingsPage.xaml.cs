using client.lib.core;
using client.lib.model;
using client.lib.screens.home;
using client.lib.screens.qr;
using client.lib.services;
using client.Resources.String;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Globalization;
using static client.lib.services.ApiService;

namespace client.lib.screens.settings;

public partial class SettingsPage : ContentPage
{
    private readonly ApiService _apiService = new ApiService();
    private readonly LocalDbService _localDb = new LocalDbService();
    private List<AppLanguage> _availableLanguages = new();

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 1. Load danh sách ngôn ngữ từ API
        _availableLanguages = await _apiService.GetAvailableLanguagesAsync();

        LanguagePicker.SelectedIndexChanged -= OnLanguageSelected;
        LanguagePicker.ItemsSource = _availableLanguages;

        // 2. Tìm ngôn ngữ đã lưu để chọn mặc định
        string savedCode = Preferences.Get("AppLanguage", "vi");
        var selectedLang = _availableLanguages.FirstOrDefault(l => l.LanguageCode == savedCode);

        if (selectedLang != null) LanguagePicker.SelectedItem = selectedLang;
        else if (_availableLanguages.Any()) LanguagePicker.SelectedIndex = 0;

        LanguagePicker.SelectedIndexChanged += OnLanguageSelected;
    }

    private async void OnLanguageSelected(object sender, EventArgs e)
    {
        if (LanguagePicker.SelectedItem is not AppLanguage selectedLang) return;

        string newCode = selectedLang.LanguageCode;
        if (Preferences.Get("AppLanguage", "vi") == newCode) return;

        try
        {
            // 1. Lưu tùy chọn ngôn ngữ vào máy
            Preferences.Set("AppLanguage", newCode);

            // 2. Lấy bộ từ điển mới từ API
            var translations = await _apiService.GetUITranslationsAsync(newCode);

            // 3. Nạp vào Resource Manager -> UI trên toàn ứng dụng sẽ tự động đổi chữ!
            LocalizationResourceManager.Instance.SetTranslations(translations);

            // Hiển thị thông báo thành công
            var toast = Toast.Make($"Đã đổi ngôn ngữ: {selectedLang.LanguageName}", ToastDuration.Short, 14);
            await toast.Show();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể đổi ngôn ngữ.\nChi tiết: {ex.Message}", "OK");
        }
    }

    private async void OnQrScannerTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new QrScannerPage());
    }

    private async void OnAboutTapped(object sender, TappedEventArgs e)
    {
        string title = LocalizationResourceManager.Instance["SettingsAboutTitle"];
        string tagline = LocalizationResourceManager.Instance["SettingsFooterTagline"];
        string version = LocalizationResourceManager.Instance["SettingsFooterVersion"];

        await DisplayAlert(title, $"{tagline}\n\n{version}", "OK");
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnSyncOfflineDataTapped(object sender, TappedEventArgs e)
    {
        bool confirm = await DisplayAlert("Đồng bộ Offline", "Quá trình này sẽ tải dữ liệu mới nhất về máy điện thoại của bạn. Bạn có muốn tiếp tục?", "Tải ngay", "Hủy");
        if (!confirm) return;

        try
        {
            await DisplayAlert("Đang tải", "Quá trình đồng bộ đang diễn ra, vui lòng không tắt ứng dụng...", "OK");

            string lang = Preferences.Get("AppLanguage", "vi");

            var onlinePois = await _apiService.FetchPOIsAsync(lang);

            if (onlinePois != null && onlinePois.Any())
            {
                await _localDb.ClearAllDataAsync();

                foreach (var apiPoi in onlinePois)
                {
                    var detailPoi = await _apiService.FetchPOIByIdAsync(apiPoi.PoiId, lang);
                    if (detailPoi == null) continue;

                    var localPoi = new PoiLocal
                    {
                        PoiId = detailPoi.PoiId,
                        Name = detailPoi.Name,
                        Latitude = detailPoi.Latitude,
                        Longitude = detailPoi.Longitude,
                        ImageUrlsJoined = detailPoi.ImageUrls != null ? string.Join(",", detailPoi.ImageUrls) : ""
                    };

                    var localRestaurants = new List<RestaurantLocal>();
                    var localFoods = new List<FoodLocal>();

                    if (detailPoi.Restaurants != null)
                    {
                        foreach (var r in detailPoi.Restaurants)
                        {
                            localRestaurants.Add(new RestaurantLocal { RestaurantId = r.RestaurantId, PoiId = localPoi.PoiId, Name = r.Name, Address = r.Address });
                            if (r.Foods != null)
                            {
                                foreach (var f in r.Foods)
                                    localFoods.Add(new FoodLocal { FoodId = f.FoodId, RestaurantId = r.RestaurantId, Name = f.Name, Price = (double)f.Price });
                            }
                        }
                    }

                    var localNarrations = new List<NarrationLocal>();

                    await _localDb.SavePoiDataAsync(localPoi, localRestaurants, localFoods, localNarrations);
                }

                await DisplayAlert("Thành công", "Đã tải xong toàn bộ dữ liệu Offline! Bây giờ bạn có thể xem danh sách khi không có mạng.", "Tuyệt vời");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể đồng bộ: {ex.Message}", "OK");
        }
    }
}