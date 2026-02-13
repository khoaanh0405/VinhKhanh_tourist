import 'package:flutter/material.dart';
import 'package:vinh_khanh_tourism/models/models.dart';
import 'package:vinh_khanh_tourism/core/constants/app_constants.dart';
import 'food_item.dart';

class RestaurantDetailsSheet extends StatelessWidget {
  final Restaurant restaurant;

  const RestaurantDetailsSheet({super.key, required this.restaurant});

  @override
  Widget build(BuildContext context) {
    return DraggableScrollableSheet(
      initialChildSize: 0.7,
      minChildSize: 0.5,
      maxChildSize: 0.95,
      expand: false,
      builder: (context, scrollController) {
        return Container(
          decoration: const BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
          ),
          child: Column(
            children: [
              Container(
                margin: const EdgeInsets.only(top: 8),
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: Colors.grey[300],
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
              Expanded(
                child: ListView(
                  controller: scrollController,
                  padding: const EdgeInsets.all(16),
                  children: [
                    Container(
                      height: 120,
                      decoration: BoxDecoration(
                        color: AppConstants.secondaryColor,
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: const Center(
                        child: Icon(
                          Icons.restaurant_menu,
                          size: 64,
                          color: AppConstants.primaryColor,
                        ),
                      ),
                    ),
                    const SizedBox(height: 16),
                    Text(
                      restaurant.name,
                      style: const TextStyle(
                        fontSize: 24,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    if (restaurant.address != null) ...[
                      const SizedBox(height: 8),
                      Row(
                        children: [
                          const Icon(Icons.location_on,
                              size: 20, color: Colors.grey),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              restaurant.address!,
                              style: const TextStyle(color: Colors.grey),
                            ),
                          ),
                        ],
                      ),
                    ],
                    if (restaurant.description != null) ...[
                      const SizedBox(height: 12),
                      Text(
                        restaurant.description!,
                        style: const TextStyle(fontSize: 15),
                      ),
                    ],
                    const SizedBox(height: 24),
                    const Row(
                      children: [
                        Icon(Icons.menu_book),
                        SizedBox(width: 8),
                        Text(
                          'Thực đơn',
                          style: TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    if (restaurant.foods.isEmpty)
                      const Center(
                        child: Padding(
                          padding: EdgeInsets.all(32),
                          child: Text(
                            'Chưa có thực đơn',
                            style: TextStyle(color: Colors.grey),
                          ),
                        ),
                      )
                    else
                      ...restaurant.foods.map((food) => FoodItem(food: food)),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}
