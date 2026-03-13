using client.lib.screens.home;
using client.lib.model;
using System.Linq;

namespace client.lib.screens
{
    public partial class HomePage : ContentPage
    {
        private HomeViewModel _viewModel;

        public HomePage(HomeViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        private async void OnProfileIconTapped(object sender, TappedEventArgs e)
        {
            // Kiểm tra trạng thái đã đăng nhập chưa bằng Preferences
            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);

            if (isLoggedIn)
            {
                // Nếu đã đăng nhập, mở trang Quản lý tài khoản (Profile)
                await Shell.Current.GoToAsync("ProfilePage");
            }
            else
            {
                // Nếu chưa đăng nhập, mở trang Đăng nhập
                await Shell.Current.GoToAsync("LoginScreen");
            }
        }

        // Cập nhật lại Icon mỗi khi trang chủ xuất hiện (để đổi icon nếu user vừa đăng nhập xong)
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // 1. Cập nhật icon đăng nhập (Code cũ của bạn)
            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
            ProfileIconLabel.Text = isLoggedIn ? "🟢" : "👤";

            // 2. BỔ SUNG: Tự động gọi API tải dữ liệu ngay khi vào trang (nếu dữ liệu đang trống)
            if (_viewModel.Pois == null || !_viewModel.Pois.Any())
            {
                await _viewModel.LoadDataAsync();
            }
        }

        private async Task NavigateToDetailAsync(object sender)
        {
            var frame = sender as Frame;
            var selectedPOI = (frame?.GestureRecognizers.FirstOrDefault() as TapGestureRecognizer)?.CommandParameter as POI
                              ?? frame?.BindingContext as POI;

            if (selectedPOI != null)
            {
                await Navigation.PushAsync(new DetailScreen(selectedPOI));
            }
        }

        public async void OnNavigateToDetail(object sender, EventArgs e)
        {
            await NavigateToDetailAsync(sender);
        }
    }
}