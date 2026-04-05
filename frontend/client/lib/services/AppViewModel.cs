using client.lib.model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace client.lib.services
{
    public partial class AppViewModel : ObservableObject
    {
        public ObservableCollection<POI> FavoritePOIs { get; } = new();
        public ObservableCollection<Restaurant> FavoriteRestaurants { get; } = new();

        public bool IsPOIFavorite(int poiId) => FavoritePOIs.Any(p => p.PoiId == poiId);

        // Đã xóa FavoritesTitle vì key TabFavorites không còn trong file .resx
        // Ghi chú: Nếu file XAML của trang Favorites cần 1 cái Title để hiển thị, 
        // bạn có thể dùng tạm key TopFavorites như sau:
        // public string FavoritesTitle => client.Resources.String.AppResources.TopFavorites;

        public string SearchFavoritesPlaceholder => client.Resources.String.AppResources.SearchFavoritesPlaceholder;
        public string EmptyFavoritesText => client.Resources.String.AppResources.EmptyFavoritesText;

        // Hàm này để HomeViewModel gọi tới khi đổi ngôn ngữ, giúp UI tự động update
        public void RefreshTranslations()
        {
            // Đã xóa dòng OnPropertyChanged(nameof(FavoritesTitle));
            OnPropertyChanged(nameof(SearchFavoritesPlaceholder));
            OnPropertyChanged(nameof(EmptyFavoritesText));
        }

        [RelayCommand]
        public void TogglePOIFavorite(POI poi)
        {
            if (IsPOIFavorite(poi.PoiId))
                FavoritePOIs.Remove(FavoritePOIs.First(p => p.PoiId == poi.PoiId));
            else
                FavoritePOIs.Add(poi);
        }

        [RelayCommand]
        public void ToggleRestaurantFavorite(Restaurant restaurant)
        {
            var existing = FavoriteRestaurants.FirstOrDefault(r => r.RestaurantId == restaurant.RestaurantId);
            if (existing != null)
                FavoriteRestaurants.Remove(existing);
            else
                FavoriteRestaurants.Add(restaurant);
        }

        // Language Logic (Giản lược)
        [ObservableProperty]
        private string _currentLanguageCode = "vi";

        [RelayCommand]
        public void ChangeLanguage(string code)
        {
            CurrentLanguageCode = code;
            // Thực hiện logic đổi ResourceDictionary hoặc CultureInfo tại đây
        }

        public void UpdateFavoritesTranslations(List<POI> freshPois)
        {
            for (int i = 0; i < FavoritePOIs.Count; i++)
            {
                var freshPoi = freshPois.FirstOrDefault(p => p.PoiId == FavoritePOIs[i].PoiId);
                if (freshPoi != null)
                {
                    // Gán đè object mới vào vị trí cũ, UI sẽ tự động nháy lên ngôn ngữ mới
                    FavoritePOIs[i] = freshPoi;
                }
            }
        }
    }
}