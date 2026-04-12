using client.lib.services;

namespace client.lib.screens.login;

public partial class LoginScreen : ContentPage
{
    private readonly ApiService _apiService = new ApiService();
    private bool _isBusy = false;

    public LoginScreen()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (_isBusy) return;

        string username = UsernameEntry.Text?.Trim() ?? string.Empty;
        string password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Thông báo", "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.", "OK");
            return;
        }

        try
        {
            _isBusy = true;
            // Gợi ý UX: Đổi màu nút hoặc thêm chữ "Đang đăng nhập..." ở đây

            var response = await _apiService.LoginAsync(username, password);

            if (response != null && !string.IsNullOrEmpty(response.Token))
            {
                await SecureStorage.Default.SetAsync("jwt_token", response.Token);
                Preferences.Set("IsLoggedIn", true);
                Preferences.Set("CurrentUsername", username);

                // UX: Xóa text mật khẩu sau khi đăng nhập thành công
                PasswordEntry.Text = string.Empty;

                await DisplayAlert("Thành công", "Đăng nhập thành công!", "OK");

                await Shell.Current.GoToAsync("..");
            }
            else
            {
                // UX: Nếu sai mật khẩu, xóa trường mật khẩu bắt nhập lại, giữ lại Username
                PasswordEntry.Text = string.Empty;
                string errorMsg = response?.Message ?? "Tài khoản hoặc mật khẩu không chính xác.";
                await DisplayAlert("Đăng nhập thất bại", errorMsg, "Thử lại");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi kết nối", "Hệ thống đang bảo trì hoặc mất mạng.", "OK");
            System.Diagnostics.Debug.WriteLine($"Login Error: {ex.Message}");
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async void OnGoToRegisterTapped(object sender, TappedEventArgs e)
    {
        if (_isBusy) return;
        await Shell.Current.GoToAsync("RegisterScreen");
    }

    private async void OnCloseClicked(object sender, TappedEventArgs e)
    {
        if (_isBusy) return;
        await Shell.Current.GoToAsync("..");
    }
}