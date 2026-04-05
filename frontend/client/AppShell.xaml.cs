using client.lib.screens.login;
using System.Globalization;

namespace client;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // CHỈ CẦN ĐĂNG KÝ CÁC ROUTE ẨN
        Routing.RegisterRoute("LoginScreen", typeof(LoginScreen));
        Routing.RegisterRoute("RegisterScreen", typeof(RegisterScreen));
        Routing.RegisterRoute("ProfileScreen", typeof(ProfileScreen));

        string savedLang = Preferences.Get("AppLanguage", "vi");

        // Ép hệ thống dùng ngôn ngữ này ngay lập tức trước khi vẽ Tab
        var culture = new CultureInfo(savedLang);
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        client.Resources.String.AppResources.Culture = culture;

        // Đã xóa BindingContext và UpdateTabsLanguage() vì chỉ dùng Icon cho TabBar
    }
}