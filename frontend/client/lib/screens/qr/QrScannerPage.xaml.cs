using client.lib.screens.poi;
using client.lib.services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using ZXing.Net.Maui;

namespace client.lib.screens.qr;

public partial class QrScannerPage : ContentPage
{
    // ── Chống scan trùng lặp ────────────────────────────────────────
    private volatile bool _isProcessing = false;
    private string _lastScannedCode = string.Empty;
    private DateTime _lastScannedTime = DateTime.MinValue;

    private const int CooldownSeconds = 3;

    // ── Services ─────────────────────────────────────────────────────
    private readonly AudioService _audioService;

    // ── Constructor ──────────────────────────────────────────────────
    public QrScannerPage()
    {
        InitializeComponent();

        _audioService = Application.Current!
            .Handler!.MauiContext!.Services
            .GetRequiredService<AudioService>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ResetScanState();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_audioService.IsSpeaking)
            _audioService.Stop();
    }

    // ── ZXing callback ──────────────────────────────────────────────
    private async void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var first = e.Results?.FirstOrDefault();
        if (first is null) return;

        string rawCode = first.Value?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(rawCode)) return;

        if (_isProcessing) return;

        bool isSameCode = string.Equals(rawCode, _lastScannedCode, StringComparison.Ordinal);
        bool isInCooldown = (DateTime.Now - _lastScannedTime).TotalSeconds < CooldownSeconds;
        if (isSameCode && isInCooldown) return;

        _isProcessing = true;
        _lastScannedCode = rawCode;
        _lastScannedTime = DateTime.Now;

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await ProcessQrCodeAsync(rawCode));
        }
        finally
        {
            _isProcessing = false;
        }
    }

    // ── Xử lý QR code đã scan (Khớp với DB SQL của bạn) ─────────────
    private async Task ProcessQrCodeAsync(string rawCode)
    {
        // Kiểm tra xem QR code có bắt đầu bằng format chuẩn không
        if (rawCode.StartsWith("app://vinhkhanh/poi/"))
        {
            // Cắt lấy phần ID ở cuối chuỗi
            string idString = rawCode.Replace("app://vinhkhanh/poi/", "");

            // Ép kiểu sang số nguyên (int)
            if (int.TryParse(idString, out int poiId))
            {
                System.Diagnostics.Debug.WriteLine($"[QR] Parse thành công → PoiId={poiId}");

                ShowScanFeedback();

                // Chuyển trang và báo cho POIDetailPage biết là hãy tự động phát Audio
                await Navigation.PushAsync(new POIDetailPage(poiId, autoPlayAudio: true), animated: true);
            }
            else
            {
                await ShowInvalidQrToastAsync("Mã QR chứa ID không hợp lệ.");
            }
        }
        else
        {
            await ShowInvalidQrToastAsync("Đây không phải là mã QR của địa điểm Vĩnh Khánh.");
        }
    }

    // ── UI Helpers ────────────────────────────────────────────────────
    private void ShowScanFeedback()
    {
        // Hiệu ứng nháy viền xanh báo hiệu quét thành công
        if (ScannerOverlay is not null)
        {
            ScannerOverlay.Stroke = new SolidColorBrush(Color.FromArgb("#00C853"));
            Task.Delay(400).ContinueWith(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    ScannerOverlay.Stroke = new SolidColorBrush(Color.FromArgb("#FFFFFF")));
            });
        }
    }

    private static async Task ShowInvalidQrToastAsync(string errorDetail)
    {
        var toast = Toast.Make(errorDetail, ToastDuration.Short, 14);
        await toast.Show();
    }

    private void ResetScanState()
    {
        _isProcessing = false;
        _lastScannedCode = string.Empty;
        _lastScannedTime = DateTime.MinValue;
    }

    private async void OnBackTapped(object sender, TappedEventArgs e)
    {
        _audioService.Stop();
        await Navigation.PopAsync();
    }
}