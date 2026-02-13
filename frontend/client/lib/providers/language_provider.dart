import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:vinh_khanh_tourism/models/models.dart';
import 'package:vinh_khanh_tourism/services/api_service.dart';

class LanguageProvider extends ChangeNotifier {
  final ApiService _apiService = ApiService();

  Locale _currentLocale = const Locale('vi', 'VN');
  List<Language> _availableLanguages = [];
  bool _isLoading = false;

  Locale get currentLocale => _currentLocale;
  List<Language> get availableLanguages => _availableLanguages;
  bool get isLoading => _isLoading;

  String get currentLanguageCode => _currentLocale.languageCode;

  LanguageProvider() {
    _loadSavedLanguage();
    fetchLanguages();
  }

  Future<void> _loadSavedLanguage() async {
    final prefs = await SharedPreferences.getInstance();
    final savedCode = prefs.getString('language_code') ?? 'vi';
    final savedCountry = prefs.getString('language_country') ?? 'VN';
    _currentLocale = Locale(savedCode, savedCountry);
    notifyListeners();
  }

  Future<void> fetchLanguages() async {
    _isLoading = true;
    notifyListeners();

    try {
      _availableLanguages = await _apiService.fetchLanguages();
    } catch (e) {
      debugPrint('Error fetching languages: $e');
      _availableLanguages = _getDefaultLanguages();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  List<Language> _getDefaultLanguages() {
    return [
      Language(languageCode: 'vi', languageName: 'Tiếng Việt'),
      Language(languageCode: 'en', languageName: 'English'),
      Language(languageCode: 'zh', languageName: '中文'),
      Language(languageCode: 'ja', languageName: '日本語'),
      Language(languageCode: 'ko', languageName: '한국어'),
    ];
  }

  Future<void> changeLanguage(String languageCode) async {
    String countryCode;
    switch (languageCode) {
      case 'vi':
        countryCode = 'VN';
        break;
      case 'en':
        countryCode = 'US';
        break;
      case 'zh':
        countryCode = 'CN';
        break;
      case 'ja':
        countryCode = 'JP';
        break;
      case 'ko':
        countryCode = 'KR';
        break;
      default:
        countryCode = 'VN';
    }

    _currentLocale = Locale(languageCode, countryCode);

    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('language_code', languageCode);
    await prefs.setString('language_country', countryCode);

    notifyListeners();
  }

  String getLanguageName(String code) {
    final language = _availableLanguages.firstWhere(
      (lang) => lang.languageCode == code,
      orElse: () =>
          Language(languageCode: code, languageName: code.toUpperCase()),
    );
    return language.languageName;
  }
}
