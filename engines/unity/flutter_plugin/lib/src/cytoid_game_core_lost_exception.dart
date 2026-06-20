/// Raised when the game core stops responding during bridge ↔ game messaging.
class CytoidGameCoreLostException implements Exception {
  CytoidGameCoreLostException(this.message, {this.consecutiveFailures = 0});

  final String message;
  final int consecutiveFailures;

  @override
  String toString() => 'CytoidGameCoreLostException: $message';
}
