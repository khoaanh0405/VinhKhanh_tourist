import 'dart:async';
import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import 'package:vinh_khanh_tourism/models/models.dart';
import 'package:vinh_khanh_tourism/services/api_service.dart';
import 'audio_player_provider.dart';

class GeofenceProvider extends ChangeNotifier {
  final ApiService _apiService = ApiService();

  Position? _currentPosition;
  List<POI> _allPOIs = [];

  final Set<int> _triggeredPOIs = {};
  StreamSubscription<Position>? _positionSubscription;

  bool _isMonitoring = false;
  POI? _currentActivePOI;

  // 🔥 FIX TTS SPAM
  int? _currentlySpeakingPoiId;
  bool _isSpeaking = false;

  Position? get currentPosition => _currentPosition;
  bool get isMonitoring => _isMonitoring;
  POI? get currentActivePOI => _currentActivePOI;

  // ================= INITIALIZE =================
  Future<void> initialize() async {
    await _requestLocationPermission();
    await _fetchAllPOIs();
  }

  // ================= PERMISSION =================
  Future<void> _requestLocationPermission() async {
    if (!await Geolocator.isLocationServiceEnabled()) return;

    LocationPermission permission = await Geolocator.checkPermission();

    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }

    if (permission == LocationPermission.deniedForever) {
      await Geolocator.openAppSettings();
    }
  }

  // ================= FETCH POI =================
  Future<void> _fetchAllPOIs() async {
    _allPOIs = await _apiService.fetchPOIs();
    debugPrint("Loaded ${_allPOIs.length} POIs");
  }

  // ================= START =================
  Future<void> startMonitoring(AudioPlayerProvider audioProvider) async {
    if (_isMonitoring) return;

    if (_allPOIs.isEmpty) {
      await _fetchAllPOIs();
    }

    _isMonitoring = true;

    const locationSettings = LocationSettings(
      accuracy: LocationAccuracy.high,
      distanceFilter: 3,
    );

    _positionSubscription =
        Geolocator.getPositionStream(locationSettings: locationSettings)
            .listen((position) {
      _currentPosition = position;
      _checkGeofences(audioProvider);
    });

    debugPrint("Geofence monitoring started");
  }

  // ================= STOP =================
  void stopMonitoring() {
    _positionSubscription?.cancel();
    _positionSubscription = null;

    _triggeredPOIs.clear();
    _currentActivePOI = null;
    _currentlySpeakingPoiId = null;
    _isMonitoring = false;

    notifyListeners();
  }

  // ================= CHECK =================
  void _checkGeofences(AudioPlayerProvider audioProvider) {
    if (_currentPosition == null) return;

    for (var poi in _allPOIs) {
      final distance = Geolocator.distanceBetween(
        _currentPosition!.latitude,
        _currentPosition!.longitude,
        poi.latitude,
        poi.longitude,
      );

      final radius = poi.geofence?.radius ?? 30.0;
      final exitRadius = radius + 10;

      // ===== ENTER =====
      if (distance <= radius && !_triggeredPOIs.contains(poi.poiId)) {
        _triggerPOI(poi, audioProvider);
      }

      // ===== EXIT =====
      if (distance > exitRadius && _triggeredPOIs.contains(poi.poiId)) {
        _triggeredPOIs.remove(poi.poiId);

        if (_currentActivePOI?.poiId == poi.poiId) {
          _currentActivePOI = null;
          _currentlySpeakingPoiId = null;
        }
      }
    }
  }

  // ================= TRIGGER =================
  Future<void> _triggerPOI(POI poi, AudioPlayerProvider audioProvider) async {
    // 🔥 Không đọc lại cùng POI
    if (_currentlySpeakingPoiId == poi.poiId) return;

    _triggeredPOIs.add(poi.poiId);
    _currentActivePOI = poi;
    _currentlySpeakingPoiId = poi.poiId;

    notifyListeners();

    await Future.delayed(const Duration(seconds: 1));

    if (poi.narrations.isNotEmpty && !_isSpeaking) {
      _isSpeaking = true;

      final narration = poi.narrations.first;

      debugPrint("Speaking POI: ${poi.name}");
      debugPrint("Text: ${narration.text}");

      await audioProvider.speak(
        narration.text,
        poi.name,
      );

      _isSpeaking = false;
    }

    debugPrint("Triggered POI: ${poi.name}");
  }

  // ================= DISTANCE =================
  double? getDistanceToPOI(POI poi) {
    if (_currentPosition == null) return null;

    return Geolocator.distanceBetween(
      _currentPosition!.latitude,
      _currentPosition!.longitude,
      poi.latitude,
      poi.longitude,
    );
  }

  @override
  void dispose() {
    _positionSubscription?.cancel();
    super.dispose();
  }
}
