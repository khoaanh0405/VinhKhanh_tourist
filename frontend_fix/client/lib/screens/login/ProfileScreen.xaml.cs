namespace client.lib.screens.login;

public partial class ProfileScreen : ContentPage
{
    public ProfileScreen()
    {
        InitializeComponent();
    }

    // Hàm này tự động chạy mỗi khi màn hình Profile được mở lên
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Kiểm tra xem người dùng đã đăng nhập chưa
        bool isLoggedIn = Preferences.Get("IsLoggedIn", false);

        if (isLoggedIn)
        {
            // Lấy tên đăng nhập đã lưu
            string currentUsername = Preferences.Get("CurrentUsername", "Người dùng");

            // Gắn lên giao diện
            WelcomeLabel.Text = $"Chào, {currentUsername}!";
            UsernameLabel.Text = currentUsername;
        }
        else
        {
            // Nếu chưa đăng nhập mà lỡ lạc vào trang này, đá văng về trang Đăng nhập
            Shell.Current.GoToAsync("//LoginScreen");
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        // Hỏi lại cho chắc
        bool confirm = await DisplayAlert("Đăng xuất", "Bạn có chắc chắn muốn đăng xuất khỏi tài khoản?", "Đăng xuất", "Hủy");

        if (confirm)
        {
            // 1. Xóa trạng thái đăng nhập
            Preferences.Remove("IsLoggedIn");
            Preferences.Remove("CurrentUsername");

            Preferences.Set("AutoNarration", false);

            // 2. Chuyển hướng về Trang chủ bằng lệnh lùi lại (..)
            await Shell.Current.GoToAsync("..");
        }
    }
}