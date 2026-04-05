// QrScannerPage.xaml.cs — PATCH: thêm handler nút Back
// Dán method này vào cuối class QrScannerPage.
// Toàn bộ logic còn lại (OnBarcodesDetected) GIỮ NGUYÊN.

private async void OnBackTapped(object sender, TappedEventArgs e)
{
    await Navigation.PopAsync();
}
