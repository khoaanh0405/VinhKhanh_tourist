class Geofence {
  final int geofenceId;
  final int poiId;
  final double latitude;
  final double longitude;
  final double radius;

  Geofence({
    required this.geofenceId,
    required this.poiId,
    required this.latitude,
    required this.longitude,
    required this.radius,
  });

  factory Geofence.fromJson(Map<String, dynamic> json) {
    return Geofence(
      geofenceId: json['geofenceId'],
      poiId: json['poiId'],
      latitude: (json['latitude'] as num).toDouble(),
      longitude: (json['longitude'] as num).toDouble(),
      radius: (json['radius'] as num).toDouble(),
    );
  }
}
