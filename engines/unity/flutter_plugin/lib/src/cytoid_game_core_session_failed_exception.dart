/// Thrown when an active session terminates via a `session.failed` envelope —
/// i.e. the runtime died mid-play (process gone, surface destroyed, engine
/// recreated) and the native bridge synthesized the failure.
///
/// The host MUST surface this as a session-level failure (distinct from a
/// gameplay result); never silently continue. [PlaySession.run] completes this
/// exception's future with an error when the matching `session.failed`
/// envelope arrives, so callers cannot accidentally treat a dead runtime as a
/// successful gameplay outcome.
class CytoidGameCoreSessionFailedException implements Exception {
  const CytoidGameCoreSessionFailedException({
    required this.sessionId,
    required this.code,
    required this.message,
    this.details,
  });

  /// The id of the session that failed. Matches the envelope id and the
  /// `session.start` id.
  final String sessionId;

  /// Stable machine-readable error code from the `runtime_*` family
  /// (e.g. `runtime_unreachable`, `runtime_surface_lost`, `runtime_exception`).
  final String code;

  /// Human-readable diagnostic message in English.
  final String message;

  /// Optional structured debug details forwarded from the underlying
  /// `GameCoreError`.
  final Map<String, dynamic>? details;

  @override
  String toString() =>
      'CytoidGameCoreSessionFailedException($code): $message '
      '(sessionId=$sessionId)';
}
