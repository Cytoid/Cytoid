/// `score` field on `session.result` (v2 § ScorePayload).
class ScorePayload {
  const ScorePayload({
    required this.score,
    required this.accuracy,
    required this.maxCombo,
    required this.gradeCounts,
    required this.early,
    required this.late,
    this.averageTimingError,
    this.standardTimingError,
  });

  /// Numeric score.
  final int score;

  /// Accuracy from 0 to 1.
  final double accuracy;

  /// Max combo.
  final int maxCombo;

  /// Stable grade keys (e.g. `perfect`, `great`, `good`, `bad`, `miss`).
  final Map<String, int> gradeCounts;

  /// Early count.
  final int early;

  /// Late count.
  final int late;

  /// Average timing error.
  final double? averageTimingError;

  /// Standard timing error.
  final double? standardTimingError;

  factory ScorePayload.fromJson(Map<String, dynamic> json) {
    final gradeCounts = json['gradeCounts'];
    if (gradeCounts is! Map) {
      throw FormatException(
        'ScorePayload.fromJson: "gradeCounts" must be an object.',
      );
    }
    return ScorePayload(
      score: _readInt(json, 'score'),
      accuracy: _readDouble(json, 'accuracy'),
      maxCombo: _readInt(json, 'maxCombo'),
      gradeCounts: _readGradeCounts(gradeCounts),
      early: _readInt(json, 'early'),
      late: _readInt(json, 'late'),
      averageTimingError: _readDoubleOrNull(json, 'averageTimingError'),
      standardTimingError: _readDoubleOrNull(json, 'standardTimingError'),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'score': score,
      'accuracy': accuracy,
      'maxCombo': maxCombo,
      'gradeCounts': gradeCounts,
      'early': early,
      'late': late,
      if (averageTimingError != null) 'averageTimingError': averageTimingError,
      if (standardTimingError != null) 'standardTimingError': standardTimingError,
    };
  }

  static Map<String, int> _readGradeCounts(Object raw) {
    final result = <String, int>{};
    for (final entry in (raw as Map).entries) {
      final key = entry.key;
      if (key is! String) {
        throw FormatException(
          'ScorePayload.fromJson: "gradeCounts" keys must be strings.',
        );
      }
      final v = entry.value;
      if (v is int) {
        result[key] = v;
      } else if (v is num && v % 1 == 0) {
        result[key] = v.toInt();
      } else {
        throw FormatException(
          'ScorePayload.fromJson: "gradeCounts" values must be integers.',
        );
      }
    }
    return result;
  }

  static int _readInt(Map<String, dynamic> json, String key) {
    final v = json[key];
    if (v is! int) {
      throw FormatException('ScorePayload.fromJson: "$key" must be an integer.');
    }
    return v;
  }

  static double _readDouble(Map<String, dynamic> json, String key) {
    final v = json[key];
    if (v is! num) {
      throw FormatException('ScorePayload.fromJson: "$key" must be a number.');
    }
    return v.toDouble();
  }

  static double? _readDoubleOrNull(Map<String, dynamic> json, String key) {
    final v = json[key];
    if (v == null) return null;
    if (v is! num) {
      throw FormatException('ScorePayload.fromJson: "$key" must be a number.');
    }
    return v.toDouble();
  }
}
