// DetailScreen.xaml.cs — PATCH: thêm handler nút Back trong Hero header
// Chỉ cần thêm method này vào cuối class DetailScreen.
// Toàn bộ logic còn lại GIỮ NGUYÊN không đổi.

private async void OnBackTapped(object sender, TappedEventArgs e)
{
    await Navigation.PopAsync();
}
