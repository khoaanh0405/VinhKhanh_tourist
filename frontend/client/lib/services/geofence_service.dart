import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:vinh_khanh_tourism/models/models.dart';
import 'package:vinh_khanh_tourism/core/constants/app_constants.dart';

class GeofenceService {
  final http.Client _client = http.Client();
  String? _authToken;

  void setAuthToken(String token) {
    _authToken = token;
  }

  Map<String, String> get _headers {
    final headers = {
      'Content-Type': 'application/json',
    };
    if (_authToken != null) {
      headers['Authorization'] = 'Bearer $_authToken';
    }
    return headers;
  }

  // Helper để lấy full URL
  Uri _getUri(String endpoint) {
    return Uri.parse('${AppConstants.apiBaseUrl}$endpoint');
  }

  // Get all geofences
  Future<List<Geofence>> getAllGeofences() async {
    try {
      final response = await _client.get(
        _getUri(ApiEndpoints.geofence),
        headers: _headers,
      );

      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        return data.map((json) => Geofence.fromJson(json)).toList();
      } else {
        throw Exception('Failed to load geofences: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading geofences: $e');
    }
  }

  // Create geofence
  Future<Geofence> createGeofence(int poiId, double radius) async {
    try {
      final response = await _client.post(
        _getUri(ApiEndpoints.geofence),
        headers: _headers,
        body: jsonEncode({
          'poiId': poiId,
          'radius': radius,
        }),
      );

      if (response.statusCode == 200 || response.statusCode == 201) {
        return Geofence.fromJson(jsonDecode(response.body));
      } else {
        throw Exception('Failed to create geofence');
      }
    } catch (e) {
      throw Exception('Error creating geofence: $e');
    }
  }

  // Update geofence
  Future<void> updateGeofence(int id, int poiId, double radius) async {
    try {
      final response = await _client.put(
        Uri.parse('${AppConstants.apiBaseUrl}${ApiEndpoints.geofence}/$id'),
        headers: _headers,
        body: jsonEncode({
          'geofenceId': id,
          'poiId': poiId,
          'radius': radius,
        }),
      );

      if (response.statusCode != 200 && response.statusCode != 204) {
        throw Exception('Failed to update geofence');
      }
    } catch (e) {
      throw Exception('Error updating geofence: $e');
    }
  }

  void dispose() {
    _client.close();
  }
}
