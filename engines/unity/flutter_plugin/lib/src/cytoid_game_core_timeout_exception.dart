/// Thrown when a host-side wait for an engine acknowledgement does not complete
/// within the configured timeout.
///
/// The host MUST surface this as a launch failure; never silently continue.
/// In particular, [CytoidGameCoreClient.waitForReady] throws this when the
/// engine does not become ready in time, so callers cannot accidentally fall
/// through to `session.start` against an uninitialized engine.
class CytoidGameCoreTimeoutException implements Exception {
  const CytoidGameCoreTimeoutException(this.message, {this.timeout});

  final String message;

  /// The timeout that elapsed, when known. `null` when the wait used a
  /// platform-native default whose value is not visible to Dart.
  final Duration? timeout;

  @override
  String toString() {
    if (timeout != null) {
      return 'CytoidGameCoreTimeoutException: $message (timeout: $timeout)';
    }
    return 'CytoidGameCoreTimeoutException: $message';
  }
}
