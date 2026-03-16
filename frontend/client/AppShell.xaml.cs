using client.lib.screens.login;

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
    }
}