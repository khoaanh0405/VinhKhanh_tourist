using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using client.lib.model;
using client.lib.screens.home; // Thêm để gọi DetailScreen

namespace client.lib.screens.search
{
    public class SearchViewModel : BindableObject
    {
        private readonly IEnumerable<POI> _allPois;
        private const string HistoryKey = "SearchHistoryPrefs";

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                // Tùy chọn: Nếu muốn user xóa trắng ô search thì hiện lại gợi ý
                if (string.IsNullOrWhiteSpace(value))
                {
                    ShowSuggestions();
                }
            }
        }

        private bool _isShowingSuggestions = true;
        public bool IsShowingSuggestions
        {
            get => _isShowingSuggestions;
            set { _isShowingSuggestions = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsShowingResults)); }
        }

        public bool IsShowingResults => !IsShowingSuggestions;

        public ObservableCollection<string> SearchHistory { get; set; }
        public ObservableCollection<POI> SearchSuggestions { get; set; }
        public ObservableCollection<POI> SearchResults { get; set; }

        public ICommand SearchCommand { get; }
        public ICommand SelectHistoryCommand { get; }
        public ICommand SelectSuggestionCommand { get; }
        public ICommand ClearHistoryCommand { get; }
        public ICommand SelectResultCommand { get; }
        public ICommand GoBackCommand { get; }

        // Nhận dữ liệu thực tế từ Home truyền sang
        public SearchViewModel(IEnumerable<POI> allPois)
        {
            _allPois = allPois ?? new List<POI>();

            SearchHistory = new ObservableCollection<string>();
            SearchSuggestions = new ObservableCollection<POI>();
            SearchResults = new ObservableCollection<POI>();

            LoadHistory();
            LoadSuggestions();

            SearchCommand = new Command(PerformSearch);
            SelectHistoryCommand = new Command<string>(OnHistorySelected);
            SelectSuggestionCommand = new Command<POI>(OnSuggestionSelected);
            ClearHistoryCommand = new Command(ClearHistory);
            SelectResultCommand = new Command<POI>(OnResultSelected);

            GoBackCommand = new Command(async () => {
                await Application.Current.MainPage.Navigation.PopAsync();
            });
        }

        private void LoadHistory()
        {
            var historyJson = Preferences.Get(HistoryKey, string.Empty);
            if (!string.IsNullOrEmpty(historyJson))
            {
                var history = JsonSerializer.Deserialize<List<string>>(historyJson);
                foreach (var item in history)
                {
                    SearchHistory.Add(item);
                }
            }
        }

        private void SaveHistory()
        {
            var historyJson = JsonSerializer.Serialize(SearchHistory.ToList());
            Preferences.Set(HistoryKey, historyJson);
        }

        private void ClearHistory()
        {
            SearchHistory.Clear();
            Preferences.Remove(HistoryKey);
        }

        private void LoadSuggestions()
        {
            // Lấy 4 quán có Rating cao nhất hoặc ngẫu nhiên từ DATA THẬT
            var topPois = _allPois.OrderByDescending(p => p.AverageRating).Take(4).ToList();
            foreach (var poi in topPois)
            {
                SearchSuggestions.Add(poi);
            }
        }

        private void PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return;

            string keyword = SearchText.Trim().ToLower();

            // 1. Lọc dữ liệu thật (Tìm theo Tên POI, Mô tả, Tên Quán, Tên Món ăn)
            var results = _allPois.Where(p =>
                (p.Name != null && p.Name.ToLower().Contains(keyword)) ||
                (p.Description != null && p.Description.ToLower().Contains(keyword)) ||
                (p.Restaurants != null && p.Restaurants.Any(r =>
                    (r.Name != null && r.Name.ToLower().Contains(keyword)) ||
                    (r.Foods != null && r.Foods.Any(f => f.Name != null && f.Name.ToLower().Contains(keyword)))
                ))
            ).ToList();

            SearchResults.Clear();
            foreach (var item in results)
            {
                SearchResults.Add(item);
            }

            // 2. Lưu vào lịch sử (Chỉ lưu khi thực hiện search, đưa lên đầu)
            if (SearchHistory.Contains(SearchText))
                SearchHistory.Remove(SearchText);

            SearchHistory.Insert(0, SearchText);

            // Giữ tối đa 10 lịch sử gần nhất
            if (SearchHistory.Count > 10)
                SearchHistory.RemoveAt(SearchHistory.Count - 1);

            SaveHistory();

            // 3. Đổi view sang hiển thị kết quả
            IsShowingSuggestions = false;
        }

        private void OnHistorySelected(string historyItem)
        {
            SearchText = historyItem;
            PerformSearch();
        }

        private void OnSuggestionSelected(POI suggestion)
        {
            SearchText = suggestion.Name; // Điền text
            PerformSearch(); // Search luôn
        }

        private async void OnResultSelected(POI selectedPoi)
        {
            if (selectedPoi == null) return;

            // Điều hướng sang DetailScreen (sử dụng constructor như bạn đã khai báo ở DetailScreen.xaml.cs)
            await Application.Current.MainPage.Navigation.PushAsync(new DetailScreen(selectedPoi));
        }

        private void ShowSuggestions()
        {
            SearchResults.Clear();
            IsShowingSuggestions = true;
        }
    }
}