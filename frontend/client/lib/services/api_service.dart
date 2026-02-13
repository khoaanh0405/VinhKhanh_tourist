import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:vinh_khanh_tourism/models/models.dart';
import 'package:vinh_khanh_tourism/core/constants/app_constants.dart';

class ApiService {
  final String baseUrl = AppConstants.apiBaseUrl;

  Future<List<POI>> fetchPOIs() async {
    final response = await http.get(Uri.parse('$baseUrl${ApiEndpoints.pois}'));
    if (response.statusCode == 200) {
      final List data = json.decode(utf8.decode(response.bodyBytes));
      return data.map((json) => POI.fromJson(json)).toList();
    }
    throw Exception('Failed to load POIs');
  }

  Future<POI> fetchPOIById(int id, {String? languageCode}) async {
    String url = '$baseUrl${ApiEndpoints.pois}/$id';
    if (languageCode != null) {
      url += '/language/$languageCode';
    }

    final response = await http.get(Uri.parse(url));
    if (response.statusCode == 200) {
      return POI.fromJson(json.decode(utf8.decode(response.bodyBytes)));
    }
    throw Exception('Failed to load POI');
  }

  Future<List<POI>> fetchNearbyPOIs(double lat, double lon,
      {double radius = 1.0}) async {
    final url =
        '$baseUrl${ApiEndpoints.pois}/nearby?latitude=$lat&longitude=$lon&radiusKm=$radius';
    final response = await http.get(Uri.parse(url));
    if (response.statusCode == 200) {
      final List data = json.decode(utf8.decode(response.bodyBytes));
      return data.map((json) => POI.fromJson(json)).toList();
    }
    throw Exception('Failed to load nearby POIs');
  }

  Future<List<Restaurant>> fetchRestaurants() async {
    final response =
        await http.get(Uri.parse('$baseUrl${ApiEndpoints.restaurants}'));
    if (response.statusCode == 200) {
      final List data = json.decode(utf8.decode(response.bodyBytes));
      return data.map((json) => Restaurant.fromJson(json)).toList();
    }
    throw Exception('Failed to load restaurants');
  }

  Future<Restaurant> fetchRestaurantById(int id) async {
    final response =
        await http.get(Uri.parse('$baseUrl${ApiEndpoints.restaurants}/$id'));
    if (response.statusCode == 200) {
      return Restaurant.fromJson(json.decode(utf8.decode(response.bodyBytes)));
    }
    throw Exception('Failed to load restaurant');
  }

  Future<List<Food>> fetchFoodsByRestaurant(int restaurantId) async {
    final response = await http.get(
      Uri.parse('$baseUrl${ApiEndpoints.foods}/ByRestaurant/$restaurantId'),
    );
    if (response.statusCode == 200) {
      final List data = json.decode(utf8.decode(response.bodyBytes));
      return data.map((json) => Food.fromJson(json)).toList();
    }
    throw Exception('Failed to load foods');
  }

  Future<List<Language>> fetchLanguages() async {
    final response =
        await http.get(Uri.parse('$baseUrl${ApiEndpoints.languages}'));
    if (response.statusCode == 200) {
      final List data = json.decode(utf8.decode(response.bodyBytes));
      return data.map((json) => Language.fromJson(json)).toList();
    }
    throw Exception('Failed to load languages');
  }

  Future<List<Narration>> fetchNarrationsByPOI(int poiId) async {
    final response = await http.get(
      Uri.parse('$baseUrl${ApiEndpoints.narrations}/poi/$poiId'),
    );
    if (response.statusCode == 200) {
      final List data = json.decode(utf8.decode(response.bodyBytes));
      return data.map((json) => Narration.fromJson(json)).toList();
    }
    throw Exception('Failed to load narrations');
  }

  Future<Narration> fetchNarrationByPOIAndLanguage(
      int poiId, String languageCode) async {
    final response = await http.get(
      Uri.parse(
          '$baseUrl${ApiEndpoints.narrations}/poi/$poiId/language/$languageCode'),
    );
    if (response.statusCode == 200) {
      return Narration.fromJson(json.decode(utf8.decode(response.bodyBytes)));
    }
    throw Exception('Failed to load narration');
  }

  Future<QRCode> fetchQRCode(int id) async {
    final response =
        await http.get(Uri.parse('$baseUrl${ApiEndpoints.qrCodes}/$id'));
    if (response.statusCode == 200) {
      return QRCode.fromJson(json.decode(utf8.decode(response.bodyBytes)));
    }
    throw Exception('Failed to load QR code');
  }

  Future<List<Geofence>> fetchGeofences() async {
    final response =
        await http.get(Uri.parse('$baseUrl${ApiEndpoints.geofence}'));
    if (response.statusCode == 200) {
      final List data = json.decode(utf8.decode(response.bodyBytes));
      return data.map((json) => Geofence.fromJson(json)).toList();
    }
    throw Exception('Failed to load geofences');
  }
}
