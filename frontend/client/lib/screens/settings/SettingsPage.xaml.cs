using client.lib.screens.home;     // HomeViewModel
using client.lib.services;          // AppViewModel
using client.lib.screens.qr;        // QrScannerPage
using client.Resources.String;      // AppResources
using client.lib.core;              // LocalizationResourceManager
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Globalization;

namespace client.lib.screens.settings;

public partial class SettingsPage : ContentPage
{
    private static readonly string[] LangCodes = { "vi", "en", "ko" };
    private static readonly string[] LangNames = { "Tiếng Việt 🇻🇳", "English 🇬🇧", "한국어 🇰🇷" };

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
            Preferences.Set("AppLanguage", newCode);

            var culture = new CultureInfo(newCode);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            // 🔥 ĐÂY LÀ ĐIỂM "ĂN TIỀN": GỌI RESOURCE MANAGER ĐỂ KÍCH HOẠT VẼ LẠI CHỮ TRÊN GIAO DIỆN
            LocalizationResourceManager.Instance.SetCulture(culture);

            var services = Application.Current.Handler.MauiContext.Services;
            var homeViewModel = services.GetService<HomeViewModel>();
            var appViewModel = services.GetService<AppViewModel>();

            appViewModel?.RefreshTranslations();

            if (homeViewModel != null)
            {
                _ = Task.Run(async () =>
                {
                    var matchedLang = homeViewModel.AvailableLanguages
                        .FirstOrDefault(l => l.LanguageCode == newCode);

                    if (matchedLang != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            homeViewModel.SelectedLanguage = matchedLang;
                        });
                    }

                    await homeViewModel.LoadDataAsync();
                });
            }

            // Lấy thông báo Toast theo ngôn ngữ mới ngay lập tức
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
}