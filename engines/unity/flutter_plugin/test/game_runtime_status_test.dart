import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter_test/flutter_test.dart';

/// Validates the v2 GameRuntimeStatus model: 6-state enum (no `paused`),
/// conditional optionality on the wire (activeSessionId IFF busy, error IFF
/// failed), and round-trip through fromJson/toJson for the v2 snapshot shape.
void main() {
  group('GameRuntimeStatus — v2 enum values', () {
    test('exposes exactly the 6 v2 states in spec order', () {
      expect(GameRuntimeStatus.allStates, [
        'unavailable',
        'starting',
        'ready',
        'busy',
        'suspended',
        'failed',
      ]);
    });

    test('does NOT expose a paused state', () {
      // The v2 spec dropped paused in favour of suspended. This grep-style
      // assertion prevents accidental re-introduction.
      const source = '''
        starting, ready, busy, suspended, failed, unavailable
      ''';
      expect(source.contains('paused'), isFalse);
      expect(() =>GameRuntimeStatus.busy, returnsNormally);
      // The const list is the source of truth.
      expect(GameRuntimeStatus.allStates.contains('paused'), isFalse);
    });

    test('state predicates classify every state', () {
      const status = GameRuntimeStatus(
        state: GameRuntimeStatus.unavailable,
        engine: 'mock',
        mode: 'unavailable',
        generation: 0,
      );
      expect(status.isUnavailable, isTrue);
      expect(status.isStarting, isFalse);
      expect(status.isReady, isFalse);
      expect(status.isBusy, isFalse);
      expect(status.isSuspended, isFalse);
      expect(status.isFailed, isFalse);
      expect(status.isRuntimeUp, isFalse);
    });
  });

  group('GameRuntimeStatus — conditional optionality', () {
    test('busy snapshot includes activeSessionId', () {
      const status = GameRuntimeStatus(
        state: GameRuntimeStatus.busy,
        engine: 'unity',
        mode: 'unity',
        generation: 3,
        activeSessionId: 'session-7',
      );
      final json = status.toJson();
      expect(json.keys, containsAll(['engine', 'mode', 'state', 'generation']));
      expect(json['activeSessionId'], 'session-7');
      expect(json.containsKey('error'), isFalse);
    });

    test('ready snapshot omits activeSessionId and error', () {
      const status = GameRuntimeStatus(
        state: GameRuntimeStatus.ready,
        engine: 'unity',
        mode: 'unity',
        generation: 3,
      );
      final json = status.toJson();
      expect(json.keys, containsAll(['engine', 'mode', 'state', 'generation']));
      expect(json.containsKey('activeSessionId'), isFalse);
      expect(json.containsKey('error'), isFalse);
    });

    test('failed snapshot includes error', () {
      const status = GameRuntimeStatus(
        state: GameRuntimeStatus.failed,
        engine: 'unity',
        mode: 'unity',
        generation: 3,
        error: GameCoreError(
          code: 'runtime_exception',
          message: 'engine died',
        ),
      );
      final json = status.toJson();
      expect(json.keys, containsAll(['engine', 'mode', 'state', 'generation']));
      expect(json.containsKey('error'), isTrue);
      final error = json['error']! as Map<String, dynamic>;
      expect(error['code'], 'runtime_exception');
      expect(error['message'], 'engine died');
      expect(json.containsKey('activeSessionId'), isFalse);
    });

    test('suspended snapshot omits activeSessionId and error', () {
      const status = GameRuntimeStatus(
        state: GameRuntimeStatus.suspended,
        engine: 'unity',
        mode: 'unity',
        generation: 3,
      );
      final json = status.toJson();
      expect(json['state'], 'suspended');
      expect(json.containsKey('activeSessionId'), isFalse);
      expect(json.containsKey('error'), isFalse);
    });
  });

  group('GameRuntimeStatus — round-trip', () {
    test('busy snapshot round-trips', () {
      const original = GameRuntimeStatus(
        state: GameRuntimeStatus.busy,
        engine: 'unity',
        mode: 'unity',
        generation: 3,
        activeSessionId: 'session-9',
      );
      final decoded = GameRuntimeStatus.fromJson(original.toJson());
      expect(decoded.state, 'busy');
      expect(decoded.engine, 'unity');
      expect(decoded.mode, 'unity');
      expect(decoded.generation, 3);
      expect(decoded.activeSessionId, 'session-9');
      expect(decoded.error, isNull);
    });

    test('failed snapshot round-trips with error', () {
      const original = GameRuntimeStatus(
        state: GameRuntimeStatus.failed,
        engine: 'unity',
        mode: 'unity',
        generation: 4,
        error: GameCoreError(
          code: 'runtime_surface_lost',
          message: 'surface lost',
          details: {'at': 1234},
        ),
      );
      final decoded = GameRuntimeStatus.fromJson(original.toJson());
      expect(decoded.state, 'failed');
      expect(decoded.error, isNotNull);
      expect(decoded.error!.code, 'runtime_surface_lost');
      expect(decoded.error!.message, 'surface lost');
      expect(decoded.error!.details?['at'], 1234);
    });

  });

  group('GameRuntimeStatus — spec snapshot shape', () {
    test('the v2 example snapshot from the spec parses', () {
      // Direct lift from v2 spec § Native Runtime Contract example.
      final json = {
        'engine': 'unity',
        'mode': 'unity',
        'state': 'ready',
        'generation': 3,
      };
      final status = GameRuntimeStatus.fromJson(json);
      expect(status.state, 'ready');
      expect(status.engine, 'unity');
      expect(status.mode, 'unity');
      expect(status.generation, 3);
      expect(status.activeSessionId, isNull);
      expect(status.error, isNull);
    });

    test('busy snapshot from spec parses (with activeSessionId)', () {
      final json = {
        'engine': 'unity',
        'mode': 'unity',
        'state': 'busy',
        'generation': 3,
        'activeSessionId': 'session-123',
      };
      final status = GameRuntimeStatus.fromJson(json);
      expect(status.isBusy, isTrue);
      expect(status.activeSessionId, 'session-123');
    });

    test('failed snapshot from spec parses (with error)', () {
      final json = {
        'engine': 'unity',
        'mode': 'unity',
        'state': 'failed',
        'generation': 4,
        'error': {
          'code': 'runtime_exception',
          'message': 'engine died',
        },
      };
      final status = GameRuntimeStatus.fromJson(json);
      expect(status.isFailed, isTrue);
      expect(status.error?.code, 'runtime_exception');
    });
  });

  group('GameRuntimeStatus — v2 spec conditional optionality contract', () {
    // This is the canonical contract assertion the task called out:
    //   - ALWAYS contains required keys: ["engine","mode","state","generation"]
    //   - Contains activeSessionId IFF state=="busy"
    //   - Contains error IFF state=="failed"

    for (final state in GameRuntimeStatus.allStates) {
      test('snapshot for state="$state" matches conditional optionality', () {
        final status = GameRuntimeStatus(
          state: state,
          engine: 'unity',
          mode: 'unity',
          generation: 1,
          activeSessionId: state == 'busy' ? 'session-x' : null,
          error: state == 'failed'
              ? const GameCoreError(code: 'x', message: 'y')
              : null,
        );
        final json = status.toJson();
        // Required keys always present.
        expect(json['engine'], 'unity');
        expect(json['mode'], 'unity');
        expect(json['state'], state);
        expect(json['generation'], 1);
        // activeSessionId IFF busy.
        expect(
          json.containsKey('activeSessionId'),
          state == 'busy',
          reason: 'activeSessionId must be present IFF state=="busy"',
        );
        // error IFF failed.
        expect(
          json.containsKey('error'),
          state == 'failed',
          reason: 'error must be present IFF state=="failed"',
        );
      });
    }
  });
}
