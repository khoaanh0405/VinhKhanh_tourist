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
                // [ĐÃ SỬA] Thay "ProfilePage" thành "ProfileScreen" cho khớp với Route ở AppShell
                await Shell.Current.GoToAsync("ProfileScreen");
            }
            else
            {
                // Nếu chưa đăng nhập, mở trang Đăng nhập
                await Shell.Current.GoToAsync("LoginScreen");
            }
        }

        // Cập nhật lại giao diện mỗi khi trang chủ xuất hiện
        protected override void OnAppearing() // Bỏ chữ async đi cho an toàn
        {
            base.OnAppearing();

            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);

            if (isLoggedIn)
            {
                ProfileIconBorder.BackgroundColor = Color.FromArgb("#2ECC71");
                if (_viewModel != null)
                {
                    _viewModel.IsAutoNarrationEnabled = Preferences.Get("AutoNarration", false);
                }
            }
            else
            {
                ProfileIconBorder.BackgroundColor = Color.FromArgb("#E74C3C");
                if (_viewModel != null)
                {
                    _viewModel.IsAutoNarrationEnabled = false;
                    Preferences.Set("AutoNarration", false);
                }
            }

            // [ĐÃ SỬA]: Dùng Task.Run để đẩy việc tải dữ liệu xuống chạy ngầm,
            // giúp màn hình giao diện không bị đơ chờ đợi.
            if (_viewModel != null && (_viewModel.Pois == null || !_viewModel.Pois.Any()))
            {
                Task.Run(async () =>
                {
                    await _viewModel.LoadDataAsync();
                });
            }
        }

        private async Task NavigateToDetailAsync(object sender)
        {
            // SỬA Ở ĐÂY: Dùng View thay vì Frame để bắt được cả click từ Border (Banner) và Frame (Danh sách dưới)
            var element = sender as View;

            var selectedPOI = (element?.GestureRecognizers.FirstOrDefault() as TapGestureRecognizer)?.CommandParameter as POI
                              ?? element?.BindingContext as POI;

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