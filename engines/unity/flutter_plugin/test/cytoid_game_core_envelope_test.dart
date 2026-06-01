import 'dart:typed_data';

import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('encodes and decodes envelopes', () {
    final envelope = CytoidGameCoreEnvelope.create(
      id: 'abc',
      type: WireMessageType.bridgePing,
      payload: {'text': 'hello'},
    );

    final decoded = CytoidGameCoreEnvelope.fromJsonString(envelope.toJsonString());

    expect(decoded.v, CytoidGameCoreEnvelope.currentVersion);
    expect(decoded.id, 'abc');
    expect(decoded.type, WireMessageType.bridgePing);
    expect(decoded.payload, {'text': 'hello'});
  });

  test('rejects malformed envelopes', () {
    expect(
      () => CytoidGameCoreEnvelope.fromJsonString('[]'),
      throwsA(isA<FormatException>()),
    );
    expect(
      () => CytoidGameCoreEnvelope.fromJson({
        'v': '1',
        'id': 'abc',
        'type': 'game.ready',
        'payload': <String, Object?>{},
      }),
      throwsA(isA<FormatException>()),
    );
  });

  test('round-trips launch and result payloads', () {
    final launch = GameLaunchPayload(
      levelMetaJson: '{"id":"level"}',
      selectedDifficulty: 'hard',
      musicBytes: Uint8List.fromList([1, 2, 3]),
      settings: const GameLaunchSettings(
        noteSize: 1.2,
        hitboxSizes: {'0': 2, '4': 1},
        holdHitSoundTiming: 'Both',
        graphicsQuality: 'High',
      ),
      assets: const GameLaunchAssets(chartUri: 'file:///chart.txt'),
    );

    final decodedLaunch = GameLaunchPayload.fromJson(launch.toJson());

    expect(decodedLaunch.levelMetaJson, '{"id":"level"}');
    expect(decodedLaunch.selectedDifficulty, 'hard');
    expect(decodedLaunch.musicBytes, [1, 2, 3]);
    expect(decodedLaunch.settings?.noteSize, 1.2);
    expect(decodedLaunch.settings?.hitboxSizes, {'0': 2, '4': 1});
    expect(decodedLaunch.settings?.holdHitSoundTiming, 'Both');
    expect(decodedLaunch.settings?.graphicsQuality, 'High');
    expect(decodedLaunch.assets?.chartUri, 'file:///chart.txt');

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
}
