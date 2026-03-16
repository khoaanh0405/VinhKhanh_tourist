namespace client.lib.screens.login;

public partial class RegisterScreen : ContentPage
{
    public RegisterScreen()
    {
        InitializeComponent();
    }

    // 1. Xử lý khi nhấn nút ĐĂNG KÝ NGAY
    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        string displayName = DisplayNameEntry.Text;
        string username = UsernameEntry.Text;
        string password = PasswordEntry.Text;
        string confirmPassword = ConfirmPasswordEntry.Text;

        // Kiểm tra xem người dùng có bỏ trống ô nào không
        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            await DisplayAlert("Thông báo", "Vui lòng nhập đầy đủ thông tin", "OK");
            return;
        }

        // Kiểm tra mật khẩu xác nhận có khớp không
        if (password != confirmPassword)
        {
            await DisplayAlert("Lỗi", "Mật khẩu xác nhận không khớp", "OK");
            return;
        }

        // TẠM THỜI MÔ PHỎNG ĐĂNG KÝ THÀNH CÔNG (Sau này bạn sẽ gọi API lưu vào DB ở đây)
        bool isRegisterSuccess = true;

        if (isRegisterSuccess)
        {
            await DisplayAlert("Thành công", "Đăng ký tài khoản thành công! Vui lòng đăng nhập.", "OK");

            // Đăng ký xong thì tự động quay lại trang Đăng nhập
            await Shell.Current.GoToAsync("..");
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