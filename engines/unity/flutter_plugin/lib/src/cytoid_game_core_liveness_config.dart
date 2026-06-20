/// How [CytoidGameCoreClient] monitors the game core while waiting for [game.play.result].
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
