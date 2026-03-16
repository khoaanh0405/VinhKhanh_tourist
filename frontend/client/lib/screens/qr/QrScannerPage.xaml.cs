using client.lib.screens.poi;
using Microsoft.Maui.Dispatching;
using System.Text.RegularExpressions;
using ZXing.Net.Maui;

namespace client.lib.screens.qr;

public partial class QrScannerPage : ContentPage
{
    public QrScannerPage()
    {
        InitializeComponent();
    }

    private async void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var first = e.Results.FirstOrDefault();
        if (first == null) return;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            string code = first.Value;

            var match = Regex.Match(code, @"/poi/(\d+)");
            if (match.Success)
            {
                int poiId = int.Parse(match.Groups[1].Value);
                await Navigation.PushAsync(new POIDetailPage(poiId));
            }
        });
    }
}
