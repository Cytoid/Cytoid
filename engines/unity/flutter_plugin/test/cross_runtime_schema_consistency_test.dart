import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter_test/flutter_test.dart';

/// T15 (Metis H5 fold-in — C7): Cross-runtime schema consistency.
///
/// Verifies that the v2 envelope schema string `"cytoid.game-core.v2"` is
/// present on the wire in three independently-produced envelope shapes:
///
/// 1. Dart-produced envelope via `CytoidGameCoreEnvelope.create(...).toJson()`.
/// 2. Kotlin-synthesised `session.failed` envelope, mirroring the literal
///    output of Android `CytoidGameCoreBridge.synthesizeRuntimeFailure`.
/// 3. Swift-synthesised `engine.error` envelope, mirroring the literal output
///    of iOS `CytoidGameCoreBridge.emitEngineError`.
///
/// Unity-produced envelopes are intentionally OUT of scope here — there is no
/// agent-executable Unity envelope producer. Unity envelope verification lives
/// in T16 (real-device smoke). See `.omo/plans/unity-core-v2-migration.md`
/// T15 and review notes Codex #17 / Codex r2#8 / Momus r2#3.
///
/// The literal envelopes below are written in COMPACT wire form (no
/// insignificant whitespace) because that is what the native JSON serializers
/// actually emit on the bridge wire: Kotlin `JSONObject.toString()` and
/// Swift `JSONSerialization.data(withJSONObject:)` both default to compact
/// output. The substring assertion on `'"schema":"cytoid.game-core.v2"'`
/// therefore reflects the literal byte form a host receives.
const String _kotlinSynthSessionFailedJson =
    '{"schema":"cytoid.game-core.v2","id":"sess-9f8e7d","type":"session.failed"'
    ',"payload":{"sessionId":"sess-9f8e7d","error":{"code":"runtime_unreachable",'
    '"message":"Unity runtime became unreachable."},"timestamp":1719580000000}}';

const String _swiftSynthEngineErrorJson =
    '{"schema":"cytoid.game-core.v2",'
    '"id":"B7A1E0F0-1111-2222-3333-444455556666","type":"engine.error",'
    '"payload":{"error":{"code":"runtime_exception",'
    '"message":"IllegalStateException: Unity bridge threw."}}}';

void main() {
  group('C7 cross-runtime schema consistency', () {
    test('Dart envelope carries cytoid.game-core.v2 on the wire', () {
      // Given: a Dart-built envelope.
      final envelope = CytoidGameCoreEnvelope.create(
        id: 'abc',
        type: WireMessageType.healthCheck,
        payload: const <String, Object?>{'text': 'hello'},
      );

      // When: serialised to the on-the-wire JSON form.
      final json = envelope.toJson();
      final jsonString = envelope.toJsonString();

      // Then: the v2 schema is present both as a field and as a literal
      // substring of the wire form.
      expect(json['schema'], CytoidGameCoreEnvelope.currentSchema);
      expect(jsonString, contains('"schema":"cytoid.game-core.v2"'));
    });

    test('Kotlin-synthesised session.failed envelope parses as v2', () {
      // Given: a literal Kotlin-style synth envelope mirroring
      // `CytoidGameCoreBridge.synthesizeRuntimeFailure` on Android.

      // When: parsed by the Dart envelope parser.
      final decoded = CytoidGameCoreEnvelope.fromJsonString(
        _kotlinSynthSessionFailedJson,
      );

      // Then: the literal carries v2 schema and the parser accepts it.
      expect(
        _kotlinSynthSessionFailedJson,
        contains('"schema":"cytoid.game-core.v2"'),
      );
      expect(decoded.schema, CytoidGameCoreEnvelope.currentSchema);
      expect(decoded.id, 'sess-9f8e7d');
      expect(decoded.type, WireMessageType.sessionFailed);
      expect(decoded.isSessionFailed, isTrue);
      final error = decoded.payload['error'] as Map<String, Object?>;
      expect(error['code'], 'runtime_unreachable');
    });

    test('Swift-synthesised engine.error envelope parses as v2', () {
      // Given: a literal Swift-style synth envelope mirroring
      // `CytoidGameCoreBridge.emitEngineError` on iOS.

      // When: parsed by the Dart envelope parser.
      final decoded = CytoidGameCoreEnvelope.fromJsonString(
        _swiftSynthEngineErrorJson,
      );

      // Then: the literal carries v2 schema and the parser accepts it.
      expect(
        _swiftSynthEngineErrorJson,
        contains('"schema":"cytoid.game-core.v2"'),
      );
      expect(decoded.schema, CytoidGameCoreEnvelope.currentSchema);
      expect(decoded.type, WireMessageType.engineError);
      final error = decoded.payload['error'] as Map<String, Object?>;
      expect(error['code'], 'runtime_exception');
    });
  });
}
