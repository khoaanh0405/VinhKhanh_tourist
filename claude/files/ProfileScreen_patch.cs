// ProfileScreen.xaml.cs — PATCH: thêm handler nút Back trong Hero header
// Chỉ cần thêm method này vào file ProfileScreen.xaml.cs hiện có của bạn.
// Toàn bộ logic còn lại (OnAppearing, OnLogoutClicked) GIỮ NGUYÊN không đổi.

// Dán vào class ProfileScreen:

private async void OnBackButtonClicked(object sender, EventArgs e)
{
    await Shell.Current.GoToAsync("..");
}
