import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:vinh_khanh_tourism/models/models.dart';
import 'package:vinh_khanh_tourism/services/api_service.dart';
import 'package:vinh_khanh_tourism/widgets/poi_card.dart';
import 'package:vinh_khanh_tourism/widgets/restaurant_card.dart';
import 'package:vinh_khanh_tourism/widgets/language_selector.dart';
import 'package:vinh_khanh_tourism/providers/geofence_provider.dart';
import 'package:vinh_khanh_tourism/core/constants/app_constants.dart';
import 'package:vinh_khanh_tourism/providers/audio_player_provider.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen>
    with SingleTickerProviderStateMixin {
  final ApiService _apiService = ApiService();
  late TabController _tabController;

  List<POI> _pois = [];
  List<Restaurant> _restaurants = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();

    _tabController = TabController(length: 2, vsync: this);

    _loadData();

    WidgetsBinding.instance.addPostFrameCallback((_) {
      final geo = context.read<GeofenceProvider>();
      final audio = context.read<AudioPlayerProvider>();

      geo.initialize().then((_) {
        geo.startMonitoring(audio);
      });
    });
  }

  Future<void> _loadData() async {
    setState(() => _isLoading = true);

    try {
      final pois = await _apiService.fetchPOIs();

      // Gom tất cả restaurant từ POI
      final List<Restaurant> allRestaurants = [];
      for (var p in pois) {
        allRestaurants.addAll(p.restaurants);
      }

      setState(() {
        _pois = pois;
        _restaurants = allRestaurants;
        _isLoading = false;
      });

      // Debug
      print("POIs: ${_pois.length}");
      print("Restaurants: ${_restaurants.length}");
    } catch (e) {
      setState(() => _isLoading = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi tải dữ liệu: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final geofenceProvider = context.watch<GeofenceProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text(
          AppConstants.appName,
          style: TextStyle(fontWeight: FontWeight.bold),
        ),
        actions: const [
          LanguageSelector(),
          SizedBox(width: 8),
        ],
        bottom: TabBar(
          controller: _tabController,
          tabs: const [
            Tab(text: 'Điểm tham quan', icon: Icon(Icons.place)),
            Tab(text: 'Quán ăn', icon: Icon(Icons.restaurant)),
          ],
        ),
      ),
      body: Column(
        children: [
          if (geofenceProvider.currentActivePOI != null)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              color: AppConstants.accentColor,
              child: Row(
                children: [
                  const Icon(Icons.volume_up, color: AppConstants.textDark),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      'Đang phát: ${geofenceProvider.currentActivePOI!.name}',
                      style: const TextStyle(
                        fontWeight: FontWeight.bold,
                        color: AppConstants.textDark,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          Expanded(
            child: _isLoading
                ? const Center(child: CircularProgressIndicator())
                : TabBarView(
                    controller: _tabController,
                    children: [
                      // ===== TAB POI =====
                      RefreshIndicator(
                        onRefresh: _loadData,
                        child: _pois.isEmpty
                            ? const Center(child: Text('Chưa có dữ liệu'))
                            : ListView.builder(
                                padding: const EdgeInsets.all(16),
                                itemCount: _pois.length,
                                itemBuilder: (context, index) {
                                  return POICard(poi: _pois[index]);
                                },
                              ),
                      ),

                      // ===== TAB RESTAURANT =====
                      RefreshIndicator(
                        onRefresh: _loadData,
                        child: _restaurants.isEmpty
                            ? const Center(child: Text('Chưa có dữ liệu'))
                            : ListView.builder(
                                padding: const EdgeInsets.all(16),
                                itemCount: _restaurants.length,
                                itemBuilder: (context, index) {
                                  return RestaurantCard(
                                    restaurant: _restaurants[index],
                                  );
                                },
                              ),
                      ),
                    ],
                  ),
          ),
        ],
      ),
    );
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }
}
