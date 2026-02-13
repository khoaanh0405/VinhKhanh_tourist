import 'package:flutter/material.dart';
import 'package:google_maps_flutter/google_maps_flutter.dart';
import 'package:provider/provider.dart';
import 'package:vinh_khanh_tourism/models/models.dart';
import 'package:vinh_khanh_tourism/services/api_service.dart';
import 'package:vinh_khanh_tourism/providers/geofence_provider.dart';
import 'package:vinh_khanh_tourism/core/constants/app_constants.dart';
import 'package:vinh_khanh_tourism/screens/poi/poi_detail_screen.dart';

class MapScreen extends StatefulWidget {
  const MapScreen({super.key});

  @override
  State<MapScreen> createState() => _MapScreenState();
}

class _MapScreenState extends State<MapScreen> {
  final ApiService _apiService = ApiService();
  GoogleMapController? _mapController;
  List<POI> _pois = [];
  Set<Marker> _markers = {};
  Set<Circle> _circles = {};
  bool _isLoading = true;

  static const LatLng _vinhKhanhCenter = LatLng(10.7542, 106.6924);

  @override
  void initState() {
    super.initState();
    _loadPOIs();
  }

  Future<void> _loadPOIs() async {
    try {
      final pois = await _apiService.fetchPOIs();
      setState(() {
        _pois = pois;
        _createMarkersAndCircles();
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  void _createMarkersAndCircles() {
    _markers = _pois.map((poi) {
      return Marker(
        markerId: MarkerId('poi_${poi.poiId}'),
        position: LatLng(poi.latitude, poi.longitude),
        infoWindow: InfoWindow(
          title: poi.name,
          snippet: 'Nhấn để xem chi tiết',
        ),
        icon: BitmapDescriptor.defaultMarkerWithHue(BitmapDescriptor.hueRed),
        onTap: () => _onMarkerTapped(poi),
      );
    }).toSet();

    _circles = _pois.map((poi) {
      final radius = poi.geofence?.radius ?? AppConstants.defaultGeofenceRadius;
      return Circle(
        circleId: CircleId('circle_${poi.poiId}'),
        center: LatLng(poi.latitude, poi.longitude),
        radius: radius,
        fillColor: AppConstants.primaryColor.withOpacity(0.2),
        strokeColor: AppConstants.primaryColor,
        strokeWidth: 2,
      );
    }).toSet();
  }

  void _onMarkerTapped(POI poi) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => POIDetailScreen(poi: poi),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final geofenceProvider = context.watch<GeofenceProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('Bản đồ phố ẩm thực'),
        actions: [
          IconButton(
            icon: Icon(
              geofenceProvider.isMonitoring
                  ? Icons.location_on
                  : Icons.location_off,
              color: geofenceProvider.isMonitoring ? Colors.green : Colors.grey,
            ),
            onPressed: () {
              if (geofenceProvider.isMonitoring) {
                geofenceProvider.stopMonitoring();
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(content: Text('Đã tắt tự động thuyết minh')),
                );
              } else {
                final audioProvider = context.read();
                geofenceProvider.startMonitoring(audioProvider);
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(content: Text('Đã bật tự động thuyết minh')),
                );
              }
            },
          ),
          IconButton(
            icon: const Icon(Icons.my_location),
            onPressed: () {
              if (geofenceProvider.currentPosition != null) {
                _mapController?.animateCamera(
                  CameraUpdate.newLatLngZoom(
                    LatLng(
                      geofenceProvider.currentPosition!.latitude,
                      geofenceProvider.currentPosition!.longitude,
                    ),
                    17,
                  ),
                );
              }
            },
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : GoogleMap(
              initialCameraPosition: const CameraPosition(
                target: _vinhKhanhCenter,
                zoom: AppConstants.mapZoomLevel,
              ),
              markers: _markers,
              circles: _circles,
              myLocationEnabled: true,
              myLocationButtonEnabled: false,
              compassEnabled: true,
              mapToolbarEnabled: false,
              onMapCreated: (controller) {
                _mapController = controller;
              },
            ),
    );
  }
}
