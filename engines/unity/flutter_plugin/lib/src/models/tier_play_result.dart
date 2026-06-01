/// Final tier stage state returned by the game core after one play session.
class TierPlayResult {
  const TierPlayResult({
    required this.stageIndex,
    required this.finalHealth,
    required this.maxHealth,
    required this.endingCombo,
    this.tierId,
  });

  final String? tierId;
  final int stageIndex;
  final double finalHealth;
  final double maxHealth;
  final int endingCombo;

  factory TierPlayResult.fromJson(Map<String, dynamic> json) {
    return TierPlayResult(
      tierId: json['tierId'] as String?,
      stageIndex: (json['stageIndex'] as num).toInt(),
      finalHealth: (json['finalHealth'] as num).toDouble(),
      maxHealth: (json['maxHealth'] as num).toDouble(),
      endingCombo: (json['endingCombo'] as num).toInt(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      if (tierId != null) 'tierId': tierId,
      'stageIndex': stageIndex,
      'finalHealth': finalHealth,
      'maxHealth': maxHealth,
      'endingCombo': endingCombo,
    };
  }
}
