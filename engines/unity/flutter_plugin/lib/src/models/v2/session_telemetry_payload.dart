import 'play_events_payload.dart';

/// `session.telemetry` payload (v2 § SessionTelemetryPayload).
class SessionTelemetryPayload {
  const SessionTelemetryPayload({
    required this.sessionId,
    required this.playEvents,
  });

  /// Same as the envelope id.
  final String sessionId;

  /// Recorded play events.
  final PlayEventsPayload playEvents;

  factory SessionTelemetryPayload.fromJson(Map<String, dynamic> json) {
    final sessionId = json['sessionId'];
    final playEventsJson = json['playEvents'];
    if (sessionId is! String) {
      throw FormatException(
        'SessionTelemetryPayload.fromJson: "sessionId" must be a string.',
      );
    }
    if (playEventsJson is! Map) {
      throw FormatException(
        'SessionTelemetryPayload.fromJson: "playEvents" must be an object.',
      );
    }
    return SessionTelemetryPayload(
      sessionId: sessionId,
      playEvents: PlayEventsPayload.fromJson(
        Map<String, dynamic>.from(playEventsJson),
      ),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'sessionId': sessionId,
      'playEvents': playEvents.toJson(),
    };
  }
}
