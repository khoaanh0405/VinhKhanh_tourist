using System.Globalization;

namespace client;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Đã xóa Routing.RegisterRoute cho Login, Register, Profile

        string savedLang = Preferences.Get("AppLanguage", "vi");

        var culture = new CultureInfo(savedLang);
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        client.Resources.String.AppResources.Culture = culture;
    }

    // Đã xóa hoàn toàn hàm OnAppearing() vì không còn logic kiểm tra IsLoggedIn
}