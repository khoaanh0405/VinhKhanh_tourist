using client.lib.services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Net.Http.Json;

namespace client.lib.screens.Playlist;

public partial class PlaylistPage : ContentPage
{
    private readonly AudioService _audioService;
    private readonly HttpClient _http;
    private readonly int _playlistId;

    private PlaylistItemViewModel? _currentPlayingItem;

    // Từ điển chứa các từ khóa dịch thuật
    private Dictionary<string, string> _translations = new();

    public PlaylistPage(int playlistId)
    {
        InitializeComponent();
        _playlistId = playlistId;

        _audioService = Application.Current!
            .Handler!.MauiContext!.Services.GetRequiredService<AudioService>();

        _http = Application.Current!
            .Handler!.MauiContext!.Services.GetRequiredService<HttpClient>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Tải ngôn ngữ trước, sau đó mới tải danh sách
        await LoadTranslationsAsync();
        await LoadPlaylistAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_audioService.IsSpeaking) _audioService.Stop();
        if (_currentPlayingItem != null) _currentPlayingItem.IsPlaying = false;
    }

    // ── 1. TẢI NGÔN NGỮ ĐỘNG ──────────────────────────────────────────
    private async Task LoadTranslationsAsync()
    {
        try
        {
            string currentLang = Preferences.Get("AppLanguage", "vi");
            // Gọi API lấy toàn bộ UI text theo ngôn ngữ (Cần đảm bảo Backend bạn có API này)
            // Ví dụ API trả về dạng { "Playlist_Title": "Danh Sách Phát", ... }
            var result = await _http.GetFromJsonAsync<Dictionary<string, string>>($"api/UITranslations/{currentLang}");
            if (result != null)
            {
                _translations = result;

                // Cập nhật text lên giao diện
                lblPlaylistTitle.Text = GetText("Playlist_Title", "Danh Sách Phát");
                lblLoadingMsg.Text = GetText("Playlist_Loading", "Đang tải danh sách...");
            }
        }
        catch
        {
            // Nếu lỗi API, vẫn giữ text mặc định đã gán trong file XAML
            System.Diagnostics.Debug.WriteLine("[PlaylistPage] Không thể tải ngôn ngữ.");
        }
    }

    private string GetText(string key, string fallback)
    {
        return _translations.TryGetValue(key, out var val) ? val : fallback;
    }

    // ── 2. TẢI DANH SÁCH PLAYLIST ─────────────────────────────────────
    private async Task LoadPlaylistAsync()
    {
        LoadingView.IsVisible = true;
        PlaylistCollection.IsVisible = false;
        EmptyView.IsVisible = false;

        try
        {
            var items = await _http.GetFromJsonAsync<List<PlaylistItemDto>>($"api/Playlist/{_playlistId}/items");

            if (items == null || items.Count == 0)
            {
                lblEmptyMsg.Text = GetText("Playlist_Empty", "Playlist này chưa có địa điểm nào.");
                EmptyView.IsVisible = true;
                return;
            }

            // Thay thế "{0}" bằng số lượng thực tế
            string countTemplate = GetText("Playlist_ItemCount", "{0} địa điểm");
            lblItemCount.Text = string.Format(countTemplate, items.Count);

            string playingStatusText = GetText("Playlist_PlayingStatus", "🔊 Đang phát thuyết minh...");

            var viewModels = items
                .Select(i => new PlaylistItemViewModel(i, playingStatusText))
                .ToList();

            PlaylistCollection.ItemsSource = viewModels;
            PlaylistCollection.IsVisible = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PlaylistPage] LoadError: {ex.Message}");
            lblEmptyMsg.Text = GetText("Playlist_Error", "Không thể tải danh sách. Kiểm tra kết nối mạng.");
            EmptyView.IsVisible = true;
        }
        finally
        {
            LoadingView.IsVisible = false;
        }
    }

    // ── Xử lý nút Play ───────────────────────────────────────────────
    private async void OnPlayTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not PlaylistItemViewModel tappedItem) return;

        if (_currentPlayingItem == tappedItem && _audioService.IsSpeaking)
        {
            _audioService.Stop();
            tappedItem.IsPlaying = false;
            _currentPlayingItem = null;
            return;
        }

        if (_audioService.IsSpeaking)
        {
            _audioService.Stop();
            if (_currentPlayingItem != null)
            {
                _currentPlayingItem.IsPlaying = false;
                _currentPlayingItem = null;
            }
        }

        var narration = await FetchNarrationAsync(tappedItem.PoiId);

        if (narration == null || string.IsNullOrWhiteSpace(narration.Text))
        {
            await DisplayAlert(GetText("Alert_Notice", "Thông báo"),
                               GetText("Alert_NoAudio", $"Chưa có thuyết minh cho '{tappedItem.PoiName}'."),
                               GetText("Btn_OK", "OK"));
            return;
        }

        tappedItem.IsPlaying = true;
        _currentPlayingItem = tappedItem;

        string currentLang = Preferences.Get("AppLanguage", "vi");
        _ = _audioService.SpeakAsync(narration.Text, tappedItem.PoiName, currentLang).ContinueWith(_ =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                tappedItem.IsPlaying = false;
                if (_currentPlayingItem == tappedItem)
                    _currentPlayingItem = null;
            });
        });
    }

    private async Task<NarrationDto?> FetchNarrationAsync(int poiId)
    {
        try
        {
            string currentLang = Preferences.Get("AppLanguage", "vi");
            var response = await _http.GetAsync($"api/Narrations/poi/{poiId}/language/{currentLang}");

            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<NarrationDto>();
        }
        catch
        {
            return null;
        }
    }

    private async void OnBackTapped(object sender, TappedEventArgs e)
    {
        if (_audioService.IsSpeaking) _audioService.Stop();
        await Navigation.PopAsync();
    }

    // ════════════════════════════════════════════════════════════════
    // ViewModel
    // ════════════════════════════════════════════════════════════════
    public class PlaylistItemViewModel : INotifyPropertyChanged
    {
        public int PoiId { get; }
        public string PoiName { get; }
        public string? RestaurantName { get; }
        public int DisplayOrder { get; }
        public string PlayingStatusText { get; }

        public bool HasRestaurantName => !string.IsNullOrEmpty(RestaurantName);

        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying == value) return;
                _isPlaying = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlayButtonIcon));
            }
        }

        // CHỈ ĐỔI ICON, BỎ VIỆC ĐỔI MÀU NỀN
        public string PlayButtonIcon => IsPlaying ? "⏸" : "▶";

        public PlaylistItemViewModel(PlaylistItemDto dto, string playingStatusText)
        {
            PoiId = dto.PoiId;
            PoiName = dto.PoiName;
            RestaurantName = dto.RestaurantName;
            DisplayOrder = dto.DisplayOrder;
            PlayingStatusText = playingStatusText;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // [ĐÃ BỎ TRƯỜNG ADDRESS]
    public record PlaylistItemDto(
        int PoiId,
        string PoiName,
        string? RestaurantName,
        double Latitude,
        double Longitude,
        int DisplayOrder
    );

    public record NarrationDto(string Text, string? VoiceName, float SpeechRate, float Volume);
}