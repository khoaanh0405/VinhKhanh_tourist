using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using client.lib.core; // FuzzySearchHelper
using client.lib.model;
using client.lib.screens.home; // DetailScreen

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

            GoBackCommand = new Command(async () =>
            {
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
            var topPois = _allPois.OrderByDescending(p => p.AverageRating).Take(4).ToList();
            foreach (var poi in topPois)
            {
                SearchSuggestions.Add(poi);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // 🔥 NÂNG CẤP: Fuzzy Search + Relevance Scoring
        // ══════════════════════════════════════════════════════════════
        private void PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return;

            string keyword = SearchText.Trim();
            string normalizedQuery = FuzzySearchHelper.NormalizeText(keyword);

            // Tính điểm liên quan cho từng POI
            var scoredResults = _allPois
                .Select(p => new
                {
                    Poi = p,
                    Score = FuzzySearchHelper.CalculateRelevanceScore(
                        name: p.Name,
                        description: p.Description,
                        foodNames: p.Restaurants?
                            .Where(r => r.Foods != null)
                            .SelectMany(r => r.Foods.Select(f => f.Name)),
                        normalizedQuery: normalizedQuery
                    )
                })
                .Where(x => x.Score > 0)             // Chỉ lấy kết quả khớp
                .OrderByDescending(x => x.Score)      // Liên quan nhất lên đầu
                .ThenByDescending(x => x.Poi.AverageRating) // Cùng điểm → rating cao hơn
                .Select(x => x.Poi)
                .ToList();

            SearchResults.Clear();
            foreach (var item in scoredResults)
            {
                SearchResults.Add(item);
            }

            // Lưu vào lịch sử
            if (SearchHistory.Contains(SearchText))
                SearchHistory.Remove(SearchText);
            SearchHistory.Insert(0, SearchText);
            if (SearchHistory.Count > 10)
                SearchHistory.RemoveAt(SearchHistory.Count - 1);
            SaveHistory();

            IsShowingSuggestions = false;
        }

        private void OnHistorySelected(string historyItem)
        {
            SearchText = historyItem;
            PerformSearch();
        }

        private void OnSuggestionSelected(POI suggestion)
        {
            SearchText = suggestion.Name;
            PerformSearch();
        }

        private async void OnResultSelected(POI selectedPoi)
        {
            if (selectedPoi == null) return;
            await Application.Current.MainPage.Navigation.PushAsync(new DetailScreen(selectedPoi));
        }

        private void ShowSuggestions()
        {
            SearchResults.Clear();
            IsShowingSuggestions = true;
        }
    }
}