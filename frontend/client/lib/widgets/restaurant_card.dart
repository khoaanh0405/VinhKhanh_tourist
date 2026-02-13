import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:vinh_khanh_tourism/models/models.dart';
import 'package:vinh_khanh_tourism/providers/app_provider.dart';
import 'package:vinh_khanh_tourism/core/constants/app_constants.dart';
import 'restaurant_details_sheet.dart';

class RestaurantCard extends StatelessWidget {
  final Restaurant restaurant;

  const RestaurantCard({super.key, required this.restaurant});

  @override
  Widget build(BuildContext context) {
    final appProvider = context.watch<AppProvider>();

    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: InkWell(
        onTap: () {
          showModalBottomSheet(
            context: context,
            isScrollControlled: true,
            builder: (context) =>
                RestaurantDetailsSheet(restaurant: restaurant),
          );
        },
        borderRadius: BorderRadius.circular(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              height: 140,
              decoration: const BoxDecoration(
                color: AppConstants.secondaryColor,
                borderRadius: BorderRadius.vertical(
                  top: Radius.circular(12),
                ),
              ),
              child: const Center(
                child: Icon(
                  Icons.restaurant_menu,
                  size: 56,
                  color: AppConstants.primaryColor,
                ),
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          restaurant.name,
                          style: const TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                      IconButton(
                        icon: Icon(
                          appProvider
                                  .isRestaurantFavorite(restaurant.restaurantId)
                              ? Icons.favorite
                              : Icons.favorite_border,
                          color: appProvider
                                  .isRestaurantFavorite(restaurant.restaurantId)
                              ? Colors.red
                              : Colors.grey,
                        ),
                        onPressed: () =>
                            appProvider.toggleRestaurantFavorite(restaurant),
                      ),
                    ],
                  ),
                  if (restaurant.address != null) ...[
                    const SizedBox(height: 4),
                    Row(
                      children: [
                        const Icon(Icons.location_on,
                            size: 16, color: Colors.grey),
                        const SizedBox(width: 4),
                        Expanded(
                          child: Text(
                            restaurant.address!,
                            style: const TextStyle(
                                color: Colors.grey, fontSize: 12),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                      ],
                    ),
                  ],
                  if (restaurant.description != null) ...[
                    const SizedBox(height: 4),
                    Text(
                      restaurant.description!,
                      style: const TextStyle(fontSize: 13),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                  if (restaurant.foods.isNotEmpty) ...[
                    const SizedBox(height: 8),
                    Chip(
                      label: Text('${restaurant.foods.length} món ăn'),
                      avatar: const Icon(Icons.fastfood, size: 16),
                      materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                    ),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
