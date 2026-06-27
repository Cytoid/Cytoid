/// Single recorded touch event (v2 § PlayEventPayload). Field names are short
/// to keep large telemetry batches small.
class PlayEventPayload {
  const PlayEventPayload({
    required this.t,
    required this.f,
    required this.p,
    required this.x,
    required this.y,
  });

  /// Milliseconds since the session gameplay clock started. Non-negative.
  final int t;

  /// Finger index, 0-based. Stable within one finger lifecycle.
  final int f;

  /// Phase: `down`, `move`, or `up`.
  final String p;

  /// Normalized screen X, 0–65535 inclusive (left → right).
  final int x;

  /// Normalized screen Y, 0–65535 inclusive (bottom → top).
  final int y;

  static const validPhases = {'down', 'move', 'up'};

  factory PlayEventPayload.fromJson(Map<String, dynamic> json) {
    final t = json['t'];
    final f = json['f'];
    final p = json['p'];
    final x = json['x'];
    final y = json['y'];
    if (t is! int || t < 0) {
      throw FormatException(
        'PlayEventPayload.fromJson: "t" must be a non-negative integer.',
      );
    }
    if (f is! int) {
      throw FormatException(
        'PlayEventPayload.fromJson: "f" must be an integer.',
      );
    }
    if (p is! String || !validPhases.contains(p)) {
      throw FormatException(
        'PlayEventPayload.fromJson: "p" must be one of $validPhases.',
      );
    }
    if (x is! int || x < 0 || x > 65535) {
      throw FormatException(
        'PlayEventPayload.fromJson: "x" must be an integer in 0..65535.',
      );
    }
    if (y is! int || y < 0 || y > 65535) {
      throw FormatException(
        'PlayEventPayload.fromJson: "y" must be an integer in 0..65535.',
      );
    }
    return PlayEventPayload(t: t, f: f, p: p, x: x, y: y);
  }

  Map<String, dynamic> toJson() {
    return {'t': t, 'f': f, 'p': p, 'x': x, 'y': y};
  }
}
