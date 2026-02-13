import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:vinh_khanh_tourism/models/models.dart';
import 'package:vinh_khanh_tourism/screens/poi/poi_detail_screen.dart';
import 'package:vinh_khanh_tourism/providers/app_provider.dart';
import 'package:vinh_khanh_tourism/providers/geofence_provider.dart';
import 'package:vinh_khanh_tourism/core/constants/app_constants.dart';

class POICard extends StatelessWidget {
  final POI poi;

  const POICard({super.key, required this.poi});

  @override
  Widget build(BuildContext context) {
    final appProvider = context.watch<AppProvider>();
    final geofenceProvider = context.watch<GeofenceProvider>();
    final distance = geofenceProvider.getDistanceToPOI(poi);

    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () {
          Navigator.push(
            context,
            MaterialPageRoute(
              builder: (context) => POIDetailScreen(poi: poi),
            ),
          );
        },
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ================= IMAGE =================
            SizedBox(
              height: 160,
              width: double.infinity,
              child: ClipRRect(
                borderRadius: const BorderRadius.vertical(
                  top: Radius.circular(12),
                ),
                child: poi.imageUrls.isNotEmpty
                    ? Image.network(
                        poi.imageUrls.first,
                        fit: BoxFit.cover,
                        errorBuilder: (_, __, ___) => _placeholderImage(),
                      )
                    : _placeholderImage(),
              ),
            ),

            // ================= CONTENT =================
            Padding(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // ===== TITLE + FAVORITE =====
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          poi.name,
                          style: const TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                      IconButton(
                        icon: Icon(
                          appProvider.isPOIFavorite(poi.poiId)
                              ? Icons.favorite
                              : Icons.favorite_border,
                          color: appProvider.isPOIFavorite(poi.poiId)
                              ? Colors.red
                              : Colors.grey,
                        ),
                        onPressed: () => appProvider.togglePOIFavorite(poi),
                      ),
                    ],
                  ),

                  const SizedBox(height: 4),

                  // ===== LOCATION =====
                  Row(
                    children: [
                      const Icon(Icons.location_on,
                          size: 16, color: Colors.grey),
                      const SizedBox(width: 4),
                      Text(
                        '${poi.latitude.toStringAsFixed(4)}, ${poi.longitude.toStringAsFixed(4)}',
                        style: const TextStyle(
                          color: Colors.grey,
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ),

                  // ===== DISTANCE =====
                  if (distance != null) ...[
                    const SizedBox(height: 4),
                    Row(
                      children: [
                        const Icon(Icons.directions_walk,
                            size: 16, color: Colors.blue),
                        const SizedBox(width: 4),
                        Text(
                          distance < 1000
                              ? '${distance.toStringAsFixed(0)}m'
                              : '${(distance / 1000).toStringAsFixed(1)}km',
                          style: const TextStyle(
                            color: Colors.blue,
                            fontSize: 12,
                          ),
                        ),
                      ],
                    ),
                  ],

                  const SizedBox(height: 8),

                  // ===== TAGS =====
                  Wrap(
                    spacing: 8,
                    children: [
                      if (poi.narrations.isNotEmpty)
                        Chip(
                          label: Text('${poi.narrations.length} thuyết minh'),
                          avatar: const Icon(Icons.volume_up, size: 16),
                          materialTapTargetSize:
                              MaterialTapTargetSize.shrinkWrap,
                        ),
                      if (poi.restaurants.isNotEmpty)
                        Chip(
                          label: Text('${poi.restaurants.length} quán ăn'),
                          avatar: const Icon(Icons.restaurant, size: 16),
                          materialTapTargetSize:
                              MaterialTapTargetSize.shrinkWrap,
                        ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  // ================= PLACEHOLDER =================
  Widget _placeholderImage() {
    return Container(
      color: AppConstants.accentColor,
      child: const Center(
        child: Icon(
          Icons.place,
          size: 64,
          color: AppConstants.primaryColor,
        ),
      ),
    );
  }
}
