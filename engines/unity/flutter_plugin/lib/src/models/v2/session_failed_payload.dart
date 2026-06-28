import '_validators.dart';
import 'error_payload.dart';

/// `session.failed` payload (v2 § session.failed). Terminal runtime-failure
/// envelope for an active session, synthesized by the native bridge when the
/// runtime dies mid-play (process gone, surface destroyed, engine recreated).
///
/// This envelope carries NO `outcome`, `flags`, `telemetry`, `mode`, or
/// `score` — the runtime is dead and cannot produce them. Routing rule:
/// `activeSessionId != null` -> `session.failed`; `== null` -> `engine.error`.
class SessionFailedPayload {
  const SessionFailedPayload({
    required this.sessionId,
    required this.error,
    required this.timestamp,
  });

  /// Same as the envelope id. Matches the `session.start` id.
  final String sessionId;

  /// Required. `code` comes from the `runtime_*` family
  /// (e.g. `runtime_unreachable`, `runtime_surface_lost`, `runtime_exception`).
  final GameCoreError error;

  /// Unix epoch milliseconds.
  final int timestamp;

  factory SessionFailedPayload.fromJson(Map<String, dynamic> json) {
    return SessionFailedPayload(
      sessionId: json['sessionId'] as String,
      error: GameCoreError.fromJson(_asMap(json, 'error')),
      timestamp: readRequiredInt(json, 'timestamp', 'SessionFailedPayload'),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'sessionId': sessionId,
      'error': error.toJson(),
      'timestamp': timestamp,
    };
  }

  static Map<String, dynamic> _asMap(Map<String, dynamic> json, String key) {
    final v = json[key];
    if (v is! Map) {
      throw FormatException(
        'SessionFailedPayload.fromJson: "$key" must be an object.',
      );
    }
    return Map<String, dynamic>.from(v);
  }
}
