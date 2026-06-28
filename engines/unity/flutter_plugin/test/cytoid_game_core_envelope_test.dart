import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('encodes and decodes envelopes', () {
    final envelope = CytoidGameCoreEnvelope.create(
      id: 'abc',
      type: WireMessageType.healthCheck,
      payload: {'text': 'hello'},
    );

    final decoded = CytoidGameCoreEnvelope.fromJsonString(
      envelope.toJsonString(),
    );

    expect(decoded.schema, CytoidGameCoreEnvelope.currentSchema);
    expect(decoded.id, 'abc');
    expect(decoded.type, WireMessageType.healthCheck);
    expect(decoded.payload, {'text': 'hello'});
  });

  test('rejects malformed envelopes', () {
    expect(
      () => CytoidGameCoreEnvelope.fromJsonString('[]'),
      throwsA(isA<FormatException>()),
    );
    expect(
      () => CytoidGameCoreEnvelope.fromJson({
        'schema': 'cytoid.game-core.v1',
        'id': 'abc',
        'type': 'engine.ready',
        'payload': <String, Object?>{},
      }),
      throwsA(isA<FormatException>()),
    );
  });

  test('round-trips launch and result payloads', () {
    final launch = GameLaunchPayload(
      levelMetaJson: '{"id":"level"}',
      selectedDifficulty: 'hard',
      assets: const GameLaunchAssets(
        vfsUri: 'file:///levels/level/',
        chartPath: 'charts/hard.json',
        musicPath: 'audio/song.ogg',
      ),
      settings: const GameLaunchSettings(
        noteSize: 1.2,
        hitboxSizes: {'0': 2, '4': 1},
        holdHitSoundTiming: 'Both',
        graphicsQuality: 'High',
      ),
    );

    final decodedLaunch = GameLaunchPayload.fromJson(launch.toJson());

    expect(decodedLaunch.levelMetaJson, '{"id":"level"}');
    expect(decodedLaunch.selectedDifficulty, 'hard');
    expect(decodedLaunch.settings?.noteSize, 1.2);
    expect(decodedLaunch.settings?.hitboxSizes, {'0': 2, '4': 1});
    expect(decodedLaunch.settings?.holdHitSoundTiming, 'Both');
    expect(decodedLaunch.settings?.graphicsQuality, 'High');
    expect(decodedLaunch.assets.vfsUri, 'file:///levels/level/');
    expect(decodedLaunch.assets.chartPath, 'charts/hard.json');
    expect(decodedLaunch.assets.musicPath, 'audio/song.ogg');

    final result = GameResultPayload.fromJson({
      'completed': true,
      'failed': false,
      'usedAutoMod': false,
      'score': 123456,
      'accuracy': 99.5,
      'gradeCounts': {'perfect': 10},
    });

    expect(result.completed, isTrue);
    expect(result.score, 123456);
    expect(result.accuracy, 99.5);
    expect(result.gradeCounts, {'perfect': 10});
  });

  test('reads play events and reports json and binary sizes', () {
    final result = GameResultPayload.fromJson({
      'completed': true,
      'failed': false,
      'usedAutoMod': false,
      'playEvents': [
        {'t': 1000, 'f': 0, 'p': 'down', 'x': 32768, 'y': 16384},
        {'t': 1016, 'f': 0, 'p': 'move', 'x': 33000, 'y': 16400},
        {'t': 1048, 'f': 0, 'p': 'up', 'x': 33120, 'y': 16480},
      ],
    });

    expect(result.playEvents, hasLength(3));
    expect(result.playEventJsonBytes, greaterThan(0));
    expect(result.playEventBinaryBytes, greaterThan(0));
    expect(result.playEventBinaryBytes, lessThan(result.playEventJsonBytes));
    expect(result.toJson()['playEvents'], result.playEvents);
  });

  test('binary codec rejects unknown play event phase at encode time', () {
    // The Unity core only ever emits down/move/up. A payload that carries any
    // other phase (protocol drift) must surface immediately when the compact
    // binary representation is derived, rather than silently mapping to 0.
    final result = GameResultPayload.fromJson({
      'completed': true,
      'failed': false,
      'usedAutoMod': false,
      'playEvents': [
        {'t': 1000, 'f': 0, 'p': 'drag', 'x': 32768, 'y': 16384},
      ],
    });

    expect(
      () => result.playEventBinary,
      throwsA(isA<FormatException>()),
    );
  });

  test('rejects missing or malformed launch assets', () {
    expect(
      () => GameLaunchPayload.fromJson({
        'levelMetaJson': '{}',
        'selectedDifficulty': 'hard',
      }),
      throwsA(
        isA<FormatException>().having(
          (error) => error.message,
          'message',
          contains("Invalid or missing 'assets' field"),
        ),
      ),
    );

    expect(
      () => GameLaunchPayload.fromJson({
        'levelMetaJson': '{}',
        'selectedDifficulty': 'hard',
        'assets': {
          'vfsUri': 'file:///levels/level/',
          'chartPath': 'charts/hard.json',
        },
      }),
      throwsA(
        isA<FormatException>().having(
          (error) => error.message,
          'message',
          contains(
            'GameLaunchAssets.fromJson: missing or invalid field musicPath',
          ),
        ),
      ),
    );
  });
}
