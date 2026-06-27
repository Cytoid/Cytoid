/// Single log entry inside `logs.batch` (v2 § LogEntryPayload).
class LogEntryPayload {
  const LogEntryPayload({
    required this.level,
    required this.message,
    required this.timestamp,
    this.stackTrace,
    this.sessionId,
  });

  /// Severity: `debug`, `info`, `warning`, `error`, or `fatal`.
  final String level;

  /// Log message text.
  final String message;

  /// Stack trace when the entry captures a thrown exception.
  final String? stackTrace;

  /// Unix epoch milliseconds.
  final int timestamp;

  /// Required when the entry is bound to an active session. Omit for
  /// engine-global logs.
  final String? sessionId;

  static const validLevels = {'debug', 'info', 'warning', 'error', 'fatal'};

  factory LogEntryPayload.fromJson(Map<String, dynamic> json) {
    final level = json['level'];
    final message = json['message'];
    final timestamp = json['timestamp'];
    if (level is! String || !validLevels.contains(level)) {
      throw FormatException(
        'LogEntryPayload.fromJson: "level" must be one of $validLevels.',
      );
    }
    if (message is! String) {
      throw FormatException(
        'LogEntryPayload.fromJson: "message" must be a string.',
      );
    }
    if (timestamp is! int) {
      throw FormatException(
        'LogEntryPayload.fromJson: "timestamp" must be an integer.',
      );
    }
    return LogEntryPayload(
      level: level,
      message: message,
      timestamp: timestamp,
      stackTrace: json['stackTrace'] as String?,
      sessionId: json['sessionId'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'level': level,
      'message': message,
      'timestamp': timestamp,
      if (stackTrace != null) 'stackTrace': stackTrace,
      if (sessionId != null) 'sessionId': sessionId,
    };
  }
}
