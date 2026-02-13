import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import 'package:provider/provider.dart';
import 'package:vinh_khanh_tourism/providers/geofence_provider.dart';
import 'package:vinh_khanh_tourism/providers/app_provider.dart';
import 'package:vinh_khanh_tourism/providers/audio_player_provider.dart';
// Import model POI

class GeofenceNotification extends StatelessWidget {
  const GeofenceNotification({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer3<GeofenceProvider, AppProvider, AudioPlayerProvider>(
      builder: (context, geoProvider, appProvider, audioProvider, child) {
        if (!geoProvider.isMonitoring) {
          return const SizedBox.shrink();
        }

        // Logic sửa đổi: Dựa vào currentActivePOI mà Provider đã xử lý
        final activePOI = geoProvider.currentActivePOI;
        final currentPos = geoProvider.currentPosition;

        // Nếu không có POI nào đang active hoặc chưa có vị trí -> ẩn
        if (activePOI == null || currentPos == null) {
          return const SizedBox.shrink();
        }

        // Tính khoảng cách để hiển thị lên UI
        final distance = Geolocator.distanceBetween(
          currentPos.latitude,
          currentPos.longitude,
          activePOI.latitude,
          activePOI.longitude,
        );

        return Container(
          margin: const EdgeInsets.all(16),
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            gradient: LinearGradient(
              colors: [
                Theme.of(context).primaryColor,
                Theme.of(context).primaryColor.withOpacity(0.8),
              ],
            ),
            borderRadius: BorderRadius.circular(16),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withOpacity(0.2),
                blurRadius: 10,
                offset: const Offset(0, 4),
              ),
            ],
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: Colors.white.withOpacity(0.2),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: const Icon(
                      Icons.location_on,
                      color: Colors.white,
                      size: 24,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Bạn đang ở gần',
                          style: TextStyle(
                            color: Colors.white70,
                            fontSize: 12,
                          ),
                        ),
                        Text(
                          activePOI.name,
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close, color: Colors.white),
                    onPressed: () {
                      // Logic để tắt thông báo tạm thời (cần thêm vào provider nếu muốn)
                      // Hiện tại có thể dùng stopMonitoring hoặc bỏ qua
                    },
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Text(
                'Khoảng cách: ${distance.toInt()} mét',
                style: const TextStyle(
                  color: Colors.white70,
                  fontSize: 14,
                ),
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: ElevatedButton.icon(
                      onPressed: () {
                        // Logic phát âm thanh: AppProvider hiện tại chưa có selectedLanguage
                        // Nên ta sẽ lấy mặc định hoặc cần thêm field đó vào AppProvider
                        if (activePOI.narrations.isNotEmpty) {
                          final narration = activePOI.narrations.first;

                          audioProvider.speak(
                            narration.text, // text từ database
                            activePOI.name,
                          );
                        }
                      },
                      icon: const Icon(Icons.play_arrow),
                      label: const Text('Nghe thuyết minh'),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: Colors.white,
                        foregroundColor: Theme.of(context).primaryColor,
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  ElevatedButton(
                    onPressed: () {
                      Navigator.pushNamed(
                        context,
                        '/poi-detail',
                        arguments: activePOI,
                      );
                    },
                    style: ElevatedButton.styleFrom(
                      backgroundColor: Colors.white.withOpacity(0.2),
                      foregroundColor: Colors.white,
                    ),
                    child: const Text('Xem'),
                  ),
                ],
              ),
            ],
          ),
        );
      },
    );
  }
}
