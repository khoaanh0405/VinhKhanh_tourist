import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:vinh_khanh_tourism/models/models.dart';
import 'package:vinh_khanh_tourism/providers/app_provider.dart';

// POI Card Widget
class POICard extends StatelessWidget {
  final POI poi;
  final VoidCallback onTap;

  const POICard({
    super.key,
    required this.poi,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            children: [
              // Image
              Container(
                width: 80,
                height: 80,
                decoration: BoxDecoration(
                  color: Colors.grey[300],
                  borderRadius: BorderRadius.circular(8),
                ),
                child: const Icon(
                  Icons.place,
                  size: 40,
                  color: Colors.grey,
                ),
              ),
              const SizedBox(width: 12),

              // Info
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      poi.name,
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                      ),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 4),
                    Row(
                      children: [
                        Icon(
                          Icons.restaurant,
                          size: 14,
                          color: Colors.grey[600],
                        ),
                        const SizedBox(width: 4),
                        Text(
                          '${poi.restaurants.length ?? 0} nhà hàng',
                          style: TextStyle(
                            fontSize: 12,
                            color: Colors.grey[600],
                          ),
                        ),
                      ],
                    ),
                    // NOTE: AppProvider hiện tại chưa có logic "visited".
                    // Bỏ qua hoặc comment lại phần check visited.
                    /*
                    const SizedBox(height: 4),
                    Row(
                      children: [
                        Icon(Icons.check_circle, size: 14, color: Colors.green[600]),
                        const SizedBox(width: 4),
                        Text('Đã tham quan', ...),
                      ],
                    ),
                    */
                  ],
                ),
              ),

              // Favorite icon
              Consumer<AppProvider>(
                builder: (context, provider, child) {
                  // Sửa tên hàm cho đúng với AppProvider
                  final isFavorite = provider.isPOIFavorite(poi.poiId);
                  return IconButton(
                    icon: Icon(
                      isFavorite ? Icons.favorite : Icons.favorite_border,
                      color: isFavorite ? Colors.red : Colors.grey,
                    ),
                    onPressed: () {
                      // Sửa tham số truyền vào: togglePOIFavorite nhận POI object
                      provider.togglePOIFavorite(poi);
                    },
                  );
                },
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// Restaurant Card Widget
class RestaurantCard extends StatelessWidget {
  final Restaurant restaurant;

  const RestaurantCard({super.key, required this.restaurant});

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 1,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: InkWell(
        onTap: () {
          _showRestaurantDetails(context);
        },
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: Theme.of(context).primaryColor.withOpacity(0.1),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Icon(
                      Icons.restaurant_menu,
                      color: Theme.of(context).primaryColor,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          restaurant.name,
                          style: const TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        if (restaurant.address != null)
                          Text(
                            restaurant.address!,
                            style: TextStyle(
                              fontSize: 12,
                              color: Colors.grey[600],
                            ),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                      ],
                    ),
                  ),
                  const Icon(Icons.chevron_right),
                ],
              ),
              // ... Giữ nguyên phần hiển thị description ...
            ],
          ),
        ),
      ),
    );
  }

  void _showRestaurantDetails(BuildContext context) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (context) => _RestaurantDetailsSheet(restaurant: restaurant),
    );
  }
}

class _RestaurantDetailsSheet extends StatelessWidget {
  final Restaurant restaurant;
  const _RestaurantDetailsSheet({required this.restaurant});

  @override
  Widget build(BuildContext context) {
    // ... Giữ nguyên UI của sheet ...
    return DraggableScrollableSheet(
      initialChildSize: 0.7,
      minChildSize: 0.5,
      maxChildSize: 0.95,
      builder: (context, scrollController) {
        return Container(
          decoration: const BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
          ),
          child: ListView(
            controller: scrollController,
            padding: const EdgeInsets.all(20),
            children: [
              Text(restaurant.name,
                  style: const TextStyle(
                      fontSize: 24, fontWeight: FontWeight.bold)),
              const SizedBox(height: 24),
              const Text('Thực đơn',
                  style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
              const SizedBox(height: 12),
              if (restaurant.foods.isNotEmpty)
                ...restaurant.foods.map((food) => _FoodItem(food: food))
              else
                const Center(child: Text('Chưa có thông tin thực đơn')),
            ],
          ),
        );
      },
    );
  }
}

class _FoodItem extends StatelessWidget {
  final Food food;
  const _FoodItem({required this.food});

  @override
  Widget build(BuildContext context) {
    // Giả sử Food model có field price (double) hoặc formattedPrice (getter)
    // Nếu model chưa có getter formattedPrice, bạn cần thêm vào hoặc xử lý ở đây.
    String priceDisplay = '${food.price} đ';

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          children: [
            // ... UI Icon ...
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(food.name,
                      style: const TextStyle(
                          fontSize: 16, fontWeight: FontWeight.w600)),
                  if (food.description != null)
                    Text(food.description!,
                        style:
                            TextStyle(fontSize: 12, color: Colors.grey[600])),
                ],
              ),
            ),
            Text(
              priceDisplay,
              style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.bold,
                  color: Theme.of(context).primaryColor),
            ),
          ],
        ),
      ),
    );
  }
}

// NOTE: Widget này yêu cầu AppProvider phải có 'languages' và 'selectedLanguage'.
// Hiện tại AppProvider chưa có, nên ta tạm thời comment hoặc để placeholder.
class LanguageSelector extends StatelessWidget {
  const LanguageSelector({super.key});

  @override
  Widget build(BuildContext context) {
    return const SizedBox.shrink();
    /* // Uncomment khi AppProvider đã update
    return Consumer<AppProvider>(
      builder: (context, provider, child) {
        return PopupMenuButton<String>(
           // ... logic cũ ...
        );
      },
    );
    */
  }
}
