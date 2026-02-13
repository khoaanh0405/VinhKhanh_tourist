import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:vinh_khanh_tourism/providers/audio_player_provider.dart';
import 'package:vinh_khanh_tourism/core/constants/app_constants.dart';

class AudioPlayerWidget extends StatelessWidget {
  const AudioPlayerWidget({super.key});

  @override
  Widget build(BuildContext context) {
    final audioProvider = context.watch<AudioPlayerProvider>();

    return Container(
      height: MediaQuery.of(context).size.height * 0.5,
      decoration: const BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              width: 180,
              height: 180,
              decoration: const BoxDecoration(
                color: AppConstants.accentColor,
                shape: BoxShape.circle,
              ),
              child: const Icon(
                Icons.volume_up,
                size: 80,
                color: AppConstants.primaryColor,
              ),
            ),
            const SizedBox(height: 32),
            Text(
              audioProvider.currentTrackTitle ?? 'Đang thuyết minh',
              style: const TextStyle(
                fontSize: 22,
                fontWeight: FontWeight.bold,
              ),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 32),
            ElevatedButton.icon(
              style: ElevatedButton.styleFrom(
                backgroundColor: AppConstants.primaryColor,
                padding:
                    const EdgeInsets.symmetric(horizontal: 32, vertical: 12),
              ),
              icon: const Icon(Icons.stop),
              label: const Text('Dừng thuyết minh'),
              onPressed: () {
                audioProvider.stop();
                Navigator.pop(context);
              },
            ),
          ],
        ),
      ),
    );
  }
}
