import '_validators.dart';

/// `tier` field on `session.result` when `mode = "tier"`
/// (v2 § TierResultPayload).
class TierResultPayload {
  const TierResultPayload({
    required this.tierId,
    required this.stageIndex,
    required this.stageCount,
    required this.health,
    required this.maxHealth,
    required this.combo,
  });

  /// Host-defined tier id.
  final String tierId;

  /// 0-based stage index.
  final int stageIndex;

  /// Total stage count for UI/echo.
  final int stageCount;

  /// Ending health.
  final double health;

  /// HP cap.
  final double maxHealth;

  /// Ending cumulative combo.
  final int combo;

  factory TierResultPayload.fromJson(Map<String, dynamic> json) {
    return TierResultPayload(
      tierId: json['tierId'] as String,
      stageIndex: readRequiredInt(json, 'stageIndex', 'TierResultPayload.fromJson'),
      stageCount: readRequiredInt(json, 'stageCount', 'TierResultPayload.fromJson'),
      health: (json['health'] as num).toDouble(),
      maxHealth: (json['maxHealth'] as num).toDouble(),
      combo: readRequiredInt(json, 'combo', 'TierResultPayload.fromJson'),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'tierId': tierId,
      'stageIndex': stageIndex,
      'stageCount': stageCount,
      'health': health,
      'maxHealth': maxHealth,
      'combo': combo,
    };
  }
}
