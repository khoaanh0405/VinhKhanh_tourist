import 'narration.dart';
import 'restaurant.dart';
import 'geofence.dart';

class POI {
  final int poiId;
  final String name;
  final String? imageUrl;
  final double latitude;
  final double longitude;
  final List<String> imageUrls;
  final List<Narration> narrations;
  final List<Restaurant> restaurants;
  final Geofence? geofence;

  POI({
    required this.poiId,
    required this.name,
    this.imageUrl,
    required this.latitude,
    required this.longitude,
    required this.imageUrls,
    this.narrations = const [],
    this.restaurants = const [],
    this.geofence,
  });

  factory POI.fromJson(Map<String, dynamic> json) {
    return POI(
      poiId: json['poiId'],
      name: json['name'],
      imageUrl: json['imageUrl'], // ✅ THÊM
      latitude: (json['latitude'] as num).toDouble(),
      longitude: (json['longitude'] as num).toDouble(),
      imageUrls: List<String>.from(json['imageUrls'] ?? []),
      narrations: (json['narrations'] as List<dynamic>?)
              ?.map((n) => Narration.fromJson(n))
              .toList() ??
          [],
      restaurants: (json['restaurants'] as List<dynamic>?)
              ?.map((r) => Restaurant.fromJson(r))
              .toList() ??
          [],
      geofence:
          json['geofence'] != null ? Geofence.fromJson(json['geofence']) : null,
    );
  }
}
