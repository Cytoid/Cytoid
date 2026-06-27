import '_validators.dart';

/// `level` echo on `session.result` (v2 § LevelResultPayload).
class LevelResultPayload {
  const LevelResultPayload({
    required this.id,
    required this.title,
    required this.difficulty,
    required this.difficultyLevel,
  });

  /// Level id.
  final String id;

  /// Level title.
  final String title;

  /// Difficulty id.
  final String difficulty;

  /// Difficulty level integer.
  final int difficultyLevel;

  factory LevelResultPayload.fromJson(Map<String, dynamic> json) {
    return LevelResultPayload(
      id: json['id'] as String,
      title: json['title'] as String,
      difficulty: json['difficulty'] as String,
      difficultyLevel: readRequiredInt(
        json,
        'difficultyLevel',
        'LevelResultPayload',
      ),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'title': title,
      'difficulty': difficulty,
      'difficultyLevel': difficultyLevel,
    };
  }
}
