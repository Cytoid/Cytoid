/// Structured error shape used by `engine.error`, `settings.applied`, and the
/// `error` field on `session.result` (v2 § ErrorPayload).
///
/// Public class name is `GameCoreError` per the v2 spec's "Dart API
/// Expectations" — it reads as an error type rather than a wire payload name.
class GameCoreError {
  const GameCoreError({
    required this.code,
    required this.message,
    this.details,
  });

  /// Stable machine-readable error code (e.g. `invalid_payload`).
  final String code;

  /// Human-readable diagnostic message in English.
  final String message;

  /// Optional structured debug details.
  final Map<String, dynamic>? details;

  factory GameCoreError.fromJson(Map<String, dynamic> json) {
    final code = json['code'];
    final message = json['message'];
    if (code is! String) {
      throw FormatException('GameCoreError.fromJson: "code" must be a string.');
    }
    if (message is! String) {
      throw FormatException(
        'GameCoreError.fromJson: "message" must be a string.',
      );
    }
    final details = json['details'];
    if (details != null && details is! Map) {
      throw FormatException(
        'GameCoreError.fromJson: "details" must be an object when present.',
      );
    }
    return GameCoreError(
      code: code,
      message: message,
      details: details is Map
          ? Map<String, dynamic>.from(details)
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'code': code,
      'message': message,
      if (details != null) 'details': details,
    };
  }

  GameCoreError copyWith({
    String? code,
    String? message,
    Map<String, dynamic>? details,
  }) {
    return GameCoreError(
      code: code ?? this.code,
      message: message ?? this.message,
      details: details ?? this.details,
    );
  }
}
