using client.lib.core;
using client.lib.model;
using client.lib.services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace client.lib.screens.poi;

public partial class POIDetailPage : ContentPage
{
    private POI? _poi;
    private readonly int _poiId;
    private readonly bool _autoPlayAudio;
    private bool _audioStarted = false;

    private readonly ApiService _apiService;
    private readonly AudioService _audioService;

    public POIDetailPage(int poiId, bool autoPlayAudio = false)
    {
        InitializeComponent();

        _poiId = poiId;
        _autoPlayAudio = autoPlayAudio;

        var services = Application.Current!.Handler!.MauiContext!.Services;
        _apiService = services.GetRequiredService<ApiService>();
        _audioService = services.GetRequiredService<AudioService>();
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
        // ĐƯA BIẾN RA NGOÀI KHỐI TRY
        string langCode = GetCurrentLanguageCode();

        try
        {
            SetLoadingState(true); // 1. Bật vòng xoay
            _poi = await _apiService.FetchPOIByIdAsync(_poiId, langCode); // 2. Lấy data từ API
        }
        catch (Exception ex)
        {
            SetLoadingState(false); // Bị lỗi mạng cũng phải tắt vòng xoay
            System.Diagnostics.Debug.WriteLine($"[POIDetail] Load error: {ex.Message}");
            await DisplayAlert("Lỗi kết nối", "Không thể tải dữ liệu. Vui lòng thử lại.", "OK");
            return; // Dừng lại luôn
        }

        // 3. Lấy xong data (dù thành công hay thất bại ở trên) thì TẮT VÒNG XOAY NGAY
        SetLoadingState(false);

        if (_poi is null)
        {
            await DisplayAlert("Lỗi", "Không tìm thấy địa điểm này.", "OK");
            await Navigation.PopAsync();
            return;
        }

        // 4. Render giao diện và cập nhật nút
        RenderPoiData(_poi);
        UpdateAudioButtonState();

        // 5. Lúc này UI đã hiện đầy đủ, vòng xoay đã tắt, mới bắt đầu đọc
        if (_autoPlayAudio && !_audioStarted)
        {
            _audioStarted = true;
            // Lúc này chương trình đã hiểu langCode là gì rồi
            await PlayNarrationAsync(_poi, langCode);
        }
    }

    private async Task PlayNarrationAsync(POI poi, string langCode)
    {
        if (poi.Narrations is null || !poi.Narrations.Any())
        {
            await _audioService.SpeakAsync(text: poi.Name, title: poi.Name, languageCode: langCode);
            return;
        }

        var narration = poi.Narrations.FirstOrDefault(n => string.Equals(n.LanguageCode, langCode, StringComparison.OrdinalIgnoreCase)) ?? poi.Narrations.First();

        if (narration.UseAudioFile && !string.IsNullOrWhiteSpace(narration.AudioUrl))
        {
            await _audioService.PlayAudioFromUrlAsync(narration.AudioUrl, poi.Name);
        }
        else if (!string.IsNullOrWhiteSpace(narration.Text))
        {
            await _audioService.SpeakAsync(text: narration.Text, title: poi.Name, languageCode: langCode);
        }
        else
        {
            await _audioService.SpeakAsync(text: poi.Name, title: poi.Name, languageCode: langCode);
        }
    }

    private static string GetCurrentLanguageCode() => Preferences.Get("AppLanguage", "vi");

    private void SetLoadingState(bool isLoading)
    {
        LoadingIndicator.IsVisible = isLoading;
        LoadingIndicator.IsRunning = isLoading;
        ContentScrollView.IsVisible = !isLoading;
    }

    private void RenderPoiData(POI poi)
    {
        LblName.Text = poi.Name;

        LblDescription.Text = !string.IsNullOrWhiteSpace(poi.Description)
            ? poi.Description
            : LocalizationResourceManager.Instance["DetailDefaultDesc"];

        // Lấy ảnh đầu tiên, nếu không có thì dùng ảnh mặc định
        HeroImage.Source = poi.ImageUrls?.FirstOrDefault() ?? "placeholder_img.webp";

        // Gán danh sách quán ăn vào CollectionView
        RestaurantsList.ItemsSource = poi.Restaurants;
    }

    private void UpdateAudioButtonState()
    {
        if (_audioService.IsSpeaking)
        {
            BtnPlayAudio.Text = "⏹ Dừng";
            BtnTogglePause.IsVisible = true;
            BtnTogglePause.Text = _audioService.IsPaused ? "▶ Tiếp tục" : "⏸ Tạm dừng";
        }
        else
        {
            BtnPlayAudio.Text = "▶ Phát thuyết minh";
            BtnTogglePause.IsVisible = false;
        }
    }

    private async void OnPlayAudioClicked(object sender, EventArgs e)
    {
        if (_poi is null) return;

        if (_audioService.IsSpeaking)
        {
            _audioService.Stop();
            UpdateAudioButtonState();
            return;
        }

        string langCode = GetCurrentLanguageCode();
        await PlayNarrationAsync(_poi, langCode);
        UpdateAudioButtonState();
    }

    private void OnTogglePauseClicked(object sender, EventArgs e)
    {
        _audioService.TogglePause();
        UpdateAudioButtonState();
    }

    private async void OnBackTapped(object sender, TappedEventArgs e)
    {
        _audioService.Stop();
        await Navigation.PopAsync();
    }
}