import 'package:flutter/material.dart';
import 'package:flutter_tts/flutter_tts.dart';

class AudioPlayerProvider extends ChangeNotifier {
  final FlutterTts _tts = FlutterTts();

  bool _isSpeaking = false;
  String? _currentText;
  String? _currentTitle;

  bool get isSpeaking => _isSpeaking;
  String? get currentTrackTitle => _currentTitle;

  AudioPlayerProvider() {
    _initializeTTS();
  }

  void _initializeTTS() {
    _tts.setCompletionHandler(() {
      _isSpeaking = false;
      notifyListeners();
    });
  }

  Future<void> speak(String text, String title,
      {String language = "vi-VN"}) async {
    try {
      await stop();

      _currentText = text;
      _currentTitle = title;

      await _tts.setLanguage(language);
      await _tts.setSpeechRate(0.5);
      await _tts.setVolume(1.0);
      await _tts.setPitch(1.0);

      await _tts.speak(text);

      _isSpeaking = true;
      notifyListeners();
    } catch (e) {
      debugPrint("TTS Error: $e");
    }
  }

  Future<void> stop() async {
    await _tts.stop();
    _isSpeaking = false;
    notifyListeners();
  }

  @override
  void dispose() {
    _tts.stop();
    super.dispose();
  }
}
