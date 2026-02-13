import 'audio_file.dart';

class Narration {
  final int narrationId;
  final String text;
  final String languageCode;
  final String? languageName;
  final bool isTTSGenerated;
  final List<AudioFile> audioFiles;

  Narration({
    required this.narrationId,
    required this.text,
    required this.languageCode,
    this.languageName,
    this.isTTSGenerated = false,
    this.audioFiles = const [],
  });

  factory Narration.fromJson(Map<String, dynamic> json) {
    return Narration(
      narrationId: json['narrationId'],
      text: json['text'] ?? '',
      languageCode: json['languageCode'],
      languageName: json['languageName'],
      isTTSGenerated: json['isTTSGenerated'] ?? false,
      audioFiles: (json['audioFiles'] as List<dynamic>?)
              ?.map((a) => AudioFile.fromJson(a))
              .toList() ??
          [],
    );
  }
}
