using client.lib.services;
using System.Text.RegularExpressions; // Thêm thư viện này

namespace client.lib.screens.login;

public partial class RegisterScreen : ContentPage
{
    private readonly ApiService _apiService = new ApiService();
    private bool _isBusy = false; // Cờ chống spam click

    public RegisterScreen()
    {
        InitializeComponent();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        if (_isBusy) return;

        // 1. Chuẩn hóa dữ liệu: Xóa khoảng trắng thừa ở đầu/cuối
        string displayName = DisplayNameEntry.Text?.Trim() ?? string.Empty;
        string username = UsernameEntry.Text?.Trim() ?? string.Empty;
        string email = EmailEntry.Text?.Trim() ?? string.Empty;
        string password = PasswordEntry.Text ?? string.Empty;
        string confirmPassword = ConfirmPasswordEntry.Text ?? string.Empty;

        // 2. Ràng buộc bỏ trống
        if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(username) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            await DisplayAlert("Lỗi", "Vui lòng điền đầy đủ tất cả các trường.", "OK");
            return;
        }

        // 3. Ràng buộc Username (3-20 ký tự, không chứa ký tự đặc biệt/khoảng trắng)
        if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]{3,20}$"))
        {
            await DisplayAlert("Lỗi định dạng", "Tên đăng nhập từ 3-20 ký tự, không chứa khoảng trắng và ký tự đặc biệt.", "OK");
            return;
        }

        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            await DisplayAlert("Lỗi định dạng", "Vui lòng nhập đúng định dạng Email (VD: nguyenvan@gmail.com).", "OK");
            return;
        }

        // 4. Ràng buộc Password (Ít nhất 6 ký tự)
        if (password.Length < 6)
        {
            await DisplayAlert("Mật khẩu yếu", "Mật khẩu phải có ít nhất 6 ký tự.", "OK");
            return;
        }

        // 5. Khớp mật khẩu
        if (password != confirmPassword)
        {
            await DisplayAlert("Lỗi", "Mật khẩu xác nhận không trùng khớp.", "OK");
            return;
        }

        try
        {
            SetLoadingState(true);

            // GỌI API ĐĂNG KÝ
            var response = await _apiService.RegisterAsync(displayName, username, email, password);

            if (response != null && response.UserId != null)
            {
                await DisplayAlert("Thành công", "Đăng ký tài khoản thành công! Vui lòng đăng nhập.", "OK");

                // UX: Xóa trắng form để an toàn
                DisplayNameEntry.Text = string.Empty;
                UsernameEntry.Text = string.Empty;
                PasswordEntry.Text = string.Empty;
                ConfirmPasswordEntry.Text = string.Empty;

                await Shell.Current.GoToAsync("..");
            }
            else
            {
                string errorMsg = response?.Message ?? "Tên đăng nhập đã tồn tại hoặc server không phản hồi.";
                await DisplayAlert("Đăng ký thất bại", errorMsg, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi kết nối", "Không thể kết nối đến máy chủ. Vui lòng thử lại sau.", "OK");
            System.Diagnostics.Debug.WriteLine($"Register Error: {ex.Message}");
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    // Hàm phụ trợ quản lý UI khi đang Loading
    private void SetLoadingState(bool isLoading)
    {
        _isBusy = isLoading;
        // Giả sử nút đăng ký của bạn có x:Name="BtnRegister" trong file XAML
        // BtnRegister.IsEnabled = !isLoading; 
        // BtnRegister.Text = isLoading ? "Đang xử lý..." : "Đăng ký";
    }

    private async void OnGoToLoginTapped(object sender, TappedEventArgs e)
    {
        if (_isBusy) return;
        await Shell.Current.GoToAsync("..");
    }

    private async void OnBackClicked(object sender, TappedEventArgs e)
    {
        if (_isBusy) return;
        await Shell.Current.GoToAsync("..");
    }
}