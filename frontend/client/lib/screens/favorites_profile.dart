import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:vinh_khanh_tourism/providers/app_provider.dart';
import 'package:vinh_khanh_tourism/widgets/poi_card.dart';
import 'package:vinh_khanh_tourism/widgets/restaurant_card.dart';

class FavoritesProfile extends StatelessWidget {
  const FavoritesProfile({super.key});

  @override
  Widget build(BuildContext context) {
    final appProvider = context.watch<AppProvider>();

    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Yêu thích'),
          bottom: const TabBar(
            tabs: [
              Tab(text: 'Địa điểm', icon: Icon(Icons.place)),
              Tab(text: 'Quán ăn', icon: Icon(Icons.restaurant)),
            ],
          ),
        ),
        body: TabBarView(
          children: [
            appProvider.favoritePOIs.isEmpty
                ? const Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.favorite_border,
                            size: 64, color: Colors.grey),
                        SizedBox(height: 16),
                        Text(
                          'Chưa có địa điểm yêu thích',
                          style: TextStyle(color: Colors.grey, fontSize: 16),
                        ),
                      ],
                    ),
                  )
                : ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: appProvider.favoritePOIs.length,
                    itemBuilder: (context, index) {
                      return POICard(poi: appProvider.favoritePOIs[index]);
                    },
                  ),
            appProvider.favoriteRestaurants.isEmpty
                ? const Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.restaurant_outlined,
                            size: 64, color: Colors.grey),
                        SizedBox(height: 16),
                        Text(
                          'Chưa có quán ăn yêu thích',
                          style: TextStyle(color: Colors.grey, fontSize: 16),
                        ),
                      ],
                    ),
                  )
                : ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: appProvider.favoriteRestaurants.length,
                    itemBuilder: (context, index) {
                      return RestaurantCard(
                        restaurant: appProvider.favoriteRestaurants[index],
                      );
                    },
                  ),
          ],
        ),
      ),
    );
  }
}
