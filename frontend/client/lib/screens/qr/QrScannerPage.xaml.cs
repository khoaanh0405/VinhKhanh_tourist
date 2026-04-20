using client.lib.screens.poi;
using client.lib.services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using ZXing.Net.Maui;
using System.Threading; // Bổ sung thư viện này để dùng Interlocked

namespace client.lib.screens.qr;

public partial class QrScannerPage : ContentPage
{
    // ── Chống scan trùng lặp an toàn tuyệt đối ──────────────────────
    // Đổi sang kiểu int (0 = false, 1 = true) để dùng Interlocked
    private int _isProcessing = 0;

    private readonly AudioService _audioService;
    private readonly TrackingService _trackingService;

    public QrScannerPage()
    {
        InitializeComponent();

        _audioService = Application.Current!
            .Handler!.MauiContext!.Services
            .GetRequiredService<AudioService>();

        _trackingService = Application.Current!
            .Handler!.MauiContext!.Services
            .GetRequiredService<TrackingService>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ResetScanState();

        // Bật lại camera khi người dùng quay lại trang này
        if (BarcodeReader != null)
            BarcodeReader.IsDetecting = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Tắt camera khi rời khỏi trang để tiết kiệm pin và chống quét ngầm
        if (BarcodeReader != null)
            BarcodeReader.IsDetecting = false;

        if (_audioService.IsSpeaking)
            _audioService.Stop();
    }

    // ── ZXing callback ──────────────────────────────────────────────
    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        // 1. KHÓA ĐA LUỒNG: Nếu đã có luồng đổi _isProcessing thành 1 thì các luồng khác sẽ bị return ngay lập tức.
        if (Interlocked.Exchange(ref _isProcessing, 1) == 1)
            return;

        var first = e.Results?.FirstOrDefault();
        if (first is null)
        {
            _isProcessing = 0;
            return;
        }

        string rawCode = first.Value?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(rawCode))
        {
            _isProcessing = 0;
            return;
        }

        // 2. TẮT TẠM THỜI CAMERA ĐỂ NGĂN QUÉT TIẾP KHUNG HÌNH SAU
        BarcodeReader.IsDetecting = false;

        // Chuyển việc xử lý UI sang MainThread
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await ProcessQrCodeAsync(rawCode);
        });
    }

    // ── Xử lý QR code đã scan ─────────────
    private async Task ProcessQrCodeAsync(string rawCode)
    {
        if (rawCode.StartsWith("app://vinhkhanh/poi/"))
        {
            string idString = rawCode.Replace("app://vinhkhanh/poi/", "");

            if (int.TryParse(idString, out int poiId))
            {
                System.Diagnostics.Debug.WriteLine($"[QR] Parse thành công → PoiId={poiId}");

                ShowScanFeedback();

                _ = _trackingService.LogQrScanAsync(poiId);

                await Navigation.PushAsync(new POIDetailPage(poiId, autoPlayAudio: true), animated: true);
            }
            else
            {
                await ShowInvalidQrToastAsync("Mã QR chứa ID không hợp lệ.");
                ResumeScanning(); // Quét sai thì cho quét lại
            }
        }
        else
        {
            await ShowInvalidQrToastAsync("Đây không phải là mã QR của địa điểm Vĩnh Khánh.");
            ResumeScanning(); // Quét sai thì cho quét lại
        }
    }

    // ── UI Helpers ────────────────────────────────────────────────────
    private void ResumeScanning()
    {
        // Trì hoãn 1.5 giây cho người dùng đọc Toast lỗi rồi mới mở lại camera
        Task.Delay(1500).ContinueWith(_ =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _isProcessing = 0;
                if (BarcodeReader != null)
                    BarcodeReader.IsDetecting = true;
            });
        });
    }

    private void ShowScanFeedback()
    {
        if (ScannerOverlay is not null)
        {
            ScannerOverlay.Stroke = new SolidColorBrush(Color.FromArgb("#00C853"));
            Task.Delay(400).ContinueWith(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    ScannerOverlay.Stroke = new SolidColorBrush(Color.FromArgb("#1ABC9C"))); // Trả về màu gốc Teal
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
        _isProcessing = 0;
    }

    private async void OnBackTapped(object sender, TappedEventArgs e)
    {
        _audioService.Stop();
        await Navigation.PopAsync();
    }
}