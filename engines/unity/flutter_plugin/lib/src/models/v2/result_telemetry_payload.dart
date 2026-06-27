/// Telemetry summary carried on every `session.result`
/// (v2 § ResultTelemetryPayload). Indicates whether full telemetry is
/// available via separate `session.telemetry` messages.
class ResultTelemetryPayload {
  const ResultTelemetryPayload({
    required this.available,
    required this.eventsRecorded,
    required this.bytes,
  });

  /// Whether `session.telemetry` messages were sent for this session.
  /// False when auto-class mod suppressed recording or when
  /// `options.recordPlayEvents = false`.
  final bool available;

  /// Number of recorded play events. `0` when `available = false`.
  final int eventsRecorded;

  /// Approximate uncompressed size of recorded events in bytes.
  /// `0` when `available = false`.
  final int bytes;

  factory ResultTelemetryPayload.fromJson(Map<String, dynamic> json) {
    final available = json['available'];
    final eventsRecorded = json['eventsRecorded'];
    final bytes = json['bytes'];
    if (available is! bool) {
      throw FormatException(
        'ResultTelemetryPayload.fromJson: "available" must be a boolean.',
      );
    }
    if (eventsRecorded is! int) {
      throw FormatException(
        'ResultTelemetryPayload.fromJson: "eventsRecorded" must be an integer.',
      );
    }
    if (bytes is! int) {
      throw FormatException(
        'ResultTelemetryPayload.fromJson: "bytes" must be an integer.',
      );
    }
    return ResultTelemetryPayload(
      available: available,
      eventsRecorded: eventsRecorded,
      bytes: bytes,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'available': available,
      'eventsRecorded': eventsRecorded,
      'bytes': bytes,
    };
  }

  ResultTelemetryPayload copyWith({
    bool? available,
    int? eventsRecorded,
    int? bytes,
  }) {
    return ResultTelemetryPayload(
      available: available ?? this.available,
      eventsRecorded: eventsRecorded ?? this.eventsRecorded,
      bytes: bytes ?? this.bytes,
    );
  }
}
