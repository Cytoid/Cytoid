import '_validators.dart';

/// `tier` field on `session.result` when `mode = "tier"`
/// (v2 § TierResultPayload).
class TierResultPayload {
  const TierResultPayload({
    required this.tierId,
    required this.stageIndex,
    required this.maxHealth,
    required this.combo,
    this.health,
    this.stageCount,
  });

  /// Host-defined tier id.
  final String tierId;

  /// 0-based stage index.
  final int stageIndex;

  /// Total stage count for UI/echo (may be null on mid-stage retry).
  final int? stageCount;

  /// Ending health (may be null on mid-stage retry).
  final double? health;

  /// HP cap.
  final double maxHealth;

  /// Ending cumulative combo.
  final int combo;

  factory TierResultPayload.fromJson(Map<String, dynamic> json) {
    return TierResultPayload(
      tierId: json['tierId'] as String,
      stageIndex: readRequiredInt(json, 'stageIndex', 'TierResultPayload.fromJson'),
      stageCount: json['stageCount'] is int ? json['stageCount'] as int : null,
      health: json['health'] is num ? (json['health'] as num).toDouble() : null,
      maxHealth: (json['maxHealth'] as num).toDouble(),
      combo: readRequiredInt(json, 'combo', 'TierResultPayload.fromJson'),
    );
  }

  Map<String, dynamic> toJson() {
    final json = <String, dynamic>{
      'tierId': tierId,
      'stageIndex': stageIndex,
      'maxHealth': maxHealth,
      'combo': combo,
    };
    if (stageCount != null) {
      json['stageCount'] = stageCount;
    }
    if (health != null) {
      json['health'] = health;
    }
    return json;
  }
}
