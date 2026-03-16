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

        protected override void OnAppearing()
        {
            base.OnAppearing();

            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);

            if (isLoggedIn)
            {
                ProfileIconBorder.BackgroundColor = Color.FromArgb("#2ECC71"); // Xanh khi đã đăng nhập
                if (_viewModel != null)
                {
                    _viewModel.IsAutoNarrationEnabled = Preferences.Get("AutoNarration", false);
                }
            }
            else
            {
                ProfileIconBorder.BackgroundColor = Color.FromArgb("#000000");
                if (_viewModel != null)
                {
                    _viewModel.IsAutoNarrationEnabled = false;
                    Preferences.Set("AutoNarration", false);
                }
            }

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