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

    test('factory default matches defaults constant', () {
      final fromFactory = HealthCheckWatchdogConfig();
      expect(
        fromFactory.firstResponseTimeout,
        HealthCheckWatchdogConfig.defaults.firstResponseTimeout,
      );
      expect(
        fromFactory.steadyResponseTimeout,
        HealthCheckWatchdogConfig.defaults.steadyResponseTimeout,
      );
      expect(
        fromFactory.pollInterval,
        HealthCheckWatchdogConfig.defaults.pollInterval,
      );
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
