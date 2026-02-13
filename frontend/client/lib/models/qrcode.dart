class QRCode {
  final int qrCodeId;
  final String codeValue;
  final int? poiId;

  QRCode({
    required this.qrCodeId,
    required this.codeValue,
    this.poiId,
  });

  factory QRCode.fromJson(Map<String, dynamic> json) {
    return QRCode(
      qrCodeId: json['qrCodeId'],
      codeValue: json['codeValue'],
      poiId: json['poiId'],
    );
  }
}
