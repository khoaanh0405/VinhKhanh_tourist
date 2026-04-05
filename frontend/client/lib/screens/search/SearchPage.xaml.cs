namespace client.lib.screens.search
{
    public partial class SearchPage : ContentPage
    {
        public SearchPage(SearchViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Sử dụng Dispatcher để đảm bảo UI thread đã sẵn sàng trước khi gọi Focus
            await Task.Delay(100); // Đợi Animation chuyển trang hoàn tất (khoảng 100-200ms)
            MainSearchEntry.Focus();
        }
    }
}