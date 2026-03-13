using client.lib.screens.login;

namespace client;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // 2. ĐĂNG KÝ CÁC ROUTE ẨN (Các trang không nằm trên thanh Tab bar ở dưới cùng)
        Routing.RegisterRoute("LoginScreen", typeof(LoginScreen));
        Routing.RegisterRoute("RegisterScreen", typeof(RegisterScreen));

        // Nếu bạn đã tạo ProfilePage thì mở comment dòng dưới
        // Routing.RegisterRoute("ProfilePage", typeof(ProfilePage)); 
    }
}