namespace client.lib.screens.login;

public partial class ProfileScreen : ContentPage
{
    public ProfileScreen()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (Preferences.Get("IsLoggedIn", false))
        {
            string currentUsername = Preferences.Get("CurrentUsername", "Người dùng");
            WelcomeLabel.Text = $"Chào, {currentUsername}!";
            UsernameLabel.Text = currentUsername;
        }
        else
        {
            // Tránh việc nháy UI nếu chưa đăng nhập
            Shell.Current.GoToAsync("//MainPage"); // Đẩy thẳng về trang chủ gốc
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Đăng xuất", "Bạn có chắc chắn muốn đăng xuất khỏi tài khoản?", "Đăng xuất", "Hủy");

        if (confirm)
        {
            try
            {
                // 1. Xóa toàn bộ dữ liệu phiên làm việc
                Preferences.Remove("IsLoggedIn");
                Preferences.Remove("CurrentUsername");
                Preferences.Set("AutoNarration", false);
                SecureStorage.Default.Remove("jwt_token");

                // 2. CHUYỂN HƯỚNG VỀ HOMEPAGE (Đã sửa tên Route và bọc trong MainThread)
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.GoToAsync("//HomePage");
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Đã xảy ra lỗi: {ex.Message}", "OK");
            }
        }
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}