class AudioFile {
  final int audioId;
  final String url;
  final int? duration;
  final String format;

  AudioFile({
    required this.audioId,
    required this.url,
    this.duration,
    required this.format,
  });

  factory AudioFile.fromJson(Map<String, dynamic> json) {
    return AudioFile(
      audioId: json['audioId'],
      url: json['url'],
      duration: json['duration'],
      format: json['format'] ?? 'mp3',
    );
  }
}
