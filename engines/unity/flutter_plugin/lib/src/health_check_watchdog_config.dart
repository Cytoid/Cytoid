/// Tuning for [PlaySession]'s v2 health.check watchdog.
///
/// The watchdog sends `health.check` envelopes while awaiting a session
/// result; [firstResponseTimeout] covers the cold/loading engine after
/// `session.start`, [steadyResponseTimeout] covers steady-state play, and
/// [pollInterval] is the cadence (a check is skipped when a non-terminal
/// engine-originated envelope arrived within the last [pollInterval]).
class HealthCheckWatchdogConfig {
  const HealthCheckWatchdogConfig({
    this.firstResponseTimeout = const Duration(seconds: 30),
    this.steadyResponseTimeout = const Duration(seconds: 10),
    this.pollInterval = const Duration(seconds: 10),
  });

  final Duration firstResponseTimeout;
  final Duration steadyResponseTimeout;
  final Duration pollInterval;
}
