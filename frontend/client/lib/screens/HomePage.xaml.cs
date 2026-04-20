using client.lib.model;
using client.lib.screens.home;
using client.lib.screens.poi;
using client.lib.services;
using System.Collections.ObjectModel;
using System.Linq;
// ĐÃ XÓA: using client.lib.screens.search;

namespace client.lib.screens
{
    public partial class HomePage : ContentPage
    {
        private HomeViewModel _viewModel;
        private readonly LocalDbService _localDb = new LocalDbService();

        public HomePage(HomeViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_viewModel != null)
            {
                _viewModel.IsAutoNarrationEnabled = Preferences.Get("AutoNarration", false);

                string savedLang = Preferences.Get("AppLanguage", "vi");

                // 1. Thêm cờ đánh dấu việc thay đổi ngôn ngữ
                bool isLanguageChanged = false;

                if (_viewModel.SelectedLanguage == null || _viewModel.SelectedLanguage.LanguageCode != savedLang)
                {
                    var matchedLang = _viewModel.AvailableLanguages.FirstOrDefault(l => l.LanguageCode == savedLang);
                    if (matchedLang != null)
                    {
                        _viewModel.SelectedLanguage = matchedLang;
                        isLanguageChanged = true; // 2. Bật cờ nếu phát hiện ngôn ngữ mới
                    }
                }

                // 3. SỬA Ở ĐÂY: Thêm điều kiện isLanguageChanged để ép load lại data mới
                if (_viewModel.Pois == null || !_viewModel.Pois.Any() || isLanguageChanged)
                {
                    await LoadDataOfflineFirst();

                    bool hasInternet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

                    // Có mạng là gọi API cập nhật luôn nội dung đa ngôn ngữ
                    if (hasInternet)
                    {
                        _ = _viewModel.LoadDataAsync();
                    }
                }
            }
        }

        private async Task LoadDataOfflineFirst()
        {
            // ... (Giữ nguyên logic LoadDataOfflineFirst tuyệt vời của bạn) ...
            // Tôi rút gọn hiển thị ở đây để tránh quá dài, bạn cứ giữ nguyên toàn bộ hàm này nhé.
        }

        private async Task NavigateToDetailAsync(object sender)
        {
            var element = sender as View;
            var selectedPOI = (element?.GestureRecognizers.FirstOrDefault() as TapGestureRecognizer)?.CommandParameter as POI
                              ?? element?.BindingContext as POI;

            if (selectedPOI != null)
            {
                await Navigation.PushAsync(new POIDetailPage(selectedPOI.PoiId, autoPlayAudio: false));
            }
        }

        public async void OnNavigateToDetail(object sender, EventArgs e)
        {
            await NavigateToDetailAsync(sender);
        }

        // ĐÃ XÓA: Hàm OnSearchTapped
    }
}