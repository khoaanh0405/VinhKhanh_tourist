using client.lib.screens.login;

namespace client;

public partial class AppShell : Shell
{
    // Thêm biến này làm chốt chặn để tránh vòng lặp vô tận
    private static bool _isFirstLaunch = true;

    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("LoginScreen", typeof(LoginScreen));
        Routing.RegisterRoute("RegisterScreen", typeof(RegisterScreen));
        Routing.RegisterRoute("ProfileScreen", typeof(ProfileScreen));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Chỉ tự động kiểm tra đăng nhập ở lần đầu tiên mở app
        if (_isFirstLaunch)
        {
            _isFirstLaunch = false; // Đánh dấu là đã kiểm tra xong

            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);

            if (!isLoggedIn)
            {
                // Dùng Dispatcher để đưa lệnh chuyển trang vào hàng đợi, 
                // đảm bảo giao diện trang chủ (Trang Khám Phá) được vẽ xong thì mới mở đè Login lên.
                Dispatcher.Dispatch(async () =>
                {
                    await Shell.Current.GoToAsync("LoginScreen");
                });
            }
        }
    }
}