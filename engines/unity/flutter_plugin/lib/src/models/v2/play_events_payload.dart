import 'play_event_payload.dart';

/// Recorded play events carried by `session.telemetry`
/// (v2 § PlayEventsPayload).
class PlayEventsPayload {
  const PlayEventsPayload({
    required this.format,
    required this.events,
  });

  /// Event payload format id. Currently `json.v1`. Engine MUST reject unknown
  /// formats.
  final String format;

  /// Recorded play events. May be empty.
  final List<PlayEventPayload> events;

  static const supportedFormats = {'json.v1'};

  factory PlayEventsPayload.fromJson(Map<String, dynamic> json) {
    final format = json['format'];
    if (format is! String) {
      throw FormatException(
        'PlayEventsPayload.fromJson: "format" must be a string.',
      );
    }
    if (!supportedFormats.contains(format)) {
      throw FormatException(
        'PlayEventsPayload.fromJson: unsupported format "$format" '
        '(expected one of $supportedFormats).',
      );
    }
    final eventsJson = json['events'];
    if (eventsJson is! List) {
      throw FormatException(
        'PlayEventsPayload.fromJson: "events" must be an array.',
      );
    }
    return PlayEventsPayload(
      format: format,
      events: eventsJson
          .map((e) => PlayEventPayload.fromJson(Map<String, dynamic>.from(e as Map)))
          .toList(growable: false),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'format': format,
      'events': events.map((e) => e.toJson()).toList(growable: false),
    };
  }
}
