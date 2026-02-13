import 'food.dart';

class Restaurant {
  final int restaurantId;
  final String name;
  final String? address;
  final String? description;
  final List<Food> foods;

  Restaurant({
    required this.restaurantId,
    required this.name,
    this.address,
    this.description,
    this.foods = const [],
  });

  factory Restaurant.fromJson(Map<String, dynamic> json) {
    return Restaurant(
      restaurantId: json['restaurantId'],
      name: json['name'],
      address: json['address'],
      description: json['description'],
      foods: (json['foods'] as List<dynamic>?)
              ?.map((f) => Food.fromJson(f))
              .toList() ??
          [],
    );
  }
}
