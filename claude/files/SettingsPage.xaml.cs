using client.lib.screens.qr;

namespace client.lib.screens.settings;

/// <summary>
/// SettingsPage — tích hợp: Quét QR + Đổi ngôn ngữ + các tuỳ chọn app.
/// Language change vẫn đi qua HomeViewModel.SelectedLanguage để giữ nguyên
/// toàn bộ logic i18n đã có.
/// </summary>
public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Đồng bộ Picker với ngôn ngữ đang được lưu
        string savedLang = Preferences.Get("AppLanguage", "vi");
        LanguagePicker.SelectedIndex = savedLang switch
        {
            "vi" => 0,
            "en" => 1,
            "ko" => 2,
            _ => 0
        };
    }

    private void OnLanguageSelected(object sender, EventArgs e)
    {
        if (LanguagePicker.SelectedIndex < 0) return;

        string[] codes = { "vi", "en", "ko" };
        string selectedCode = codes[LanguagePicker.SelectedIndex];

        // Lưu lựa chọn, để HomeViewModel đọc khi app reload hoặc restart tab
        Preferences.Set("AppLanguage", selectedCode);

        // Nếu cần apply ngay lập tức, gọi qua HomeViewModel Singleton
        // (giống cách LanguageSelector widget cũ làm)
        // Ví dụ: var homeVm = Application.Current.Handler.MauiContext.Services
        //            .GetService<HomeViewModel>();
        // homeVm?.SelectedLanguage = homeVm.AvailableLanguages
        //            .FirstOrDefault(l => l.LanguageCode == selectedCode);
    }

    private async void OnQrScannerTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new QrScannerPage());
    }

    private async void OnAboutTapped(object sender, TappedEventArgs e)
    {
        await DisplayAlert(
            "Về ứng dụng 🍜",
            "Khám phá ẩm thực Vĩnh Khánh\n\nPhiên bản 1.0.0\nMade with ❤️ in Vietnam",
            "OK");
    }

    // Handler cho nút Back trên ProfileScreen (nếu điều hướng từ đó)
    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
