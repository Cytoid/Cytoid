import 'tier_play_result.dart';

/// Outcome of a gameplay session emitted by the game core.
class GameResultPayload {
  const GameResultPayload({
    required this.completed,
    required this.failed,
    required this.usedAutoMod,
    this.error,
    this.gameMode,
    this.tierPlay,
    this.tierRetry,
    this.calibratedBaseNoteOffset,
    this.calibratedLevelNoteOffset,
    this.timestamp,
    this.levelId,
    this.title,
    this.difficulty,
    this.difficultyLevel,
    this.score,
    this.accuracy,
    this.maxCombo,
    this.gradeCounts,
    this.early,
    this.late,
    this.averageTimingError,
    this.standardTimingError,
  });

  final bool completed;
  final bool failed;
  final bool usedAutoMod;
  final String? error;
  final String? gameMode;
  final TierPlayResult? tierPlay;
  final String? tierRetry;
  final double? calibratedBaseNoteOffset;
  final double? calibratedLevelNoteOffset;
  final String? timestamp;
  final String? levelId;
  final String? title;
  final String? difficulty;
  final int? difficultyLevel;
  final int? score;
  final double? accuracy;
  final int? maxCombo;
  final Map<String, int>? gradeCounts;
  final int? early;
  final int? late;
  final double? averageTimingError;
  final double? standardTimingError;

  factory GameResultPayload.fromJson(Map<String, dynamic> json) {
    return GameResultPayload(
      completed: json['completed'] as bool? ?? false,
      failed: json['failed'] as bool? ?? false,
      usedAutoMod: json['usedAutoMod'] as bool? ?? false,
      error: json['error'] as String?,
      gameMode: json['gameMode'] as String?,
      tierPlay: json['tierPlay'] is Map<String, dynamic>
          ? TierPlayResult.fromJson(json['tierPlay'] as Map<String, dynamic>)
          : null,
      tierRetry: json['tierRetry'] as String?,
      calibratedBaseNoteOffset: _readDouble(json, 'calibratedBaseNoteOffset'),
      calibratedLevelNoteOffset: _readDouble(json, 'calibratedLevelNoteOffset'),
      timestamp: json['timestamp'] as String?,
      levelId: json['levelId'] as String?,
      title: json['title'] as String?,
      difficulty: json['difficulty'] as String?,
      difficultyLevel: _readInt(json, 'difficultyLevel'),
      score: _readInt(json, 'score'),
      accuracy: _readDouble(json, 'accuracy'),
      maxCombo: _readInt(json, 'maxCombo'),
      gradeCounts: _readGradeCounts(json['gradeCounts']),
      early: _readInt(json, 'early'),
      late: _readInt(json, 'late'),
      averageTimingError: _readDouble(json, 'averageTimingError'),
      standardTimingError: _readDouble(json, 'standardTimingError'),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'completed': completed,
      'failed': failed,
      'usedAutoMod': usedAutoMod,
      if (error != null) 'error': error,
      if (gameMode != null) 'gameMode': gameMode,
      if (tierPlay != null) 'tierPlay': tierPlay!.toJson(),
      if (tierRetry != null) 'tierRetry': tierRetry,
      if (calibratedBaseNoteOffset != null)
        'calibratedBaseNoteOffset': calibratedBaseNoteOffset,
      if (calibratedLevelNoteOffset != null)
        'calibratedLevelNoteOffset': calibratedLevelNoteOffset,
      if (timestamp != null) 'timestamp': timestamp,
      if (levelId != null) 'levelId': levelId,
      if (title != null) 'title': title,
      if (difficulty != null) 'difficulty': difficulty,
      if (difficultyLevel != null) 'difficultyLevel': difficultyLevel,
      if (score != null) 'score': score,
      if (accuracy != null) 'accuracy': accuracy,
      if (maxCombo != null) 'maxCombo': maxCombo,
      if (gradeCounts != null) 'gradeCounts': gradeCounts,
      if (early != null) 'early': early,
      if (late != null) 'late': late,
      if (averageTimingError != null) 'averageTimingError': averageTimingError,
      if (standardTimingError != null)
        'standardTimingError': standardTimingError,
    };
  }

  static double? _readDouble(Map<String, dynamic> json, String key) {
    final value = json[key];
    if (value == null) {
      return null;
    }
    if (value is num) {
      return value.toDouble();
    }
    throw FormatException('Expected number for "$key".');
  }

  static int? _readInt(Map<String, dynamic> json, String key) {
    final value = json[key];
    if (value == null) {
      return null;
    }
    if (value is num) {
      return value.toInt();
    }
    throw FormatException('Expected integer for "$key".');
  }

  static Map<String, int>? _readGradeCounts(Object? value) {
    if (value == null) {
      return null;
    }
    if (value is! Map) {
      throw FormatException('gradeCounts must be an object.');
    }
    return value.map(
      (key, raw) => MapEntry(key.toString(), (raw as num).toInt()),
    );
  }
}
