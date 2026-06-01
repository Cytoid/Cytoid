/// Raised when the game ends the play route without a [game.play.result] envelope.
class CytoidGameCorePlayRouteEndedException implements Exception {
  const CytoidGameCorePlayRouteEndedException(this.message);

  final String message;

  @override
  String toString() => 'CytoidGameCorePlayRouteEndedException: $message';
}
