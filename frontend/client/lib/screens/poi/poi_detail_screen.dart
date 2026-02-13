import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:vinh_khanh_tourism/models/models.dart';
import 'package:vinh_khanh_tourism/providers/audio_player_provider.dart';
import 'package:vinh_khanh_tourism/providers/language_provider.dart';
import 'package:vinh_khanh_tourism/providers/app_provider.dart';
import 'package:vinh_khanh_tourism/widgets/restaurant_details_sheet.dart';
import 'package:vinh_khanh_tourism/core/constants/app_constants.dart';

class POIDetailScreen extends StatefulWidget {
  final POI poi;

  const POIDetailScreen({super.key, required this.poi});

  @override
  State<POIDetailScreen> createState() => _POIDetailScreenState();
}

class _POIDetailScreenState extends State<POIDetailScreen> {
  @override
  void dispose() {
    // Dừng TTS khi rời màn hình
    context.read<AudioPlayerProvider>().stop();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final languageProvider = context.watch<LanguageProvider>();
    final audioProvider = context.watch<AudioPlayerProvider>();
    final appProvider = context.watch<AppProvider>();

    // ================= FIND NARRATION =================
    Narration? narration;

    for (var n in widget.poi.narrations) {
      if (n.languageCode == languageProvider.currentLanguageCode) {
        narration = n;
        break;
      }
    }

    narration ??=
        widget.poi.narrations.isNotEmpty ? widget.poi.narrations.first : null;

    return Scaffold(
      appBar: AppBar(
        title: Text(widget.poi.name),
        actions: [
          IconButton(
            icon: Icon(
              appProvider.isPOIFavorite(widget.poi.poiId)
                  ? Icons.favorite
                  : Icons.favorite_border,
              color: appProvider.isPOIFavorite(widget.poi.poiId)
                  ? Colors.red
                  : null,
            ),
            onPressed: () => appProvider.togglePOIFavorite(widget.poi),
          ),
        ],
      ),
      body: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ================= IMAGE =================
            SizedBox(
              height: 250,
              width: double.infinity,
              child: widget.poi.imageUrls.isNotEmpty
                  ? PageView.builder(
                      itemCount: widget.poi.imageUrls.length,
                      itemBuilder: (context, index) {
                        return Stack(
                          children: [
                            Positioned.fill(
                              child: Image.network(
                                widget.poi.imageUrls[index],
                                fit: BoxFit.cover,
                                errorBuilder: (_, __, ___) =>
                                    _placeholderImage(),
                              ),
                            ),
                            Positioned(
                              right: 12,
                              bottom: 12,
                              child: Container(
                                padding: const EdgeInsets.symmetric(
                                    horizontal: 8, vertical: 4),
                                decoration: BoxDecoration(
                                  color: Colors.black54,
                                  borderRadius: BorderRadius.circular(12),
                                ),
                                child: Text(
                                  '${index + 1}/${widget.poi.imageUrls.length}',
                                  style: const TextStyle(
                                      color: Colors.white, fontSize: 12),
                                ),
                              ),
                            )
                          ],
                        );
                      },
                    )
                  : _placeholderImage(),
            ),

            // ================= NARRATION =================
            Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      const Icon(Icons.info_outline),
                      const SizedBox(width: 8),
                      Text(
                        'Thuyết minh',
                        style: Theme.of(context).textTheme.titleLarge,
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  Text(
                    narration?.text ?? 'Chưa có thuyết minh cho ngôn ngữ này.',
                    style: const TextStyle(fontSize: 16, height: 1.5),
                  ),
                  const SizedBox(height: 16),

                  // ===== TTS BUTTON =====
                  if (narration != null)
                    Center(
                      child: ElevatedButton.icon(
                        onPressed: () async {
                          if (audioProvider.isSpeaking) {
                            await audioProvider.stop();
                          } else {
                            await audioProvider.speak(
                              narration!.text,
                              widget.poi.name,
                              language: _mapLanguageCode(
                                languageProvider.currentLanguageCode,
                              ),
                            );
                          }
                        },
                        icon: Icon(
                          audioProvider.isSpeaking
                              ? Icons.stop
                              : Icons.volume_up,
                        ),
                        label: Text(
                          audioProvider.isSpeaking
                              ? 'Dừng'
                              : 'Nghe thuyết minh',
                        ),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppConstants.primaryColor,
                          foregroundColor: Colors.white,
                          padding: const EdgeInsets.symmetric(
                            horizontal: 32,
                            vertical: 12,
                          ),
                        ),
                      ),
                    ),
                ],
              ),
            ),

            const Divider(height: 32),

            // ================= RESTAURANTS =================
            if (widget.poi.restaurants.isNotEmpty)
              Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        const Icon(Icons.restaurant),
                        const SizedBox(width: 8),
                        Text(
                          'Quán ăn tại đây',
                          style: Theme.of(context).textTheme.titleLarge,
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    ...widget.poi.restaurants.map((restaurant) {
                      return Card(
                        margin: const EdgeInsets.only(bottom: 12),
                        child: ListTile(
                          leading: const CircleAvatar(
                            child: Icon(Icons.restaurant_menu),
                          ),
                          title: Text(restaurant.name),
                          subtitle: Text(restaurant.address ?? ''),
                          trailing: const Icon(
                            Icons.arrow_forward_ios,
                            size: 16,
                          ),
                          onTap: () {
                            showModalBottomSheet(
                              context: context,
                              isScrollControlled: true,
                              builder: (context) => RestaurantDetailsSheet(
                                restaurant: restaurant,
                              ),
                            );
                          },
                        ),
                      );
                    }),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _placeholderImage() {
    return Container(
      color: AppConstants.accentColor,
      child: const Center(
        child: Icon(
          Icons.place,
          size: 80,
          color: AppConstants.primaryColor,
        ),
      ),
    );
  }

  String _mapLanguageCode(String code) {
    switch (code) {
      case 'vi':
        return 'vi-VN';
      case 'en':
        return 'en-US';
      case 'ja':
        return 'ja-JP';
      case 'ko':
        return 'ko-KR';
      case 'zh':
        return 'zh-CN';
      default:
        return 'vi-VN';
    }
  }
}
