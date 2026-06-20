/// Initial tier stage state supplied by the Flutter host for a single play session.
class TierPlayLaunch {
  const TierPlayLaunch({
    required this.stageIndex,
    required this.maxHealth,
    this.tierId,
    this.stageCount,
    this.initialHealth,
    this.initialCombo,
    this.introLabel,
  });

  final String? tierId;
  final int stageIndex;
  final int? stageCount;
  final double maxHealth;
  final double? initialHealth;
  final int? initialCombo;
  final String? introLabel;

  factory TierPlayLaunch.fromJson(Map<String, dynamic> json) {
    return TierPlayLaunch(
      tierId: json['tierId'] as String?,
      stageIndex: (json['stageIndex'] as num).toInt(),
      stageCount: _readOptionalInt(json['stageCount']),
      maxHealth: (json['maxHealth'] as num).toDouble(),
      initialHealth: _readOptionalDouble(json['initialHealth']),
      initialCombo: _readOptionalInt(json['initialCombo']),
      introLabel: json['introLabel'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      if (tierId != null) 'tierId': tierId,
      'stageIndex': stageIndex,
      if (stageCount != null) 'stageCount': stageCount,
      'maxHealth': maxHealth,
      if (initialHealth != null) 'initialHealth': initialHealth,
      if (initialCombo != null) 'initialCombo': initialCombo,
      if (introLabel != null) 'introLabel': introLabel,
    };
  }

  static int? _readOptionalInt(Object? value) {
    if (value == null) return null;
    if (value is num) return value.toInt();
    throw FormatException('Expected integer.');
  }

  static double? _readOptionalDouble(Object? value) {
    if (value == null) return null;
    if (value is num) return value.toDouble();
    throw FormatException('Expected number.');
  }
}
