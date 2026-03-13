namespace client.lib.screens.login;

public partial class LoginScreen : ContentPage
{
    public LoginScreen()
    {
        InitializeComponent();
    }

    // 1. Xử lý khi nhấn nút ĐĂNG NHẬP
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string username = UsernameEntry.Text;
        string password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Thông báo", "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu", "OK");
            return;
        }

        // TẠM THỜI MÔ PHỎNG LOGIC ĐĂNG NHẬP THÀNH CÔNG (Sau này bạn sẽ gọi API ở đây)
        bool isLoginSuccess = true;

        if (isLoginSuccess)
        {
            // Lưu trạng thái đã đăng nhập vào Preferences của điện thoại
            Preferences.Set("IsLoggedIn", true);
            Preferences.Set("CurrentUsername", username);

            await DisplayAlert("Thành công", "Đăng nhập thành công!", "OK");

            // Đóng trang đăng nhập, quay lại trang trước đó (Trang chủ)
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await DisplayAlert("Lỗi", "Tên đăng nhập hoặc mật khẩu không đúng", "Thử lại");
        }
    }

    // 2. Xử lý khi nhấn "Đăng ký ngay"
    private async void OnGoToRegisterTapped(object sender, TappedEventArgs e)
    {
        // Điều hướng sang trang RegisterScreen
        await Shell.Current.GoToAsync("RegisterScreen");
    }

    // 3. Xử lý khi nhấn dấu "X" góc trên cùng
    private async void OnCloseClicked(object sender, EventArgs e)
    {
        // Quay lại trang trước đó mà không làm gì cả
        await Shell.Current.GoToAsync("..");
    }
}