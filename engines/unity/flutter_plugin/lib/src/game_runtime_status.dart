/// Runtime snapshot reported by the embedded game core.
class GameRuntimeStatus {
  const GameRuntimeStatus({
    required this.state,
    required this.engine,
    this.activePlayId,
  });

  final String state;
  final String engine;
  final String? activePlayId;

  bool get isReady => state == ready || state == busy || state == paused;
  bool get isBusy => state == busy;
  bool get isUnavailable => state == unavailable;

  Map<String, Object?> toJson() {
    return {
      'state': state,
      'engine': engine,
      if (activePlayId != null) 'activePlayId': activePlayId,
    };
  }

  factory GameRuntimeStatus.fromJson(Map<String, Object?> json) {
    final state = json['state'];
    final engine = json['engine'];
    final activePlayId = json['activePlayId'];
    if (state is! String) {
      throw const FormatException('Runtime status "state" must be a string.');
    }
    if (engine is! String) {
      throw const FormatException('Runtime status "engine" must be a string.');
    }
    if (activePlayId != null && activePlayId is! String) {
      throw const FormatException(
        'Runtime status "activePlayId" must be a string.',
      );
    }
    return GameRuntimeStatus(
      state: state,
      engine: engine,
      activePlayId: activePlayId as String?,
    );
  }

  static const starting = 'starting';
  static const ready = 'ready';
  static const busy = 'busy';
  static const paused = 'paused';
  static const unavailable = 'unavailable';
}
