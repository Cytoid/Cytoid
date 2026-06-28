/// Tuning for [PlaySession]'s v2 health.check watchdog.
///
/// The watchdog sends `health.check` envelopes while awaiting a session
/// result; [firstResponseTimeout] covers the cold/loading engine after
/// `session.start`, [steadyResponseTimeout] covers steady-state play, and
/// [pollInterval] is the cadence (a check is skipped when a non-terminal
/// engine-originated envelope arrived within the last [pollInterval]).
class HealthCheckWatchdogConfig {
  /// Default watchdog tuning: 30s first response, 10s steady, 10s poll.
  static const HealthCheckWatchdogConfig defaults = HealthCheckWatchdogConfig._(
    firstResponseTimeout: Duration(seconds: 30),
    steadyResponseTimeout: Duration(seconds: 10),
    pollInterval: Duration(seconds: 10),
  );

  factory HealthCheckWatchdogConfig({
    Duration? firstResponseTimeout,
    Duration? steadyResponseTimeout,
    Duration? pollInterval,
  }) {
    final timeout = firstResponseTimeout ?? const Duration(seconds: 30);
    final steady = steadyResponseTimeout ?? const Duration(seconds: 10);
    final interval = pollInterval ?? const Duration(seconds: 10);
    assert(
      timeout.inMicroseconds > 0,
      'firstResponseTimeout must be strictly positive',
    );
    assert(
      steady.inMicroseconds > 0,
      'steadyResponseTimeout must be strictly positive',
    );
    assert(
      interval.inMicroseconds > 0,
      'pollInterval must be strictly positive',
    );
    return HealthCheckWatchdogConfig._(
      firstResponseTimeout: timeout,
      steadyResponseTimeout: steady,
      pollInterval: interval,
    );
  }

  const HealthCheckWatchdogConfig._({
    required this.firstResponseTimeout,
    required this.steadyResponseTimeout,
    required this.pollInterval,
  });

  final Duration firstResponseTimeout;
  final Duration steadyResponseTimeout;
  final Duration pollInterval;
}
