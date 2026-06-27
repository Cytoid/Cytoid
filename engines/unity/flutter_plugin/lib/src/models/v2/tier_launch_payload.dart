import '_validators.dart';

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
      stageIndex: readRequiredInt(json, 'stageIndex', 'TierLaunchPayload'),
      stageCount: readRequiredInt(json, 'stageCount', 'TierLaunchPayload'),
      maxHealth: (json['maxHealth'] as num).toDouble(),
      initialHealth: (json['initialHealth'] as num).toDouble(),
      initialCombo: readRequiredInt(json, 'initialCombo', 'TierLaunchPayload'),
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
