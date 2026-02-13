import 'package:flutter/material.dart';

class AppConstants {
  static const String appName = 'Vĩnh Khánh Food Street';
  static const String apiBaseUrl = 'http://10.0.2.2:5280/api';

  static const Color primaryColor = Color(0xFFE63946);
  static const Color secondaryColor = Color(0xFFF1FAEE);
  static const Color accentColor = Color(0xFFA8DADC);
  static const Color textDark = Color(0xFF1D3557);
  static const Color backgroundLight = Color(0xFFFDFDFD);

  static const double defaultGeofenceRadius = 50.0;
  static const double mapZoomLevel = 16.0;

  static const Duration autoPlayDelay = Duration(seconds: 2);
  static const Duration geofenceCheckInterval = Duration(seconds: 5);
}

class ApiEndpoints {
  static const String pois = '/POIs';
  static const String restaurants = '/Restaurant';
  static const String foods = '/Food';
  static const String narrations = '/Narrations';
  static const String languages = '/Language';
  static const String qrCodes = '/QRCode';
  static const String tts = '/TTS';
  static const String geofence = '/Geofence';
  static const String audio = '/AudioFile';
}
