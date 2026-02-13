class Food {
  final int foodId;
  final String name;
  final double price;
  final String? description;
  final String? imageUrl;

  Food({
    required this.foodId,
    required this.name,
    required this.price,
    this.description,
    this.imageUrl,
  });

  factory Food.fromJson(Map<String, dynamic> json) {
    return Food(
      foodId: json['foodId'],
      name: json['name'],
      price: (json['price'] as num).toDouble(),
      description: json['description'],
      imageUrl: json['imageUrl'],
    );
  }
}
