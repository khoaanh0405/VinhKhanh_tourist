using client.lib.core;              // LocalizationResourceManager
using client.lib.model;
using client.lib.screens.home;     // HomeViewModel
using client.lib.screens.qr;        // QrScannerPage
using client.lib.services;          // AppViewModel
using client.Resources.String;      // AppResources
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Globalization;

namespace client.lib.screens.settings;

public partial class SettingsPage : ContentPage
{
    private static readonly string[] LangCodes = { "vi", "en", "ko" };
    private static readonly string[] LangNames = { "Tiếng Việt 🇻🇳", "English 🇬🇧", "한국어 🇰🇷" };
    private readonly ApiService _apiService = new ApiService();
    private readonly LocalDbService _localDb = new LocalDbService();

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        string saved = Preferences.Get("AppLanguage", "vi");
        int idx = Array.IndexOf(LangCodes, saved);
        LanguagePicker.SelectedIndexChanged -= OnLanguageSelected;
        LanguagePicker.SelectedIndex = idx >= 0 ? idx : 0;
        LanguagePicker.SelectedIndexChanged += OnLanguageSelected;
    }

    private async void OnLanguageSelected(object sender, EventArgs e)
    {
        int pickerIdx = LanguagePicker.SelectedIndex;
        if (pickerIdx < 0 || pickerIdx >= LangCodes.Length) return;

        string newCode = LangCodes[pickerIdx];
        string newName = LangNames[pickerIdx];

        if (Preferences.Get("AppLanguage", "vi") == newCode) return;

        try
        {
            var services = Application.Current.Handler.MauiContext.Services;

            // 🔥 GỌI AUDIOSERVICE ĐỂ DỪNG ĐỌC TRƯỚC KHI ĐỔI NGÔN NGỮ
            var audioService = services.GetService<AudioService>();
            audioService?.Stop();

            Preferences.Set("AppLanguage", newCode);
            var culture = new CultureInfo(newCode);

            // --- Cập nhật luồng UI hiện tại ---
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            AppResources.Culture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            LocalizationResourceManager.Instance.SetCulture(culture);

            // Thêm một chút delay để UI kịp dọn dẹp thanh trạng thái và cập nhật
            await Task.Delay(50);

            // Cập nhật các ViewModel
            var appViewModel = services.GetService<AppViewModel>();
            var homeViewModel = services.GetService<HomeViewModel>();

            appViewModel?.RefreshTranslations();

            if (homeViewModel != null)
            {
                var matchedLang = homeViewModel.AvailableLanguages
                    .FirstOrDefault(l => l.LanguageCode == newCode);

                if (matchedLang != null)
                {
                    homeViewModel.SelectedLanguage = matchedLang;
                }
            }

            // Hiện thông báo
            string toastMessage = LocalizationResourceManager.Instance["SettingsLanguageChangedMsg"];
            var toast = Toast.Make($"{toastMessage} {newName}", ToastDuration.Short, 14);
            await toast.Show();
        }
        catch (Exception ex)
        {
            string fallback = Preferences.Get("AppLanguage", "vi");
            int fallbackIdx = Array.IndexOf(LangCodes, fallback);

            LanguagePicker.SelectedIndexChanged -= OnLanguageSelected;
            LanguagePicker.SelectedIndex = fallbackIdx >= 0 ? fallbackIdx : 0;
            LanguagePicker.SelectedIndexChanged += OnLanguageSelected;

            await DisplayAlert("Lỗi", $"Không thể đổi ngôn ngữ.\nChi tiết: {ex.Message}", "OK");
        }
    }

    private async void OnQrScannerTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new QrScannerPage());
    }

    private async void OnAboutTapped(object sender, TappedEventArgs e)
    {
        // Lấy chữ DisplayAlert bằng ngôn ngữ hiện tại
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

            // 1. Kéo toàn bộ POI từ API
            var onlinePois = await _apiService.FetchPOIsAsync(lang);

            if (onlinePois != null && onlinePois.Any())
            {
                // 2. Xóa sạch kho mini trong điện thoại để tránh rác
                await _localDb.ClearAllDataAsync();

                // 3. Lặp qua từng quán, tải chi tiết và cất vào SQLite
                foreach (var apiPoi in onlinePois)
                {
                    // Lấy chi tiết quán bằng ngôn ngữ hiện tại (để lấy Name, Description chuẩn hiển thị)
                    var detailPoi = await _apiService.FetchPOIByIdAsync(apiPoi.PoiId, lang);
                    if (detailPoi == null) continue;

                    var localPoi = new PoiLocal
                    {
                        PoiId = detailPoi.PoiId,
                        Name = detailPoi.Name,
                        Description = detailPoi.Description,
                        Latitude = detailPoi.Latitude,
                        Longitude = detailPoi.Longitude,
                        AverageRating = detailPoi.AverageRating,
                        ReviewCount = detailPoi.ReviewCount,
                        ImageUrlsJoined = detailPoi.ImageUrls != null ? string.Join(",", detailPoi.ImageUrls) : ""
                    };

                    var localRestaurants = new List<RestaurantLocal>();
                    var localFoods = new List<FoodLocal>();

                    if (detailPoi.Restaurants != null)
                    {
                        foreach (var r in detailPoi.Restaurants)
                        {
                            localRestaurants.Add(new RestaurantLocal { RestaurantId = r.RestaurantId, PoiId = localPoi.PoiId, Name = r.Name, Description = r.Description, Address = r.Address });
                            if (r.Foods != null)
                            {
                                foreach (var f in r.Foods)
                                    localFoods.Add(new FoodLocal { FoodId = f.FoodId, RestaurantId = r.RestaurantId, Name = f.Name, Price = (double)f.Price, Description = f.Description });
                            }
                        }
                    }

                    // 🔥 4. THÊM MAPPING NARRATIONS (CHO TẤT CẢ NGÔN NGỮ)
                    var localNarrations = new List<NarrationLocal>();

                    // Lặp qua mảng các ngôn ngữ mà app hỗ trợ
                    foreach (var lCode in LangCodes)
                    {
                        // Gọi API để lấy narration của từng ngôn ngữ
                        var detailPoiForLang = await _apiService.FetchPOIByIdAsync(apiPoi.PoiId, lCode);

                        if (detailPoiForLang?.Narrations != null)
                        {
                            foreach (var n in detailPoiForLang.Narrations)
                            {
                                // Tránh lưu trùng lặp nếu API lỡ trả về dữ liệu giống nhau
                                if (!localNarrations.Any(ln => ln.LanguageCode == n.LanguageCode))
                                {
                                    localNarrations.Add(new NarrationLocal
                                    {
                                        PoiId = localPoi.PoiId,
                                        NarrationId = n.NarrationId,
                                        LanguageCode = n.LanguageCode,
                                        Text = n.Text,
                                        UseAudioFile = n.UseAudioFile,
                                        AudioUrl = n.AudioUrl
                                    });
                                }
                            }
                        }
                    }

                    // 🔥 5. TRUYỀN ĐÚNG 4 BIẾN VÀO HÀM LƯU DB (Lúc này localNarrations đã chứa đủ vi, en, ko)
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