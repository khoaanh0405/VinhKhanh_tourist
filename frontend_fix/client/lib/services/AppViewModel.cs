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
    }
}