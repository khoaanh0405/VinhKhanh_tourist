using client.lib.screens.login;
using System.Globalization;

namespace client;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("LoginScreen", typeof(LoginScreen));
        Routing.RegisterRoute("RegisterScreen", typeof(RegisterScreen));
        Routing.RegisterRoute("ProfileScreen", typeof(ProfileScreen));

        string savedLang = Preferences.Get("AppLanguage", "vi");

        var culture = new CultureInfo(savedLang);
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        client.Resources.String.AppResources.Culture = culture;
    }

    // THÊM HÀM NÀY ĐỂ KIỂM TRA TRẠNG THÁI ĐĂNG NHẬP KHI MỞ APP
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Kiểm tra cờ IsLoggedIn đã lưu từ LoginScreen
        bool isLoggedIn = Preferences.Get("IsLoggedIn", false);

        if (isLoggedIn)
        {
            // Nếu đã đăng nhập, hệ thống sẽ tự động ở lại HomePage (hoặc Tab hiện tại)
            // Bạn có thể không cần làm gì thêm ở đây, hoặc lấy thêm token nếu cần
            System.Diagnostics.Debug.WriteLine("Người dùng đã đăng nhập từ phiên trước.");
        }
        else
        {

        }
    }
}