using client.lib.screens.home;
using client.lib.model;
using client.lib.screens.search;
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
            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);

            if (isLoggedIn)
            {
                await Shell.Current.GoToAsync("ProfileScreen");
            }
            else
            {
                await Shell.Current.GoToAsync("LoginScreen");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);

            if (isLoggedIn)
            {
                ProfileIconBorder.BackgroundColor = Color.FromArgb("#2ECC71"); // Xanh khi đã đăng nhập
                ProfileIconPath.Fill = Brush.White; // Đổi icon thành màu trắng để không bị đè màu

                if (_viewModel != null)
                {
                    _viewModel.IsAutoNarrationEnabled = Preferences.Get("AutoNarration", false);
                }
            }
            else
            {
                ProfileIconBorder.BackgroundColor = Color.FromArgb("#000000"); // Đen khi chưa đăng nhập
                ProfileIconPath.Fill = Brush.White; // Đổi icon thành màu trắng

                if (_viewModel != null)
                {
                    _viewModel.IsAutoNarrationEnabled = false;
                    Preferences.Set("AutoNarration", false);
                }
            }

            if (_viewModel != null && (_viewModel.Pois == null || !_viewModel.Pois.Any()))
            {
                await _viewModel.LoadDataAsync();
            }
        }

        private async Task NavigateToDetailAsync(object sender)
        {
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

        private async void OnSearchTapped(object sender, TappedEventArgs e)
        {
            // Lấy data POI thật từ HomeViewModel (_viewModel.Pois) truyền sang Search
            if (_viewModel != null && _viewModel.Pois != null)
            {
                var searchViewModel = new SearchViewModel(_viewModel.Pois);
                await Navigation.PushAsync(new client.lib.screens.search.SearchPage(searchViewModel));
            }
        }
    }
}