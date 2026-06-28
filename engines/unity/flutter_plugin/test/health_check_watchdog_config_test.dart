import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('HealthCheckWatchdogConfig', () {
    test('const default has expected timeouts', () {
      const config = HealthCheckWatchdogConfig();
      expect(config.firstResponseTimeout, const Duration(seconds: 30));
      expect(config.steadyResponseTimeout, const Duration(seconds: 10));
      expect(config.pollInterval, const Duration(seconds: 10));
    });

    test('custom values round-trip through fields', () {
      const config = HealthCheckWatchdogConfig(
        firstResponseTimeout: Duration(seconds: 5),
        steadyResponseTimeout: Duration(seconds: 2),
        pollInterval: Duration(seconds: 1),
      );
      expect(config.firstResponseTimeout, const Duration(seconds: 5));
      expect(config.steadyResponseTimeout, const Duration(seconds: 2));
      expect(config.pollInterval, const Duration(seconds: 1));
    });

    test('identical const instances', () {
      const a = HealthCheckWatchdogConfig();
      const b = HealthCheckWatchdogConfig();
      expect(identical(a, b), isTrue);
    });
  });
}
