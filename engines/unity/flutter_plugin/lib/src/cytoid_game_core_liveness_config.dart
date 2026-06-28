/// Legacy liveness tuning retained for hosts that still construct the client
/// with a config object. v2 session liveness is reported through `session.failed`.
class CytoidGameCoreLivenessConfig {
  const CytoidGameCoreLivenessConfig({
    this.checkInterval = const Duration(seconds: 5),
    this.pingTimeout = const Duration(seconds: 5),
    this.maxConsecutiveFailures = 3,
  });

  final Duration checkInterval;
  final Duration pingTimeout;
  final int maxConsecutiveFailures;
}
