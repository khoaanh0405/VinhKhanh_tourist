using client.lib.screens.poi;
using client.lib.screens.Playlist;   // <-- THÊM using mới
using client.lib.services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using ZXing.Net.Maui;
using System.Threading;

namespace client.lib.screens.qr;

public partial class QrScannerPage : ContentPage
{
    private int _isProcessing = 0;

    private readonly AudioService _audioService;
    private readonly TrackingService _trackingService;

    public QrScannerPage()
    {
        InitializeComponent();

        _audioService = Application.Current!
            .Handler!.MauiContext!.Services.GetRequiredService<AudioService>();

        _trackingService = Application.Current!
            .Handler!.MauiContext!.Services.GetRequiredService<TrackingService>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ResetScanState();
        if (BarcodeReader != null) BarcodeReader.IsDetecting = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BarcodeReader != null) BarcodeReader.IsDetecting = false;
        if (_audioService.IsSpeaking) _audioService.Stop();
    }

    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (Interlocked.Exchange(ref _isProcessing, 1) == 1) return;

        var first = e.Results?.FirstOrDefault();
        if (first is null) { _isProcessing = 0; return; }

        string rawCode = first.Value?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(rawCode)) { _isProcessing = 0; return; }

        BarcodeReader.IsDetecting = false;
        MainThread.BeginInvokeOnMainThread(async () => await ProcessQrCodeAsync(rawCode));
    }

    private async Task ProcessQrCodeAsync(string rawCode)
    {
        // ── NHÁNH 1: QR của một POI đơn lẻ (giữ nguyên) ────────────
        if (rawCode.StartsWith("app://vinhkhanh/poi/"))
        {
            string idString = rawCode.Replace("app://vinhkhanh/poi/", "");
            if (int.TryParse(idString, out int poiId))
            {
                ShowScanFeedback();
                _ = _trackingService.LogQrScanAsync(poiId);
                await Navigation.PushAsync(new POIDetailPage(poiId, autoPlayAudio: true));
            }
            else
            {
                await ShowInvalidQrToastAsync("Mã QR chứa ID không hợp lệ.");
                ResumeScanning();
            }
            return;
        }

        // ── NHÁNH 2: QR của Playlist (MỚI) ──────────────────────────
        if (rawCode.StartsWith("app://vinhkhanh/playlist/"))
        {
            string idString = rawCode.Replace("app://vinhkhanh/playlist/", "");
            if (int.TryParse(idString, out int playlistId))
            {
                ShowScanFeedback();
                _ = _trackingService.LogPlaylistScanAsync(playlistId);
                await Navigation.PushAsync(new PlaylistPage(playlistId));
            }
            else
            {
                await ShowInvalidQrToastAsync("Mã QR Playlist không hợp lệ.");
                ResumeScanning();
            }
            return;
        }

        // ── NHÁNH 3: QR không nhận dạng được ────────────────────────
        await ShowInvalidQrToastAsync("Đây không phải là mã QR của Vĩnh Khánh.");
        ResumeScanning();
    }

    private void ResumeScanning()
    {
        Task.Delay(1500).ContinueWith(_ =>
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _isProcessing = 0;
                if (BarcodeReader != null) BarcodeReader.IsDetecting = true;
            }));
    }

    private void ShowScanFeedback()
    {
        if (ScannerOverlay is null) return;
        ScannerOverlay.Stroke = new SolidColorBrush(Color.FromArgb("#00C853"));
        Task.Delay(400).ContinueWith(_ =>
            MainThread.BeginInvokeOnMainThread(() =>
                ScannerOverlay.Stroke = new SolidColorBrush(Color.FromArgb("#1ABC9C"))));
    }

    private static async Task ShowInvalidQrToastAsync(string msg)
        => await Toast.Make(msg, ToastDuration.Short, 14).Show();

    private void ResetScanState() => _isProcessing = 0;

    private async void OnBackTapped(object sender, TappedEventArgs e)
    {
        _audioService.Stop();
        await Navigation.PopAsync();
    }
}