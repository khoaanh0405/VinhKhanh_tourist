using client.lib.services;

namespace client.lib.screens.login;

public partial class RegisterScreen : ContentPage
{
    public RegisterScreen()
    {
        InitializeComponent();
    }

    private readonly ApiService _apiService = new ApiService();

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        string displayName = DisplayNameEntry.Text;
        string username = UsernameEntry.Text;
        string password = PasswordEntry.Text;
        string confirmPassword = ConfirmPasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            await DisplayAlert("Thông báo", "Vui lòng nhập đầy đủ thông tin", "OK");
            return;
        }

        if (password != confirmPassword)
        {
            await DisplayAlert("Lỗi", "Mật khẩu xác nhận không khớp", "OK");
            return;
        }

        // GỌI API ĐĂNG KÝ THẬT
        var response = await _apiService.RegisterAsync(displayName, username, password);

        // Kiểm tra UserId trả về từ API (trong class AuthResponse của ApiService)
        if (response != null && response.UserId != null)
        {
            await DisplayAlert("Thành công", "Đăng ký tài khoản thành công! Vui lòng đăng nhập.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            string errorMsg = response?.Message ?? "Đăng ký thất bại, không thể kết nối server.";
            await DisplayAlert("Lỗi", errorMsg, "OK");
        }
    }

    // 2. Xử lý khi nhấn chữ "Đăng nhập" ở dưới cùng
    private async void OnGoToLoginTapped(object sender, TappedEventArgs e)
    {
        // Vì trước khi vào Đăng ký, chúng ta đang ở trang Đăng nhập, nên chỉ cần lùi lại 1 trang (..)
        await Shell.Current.GoToAsync("..");
    }

    // 3. Xử lý khi nhấn dấu "X" ở góc trên cùng
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}