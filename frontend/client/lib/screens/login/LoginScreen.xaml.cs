using client.lib.services;

namespace client.lib.screens.login;

public partial class LoginScreen : ContentPage
{
    public LoginScreen()
    {
        InitializeComponent();
    }

    private readonly ApiService _apiService = new ApiService();

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string username = UsernameEntry.Text;
        string password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Thông báo", "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu", "OK");
            return;
        }

        // GỌI API ĐĂNG NHẬP THẬT
        var response = await _apiService.LoginAsync(username, password);

        // Backend trả về Token nếu thành công
        if (response != null && !string.IsNullOrEmpty(response.Token))
        {
            // Lưu JWT Token bằng SecureStorage (Bảo mật)
            await SecureStorage.Default.SetAsync("jwt_token", response.Token);

            // Lưu thông tin cơ bản để hiển thị lên ProfileScreen
            Preferences.Set("IsLoggedIn", true);
            Preferences.Set("CurrentUsername", username);

            await DisplayAlert("Thành công", "Đăng nhập thành công!", "OK");
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            string errorMsg = response?.Message ?? "Không thể kết nối đến server.";
            await DisplayAlert("Lỗi", errorMsg, "Thử lại");
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