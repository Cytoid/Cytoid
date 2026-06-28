import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('HealthCheckWatchdogConfig', () {
    test('const default has expected timeouts', () {
      final config = HealthCheckWatchdogConfig();
      expect(config.firstResponseTimeout, const Duration(seconds: 30));
      expect(config.steadyResponseTimeout, const Duration(seconds: 10));
      expect(config.pollInterval, const Duration(seconds: 10));
    });

    test('custom values round-trip through fields', () {
      final config = HealthCheckWatchdogConfig(
        firstResponseTimeout: const Duration(seconds: 5),
        steadyResponseTimeout: const Duration(seconds: 2),
        pollInterval: const Duration(seconds: 1),
      );
      expect(config.firstResponseTimeout, const Duration(seconds: 5));
      expect(config.steadyResponseTimeout, const Duration(seconds: 2));
      expect(config.pollInterval, const Duration(seconds: 1));
    });

    test('identical const instances', () {
      final a = HealthCheckWatchdogConfig();
      final b = HealthCheckWatchdogConfig();
      expect(identical(a, b), isFalse);
    });

    test('rejects non-positive durations with ArgumentError', () {
      expect(
        () => HealthCheckWatchdogConfig(pollInterval: Duration.zero),
        throwsA(isA<ArgumentError>()),
      );
      expect(
        () => HealthCheckWatchdogConfig(
          firstResponseTimeout: Duration(seconds: -1),
        ),
        throwsA(isA<ArgumentError>()),
      );
      expect(
        () => HealthCheckWatchdogConfig(steadyResponseTimeout: Duration.zero),
        throwsA(isA<ArgumentError>()),
      );
    });
  });
}
