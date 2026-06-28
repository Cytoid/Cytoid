import 'models/v2/error_payload.dart';

/// Runtime snapshot reported by the embedded game core (v2 § Native Runtime
/// Contract).
///
/// Replaces the v1 5-state form (which had a fifth hidden lifecycle phase) with
/// the v2 6-state form that uses `suspended` and `failed`. The wire fields
/// `engine`, `mode`, `state`, `generation` are always present;
/// `activeSessionId` is present iff `state == busy`; `error` is present iff
/// `state == failed`.
class GameRuntimeStatus {
  const GameRuntimeStatus({
    required this.state,
    required this.engine,
    required this.mode,
    required this.generation,
    this.activeSessionId,
    this.error,
  });

  /// One of [starting], [ready], [busy], [suspended], [failed],
  /// [unavailable].
  final String state;

  /// Engine adapter id, for example `unity` or `mock`.
  final String engine;

  /// Runtime mode: `unity`, `mock`, or `unavailable`.
  final String mode;

  /// Incremented when the engine runtime is recreated.
  final int generation;

  /// Required when `state == busy`. Null otherwise.
  final String? activeSessionId;

  /// Required when `state == failed`. Null otherwise.
  final GameCoreError? error;

  bool get isUnavailable => state == unavailable;
  bool get isStarting => state == starting;
  bool get isReady => state == ready;
  bool get isBusy => state == busy;
  bool get isSuspended => state == suspended;
  bool get isFailed => state == failed;

  /// True when the runtime is initialized and either idle or running a
  /// session or briefly suspended. Suspended means "still initialized, just
  /// hidden" — the runtime has not been torn down.
  bool get isRuntimeUp => state == ready || state == busy || state == suspended;

  /// Backwards-compatible v1 alias: the runtime can be interacted with.
  /// Equivalent to v1's "state is one of {ready, busy, hidden}" semantics.
  bool get isReadyOrActive => isRuntimeUp;

  Map<String, Object?> toJson() {
    final json = <String, Object?>{
      'engine': engine,
      'mode': mode,
      'state': state,
      'generation': generation,
    };
    if (state == busy && activeSessionId != null) {
      json['activeSessionId'] = activeSessionId;
    }
    if (state == failed && error != null) {
      json['error'] = error!.toJson();
    }
    return json;
  }

  factory GameRuntimeStatus.fromJson(Map<String, Object?> json) {
    final state = json['state'];
    final engine = json['engine'];
    if (state is! String) {
      throw const FormatException('Runtime status "state" must be a string.');
    }
    if (engine is! String) {
      throw const FormatException('Runtime status "engine" must be a string.');
    }
    if (!allStates.contains(state)) {
      throw FormatException(
        'Runtime status "state" must be one of $allStates.',
      );
    }
    final modeRaw = json['mode'];
    if (modeRaw is! String) {
      throw const FormatException(
        'Runtime status "mode" must be a string.',
      );
    }
    final generationRaw = json['generation'];
    if (generationRaw is! num || generationRaw % 1 != 0) {
      throw const FormatException(
        'Runtime status "generation" must be an integer.',
      );
    }
    final generation = generationRaw.toInt();

    final activeSessionId = json['activeSessionId'];
    if (activeSessionId != null && activeSessionId is! String) {
      throw const FormatException(
        'Runtime status "activeSessionId" must be a string when present.',
      );
    }

    final errorJson = json['error'];
    GameCoreError? error;
    if (errorJson != null) {
      if (errorJson is! Map) {
        throw const FormatException(
          'Runtime status "error" must be an object when present.',
        );
      }
      error = GameCoreError.fromJson(Map<String, dynamic>.from(errorJson));
    }

    if (state == busy && activeSessionId == null) {
      throw const FormatException(
        'Runtime status "activeSessionId" is required when state is busy.',
      );
    }
    if (state == failed && error == null) {
      throw const FormatException(
        'Runtime status "error" is required when state is failed.',
      );
    }

    return GameRuntimeStatus(
      state: state,
      engine: engine,
      mode: modeRaw,
      generation: generation,
      activeSessionId: activeSessionId as String?,
      error: error,
    );
  }

  static const starting = 'starting';
  static const ready = 'ready';
  static const busy = 'busy';
  static const suspended = 'suspended';
  static const failed = 'failed';
  static const unavailable = 'unavailable';

  /// All v2 runtime state names in spec order. Used by tests.
  static const allStates = [
    unavailable,
    starting,
    ready,
    busy,
    suspended,
    failed,
  ];
}
