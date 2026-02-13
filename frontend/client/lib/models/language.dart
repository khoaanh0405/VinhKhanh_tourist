class Language {
  final String languageCode;
  final String languageName;

  Language({
    required this.languageCode,
    required this.languageName,
  });

  factory Language.fromJson(Map<String, dynamic> json) {
    return Language(
      languageCode: json['languageCode'],
      languageName: json['languageName'],
    );
  }
}
