/// `session.start.options` block (v2 § SessionOptions).
class SessionOptions {
  const SessionOptions({required this.recordPlayEvents});

  /// Whether input telemetry should be recorded and sent via
  /// `session.telemetry`. The engine MUST suppress telemetry when an
  /// auto-class mod is active regardless of this flag.
  final bool recordPlayEvents;

  factory SessionOptions.fromJson(Map<String, dynamic> json) {
    final v = json['recordPlayEvents'];
    if (v is! bool) {
      throw FormatException(
        'SessionOptions.fromJson: "recordPlayEvents" must be a boolean.',
      );
    }
    return SessionOptions(recordPlayEvents: v);
  }

  Map<String, dynamic> toJson() {
    return {'recordPlayEvents': recordPlayEvents};
  }
}
