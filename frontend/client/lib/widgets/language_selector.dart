import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:vinh_khanh_tourism/providers/language_provider.dart';
import 'package:vinh_khanh_tourism/core/constants/app_constants.dart';

class LanguageSelector extends StatelessWidget {
  const LanguageSelector({super.key});

  @override
  Widget build(BuildContext context) {
    final languageProvider = context.watch<LanguageProvider>();

    return PopupMenuButton<String>(
      icon: const Icon(Icons.language),
      onSelected: (languageCode) {
        languageProvider.changeLanguage(languageCode);
      },
      itemBuilder: (context) {
        if (languageProvider.availableLanguages.isEmpty) {
          return [
            const PopupMenuItem(
              enabled: false,
              child: Text('Đang tải...'),
            ),
          ];
        }

        return languageProvider.availableLanguages.map((language) {
          final isSelected =
              language.languageCode == languageProvider.currentLanguageCode;

          return PopupMenuItem<String>(
            value: language.languageCode,
            child: Row(
              children: [
                Container(
                  width: 32,
                  height: 32,
                  decoration: BoxDecoration(
                    color: isSelected
                        ? AppConstants.primaryColor.withOpacity(0.1)
                        : Colors.grey[200],
                    shape: BoxShape.circle,
                  ),
                  child: Center(
                    child: Text(
                      _getFlagEmoji(language.languageCode),
                      style: const TextStyle(fontSize: 18),
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    language.languageName,
                    style: TextStyle(
                      fontWeight:
                          isSelected ? FontWeight.bold : FontWeight.normal,
                      color: isSelected ? AppConstants.primaryColor : null,
                    ),
                  ),
                ),
                if (isSelected)
                  const Icon(
                    Icons.check,
                    color: AppConstants.primaryColor,
                    size: 20,
                  ),
              ],
            ),
          );
        }).toList();
      },
    );
  }

  String _getFlagEmoji(String languageCode) {
    switch (languageCode) {
      case 'vi':
        return '🇻🇳';
      case 'en':
        return '🇺🇸';
      case 'zh':
        return '🇨🇳';
      case 'ja':
        return '🇯🇵';
      case 'ko':
        return '🇰🇷';
      default:
        return '🌐';
    }
  }
}
