import 'package:flutter/material.dart';
import 'package:vinh_khanh_tourism/models/models.dart';

class AppProvider extends ChangeNotifier {
  final List<POI> _favoritePOIs = [];
  final List<Restaurant> _favoriteRestaurants = [];

  List<POI> get favoritePOIs => _favoritePOIs;
  List<Restaurant> get favoriteRestaurants => _favoriteRestaurants;

  bool isPOIFavorite(int poiId) {
    return _favoritePOIs.any((poi) => poi.poiId == poiId);
  }

  bool isRestaurantFavorite(int restaurantId) {
    return _favoriteRestaurants.any((r) => r.restaurantId == restaurantId);
  }

  void togglePOIFavorite(POI poi) {
    if (isPOIFavorite(poi.poiId)) {
      _favoritePOIs.removeWhere((p) => p.poiId == poi.poiId);
    } else {
      _favoritePOIs.add(poi);
    }
    notifyListeners();
  }

  void toggleRestaurantFavorite(Restaurant restaurant) {
    if (isRestaurantFavorite(restaurant.restaurantId)) {
      _favoriteRestaurants
          .removeWhere((r) => r.restaurantId == restaurant.restaurantId);
    } else {
      _favoriteRestaurants.add(restaurant);
    }
    notifyListeners();
  }
}
