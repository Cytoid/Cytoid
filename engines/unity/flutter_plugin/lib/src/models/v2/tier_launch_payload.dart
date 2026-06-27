/// `session.start.tier` block (v2 § TierLaunchPayload). Required when
/// `mode = "tier"`.
class TierLaunchPayload {
  const TierLaunchPayload({
    required this.tierId,
    required this.stageIndex,
    required this.stageCount,
    required this.maxHealth,
    required this.initialHealth,
    required this.initialCombo,
    this.introLabel,
  });

  final String tierId;
  final int stageIndex;
  final int stageCount;
  final double maxHealth;
  final double initialHealth;
  final int initialCombo;
  final String? introLabel;

  factory TierLaunchPayload.fromJson(Map<String, dynamic> json) {
    return TierLaunchPayload(
      tierId: json['tierId'] as String,
      stageIndex: (json['stageIndex'] as num).toInt(),
      stageCount: (json['stageCount'] as num).toInt(),
      maxHealth: (json['maxHealth'] as num).toDouble(),
      initialHealth: (json['initialHealth'] as num).toDouble(),
      initialCombo: (json['initialCombo'] as num).toInt(),
      introLabel: json['introLabel'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'tierId': tierId,
      'stageIndex': stageIndex,
      'stageCount': stageCount,
      'maxHealth': maxHealth,
      'initialHealth': initialHealth,
      'initialCombo': initialCombo,
      if (introLabel != null) 'introLabel': introLabel,
    };
  }
}
