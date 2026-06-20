import 'dart:async';

import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  const channel = MethodChannel('cytoid/game_core_test');
  late StreamController<dynamic> events;
  late List<MethodCall> calls;

  setUp(() {
    events = StreamController<dynamic>.broadcast();
    calls = <MethodCall>[];
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(channel, (call) async {
          calls.add(call);
          switch (call.method) {
            case 'getEngineMode':
              return 'mock';
            case 'queryRuntimeStatus':
              return <String, Object?>{'state': 'busy', 'engine': 'mock'};
            case 'send':
            case 'ensureRuntimeStarted':
            case 'showGameSurface':
            case 'hideGameSurface':
              return null;
          }
          throw PlatformException(code: 'not_implemented');
        });
  });

  tearDown(() async {
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(channel, null);
    await events.close();
  });

  test('calls runtime, surface, engine mode, and send', () async {
    final client = CytoidGameCoreClient(
      methodChannel: channel,
      eventStream: events.stream,
    );

    await client.ensureRuntimeStarted();
    await client.showGameSurface();
    await client.hideGameSurface();
    expect(await client.getEngineMode(), 'mock');
    await client.send(
      CytoidGameCoreEnvelope.create(
        id: '1',
        type: WireMessageType.bridgePlayEnd,
      ),
    );

    expect(calls.map((call) => call.method), [
      'ensureRuntimeStarted',
      'queryRuntimeStatus',
      'showGameSurface',
      'hideGameSurface',
      'getEngineMode',
      'send',
    ]);
  });

  test('ensureRuntimeStarted returns while runtime is starting', () async {
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(channel, (call) async {
          calls.add(call);
          switch (call.method) {
            case 'ensureRuntimeStarted':
              return null;
            case 'queryRuntimeStatus':
              return <String, Object?>{'state': 'starting', 'engine': 'mock'};
          }
          return null;
        });

    final client = CytoidGameCoreClient(
      methodChannel: channel,
      eventStream: events.stream,
    );

    await client.ensureRuntimeStarted();
  });

  test('ping waits for matching pong', () async {
    final client = CytoidGameCoreClient(
      methodChannel: channel,
      eventStream: events.stream,
      livenessConfig: const CytoidGameCoreLivenessConfig(
        pingTimeout: Duration(seconds: 1),
      ),
    );

    final pingFuture = client.ping(text: 'hello');
    await Future<void>.delayed(Duration.zero);
    final sent = CytoidGameCoreEnvelope.fromJsonString(
      calls.single.arguments as String,
    );

    events.add(
      CytoidGameCoreEnvelope.create(
        id: sent.id,
        type: WireMessageType.gamePong,
        payload: {'text': 'hello'},
      ).toJsonString(),
    );

    final pong = await pingFuture;
    expect(pong.id, sent.id);
    expect(pong.payload, {'text': 'hello'});
  });

  test('startPlay waits for matching result', () async {
    final client = CytoidGameCoreClient(
      methodChannel: channel,
      eventStream: events.stream,
      livenessConfig: const CytoidGameCoreLivenessConfig(
        checkInterval: Duration(hours: 1),
        pingTimeout: Duration(seconds: 1),
      ),
    );

    final resultFuture = client.startPlay(
      GameLaunchPayload(
        levelMetaJson: '{}',
        selectedDifficulty: 'easy',
        assets: const GameLaunchAssets(
          vfsUri: 'file:///levels/test/',
          chartPath: 'chart.json',
          musicPath: 'music.mp3',
        ),
      ),
    );
    await Future<void>.delayed(Duration.zero);
    final sent = CytoidGameCoreEnvelope.fromJsonString(
      calls.single.arguments as String,
    );

    events.add(
      CytoidGameCoreEnvelope.create(
        id: sent.id,
        type: WireMessageType.gamePlayResult,
        payload: const {
          'completed': true,
          'failed': false,
          'usedAutoMod': false,
          'score': 7,
        },
      ).toJsonString(),
    );

    final result = await resultFuture;
    expect(result.completed, isTrue);
    expect(result.score, 7);
  });

  test('startPlay ends when session.ended arrives', () async {
    final client = CytoidGameCoreClient(
      methodChannel: channel,
      eventStream: events.stream,
      livenessConfig: const CytoidGameCoreLivenessConfig(
        checkInterval: Duration(hours: 1),
        pingTimeout: Duration(seconds: 1),
      ),
    );

    final resultFuture = client.startPlay(
      GameLaunchPayload(
        levelMetaJson: '{}',
        selectedDifficulty: 'easy',
        assets: const GameLaunchAssets(
          vfsUri: 'file:///levels/test/',
          chartPath: 'chart.json',
          musicPath: 'music.mp3',
        ),
      ),
    );
    await Future<void>.delayed(Duration.zero);
    final sent = CytoidGameCoreEnvelope.fromJsonString(
      calls.single.arguments as String,
    );

    events.add(
      CytoidGameCoreEnvelope.create(
        id: sent.id,
        type: WireMessageType.gamePlayEnded,
        payload: {'ended': true},
      ).toJsonString(),
    );

    await expectLater(
      resultFuture,
      throwsA(isA<CytoidGameCorePlayRouteEndedException>()),
    );
  });

  test('startPlay fails when game stops responding', () async {
    var runtimeState = 'busy';
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(channel, (call) async {
          calls.add(call);
          switch (call.method) {
            case 'getEngineMode':
              return 'mock';
            case 'queryRuntimeStatus':
              return <String, Object?>{'state': runtimeState, 'engine': 'mock'};
            case 'send':
            case 'ensureRuntimeStarted':
            case 'showGameSurface':
            case 'hideGameSurface':
              return null;
          }
          throw PlatformException(code: 'not_implemented');
        });

    final client = CytoidGameCoreClient(
      methodChannel: channel,
      eventStream: events.stream,
      livenessConfig: const CytoidGameCoreLivenessConfig(
        checkInterval: Duration(milliseconds: 20),
        pingTimeout: Duration(milliseconds: 50),
        maxConsecutiveFailures: 2,
      ),
    );

    final resultFuture = client.startPlay(
      GameLaunchPayload(
        levelMetaJson: '{}',
        selectedDifficulty: 'easy',
        assets: const GameLaunchAssets(
          vfsUri: 'file:///levels/test/',
          chartPath: 'chart.json',
          musicPath: 'music.mp3',
        ),
      ),
    );
    await Future<void>.delayed(Duration.zero);

    runtimeState = 'ready';

    await expectLater(
      resultFuture,
      throwsA(isA<CytoidGameCoreLostException>()),
    );
  });

  test('endPlayRoute waits for matching session.ended', () async {
    final client = CytoidGameCoreClient(
      methodChannel: channel,
      eventStream: events.stream,
    );

    final endFuture = client.endPlayRoute();
    await Future<void>.delayed(Duration.zero);
    final sent = CytoidGameCoreEnvelope.fromJsonString(
      calls.single.arguments as String,
    );

    events.add(
      CytoidGameCoreEnvelope.create(
        id: sent.id,
        type: WireMessageType.gamePlayEnded,
        payload: {'ended': true},
      ).toJsonString(),
    );

    await endFuture;
  });

  test('updateSettings sends settings.update and waits for ack', () async {
    final client = CytoidGameCoreClient(
      methodChannel: channel,
      eventStream: events.stream,
    );

    final updateFuture = client.updateSettings(
      const GameLaunchSettings(musicVolume: 0.4),
    );
    await Future<void>.delayed(Duration.zero);
    final sent = CytoidGameCoreEnvelope.fromJsonString(
      calls.single.arguments as String,
    );
    expect(sent.type, WireMessageType.bridgeSettingsUpdate);
    expect(sent.payload['musicVolume'], 0.4);

    events.add(
      CytoidGameCoreEnvelope.create(
        id: sent.id,
        type: WireMessageType.gameSettingsUpdated,
        payload: {'applied': true},
      ).toJsonString(),
    );

    await updateFuture;
  });

  test('filters log batch events', () async {
    final client = CytoidGameCoreClient(
      methodChannel: channel,
      eventStream: events.stream,
    );

    final batchExpectation = expectLater(
      client.logBatchEvents,
      emits(
        isA<CytoidGameCoreLogBatch>()
            .having((batch) => batch.reason, 'reason', 'trigger')
            .having(
              (batch) => batch.triggerLevel,
              'triggerLevel',
              CytoidGameCoreLogLevel.error,
            )
            .having((batch) => batch.truncated, 'truncated', isTrue)
            .having(
              (batch) => batch.logs.map((entry) => entry.message),
              'messages',
              ['before error', 'careful'],
            )
            .having(
              (batch) => batch.logs.last.level,
              'last level',
              CytoidGameCoreLogLevel.warning,
            )
            .having(
              (batch) => batch.logs.first.message,
              'first message',
              'before error',
            ),
      ),
    );

    events.add(
      CytoidGameCoreEnvelope.create(
        id: 'batch-1',
        type: WireMessageType.gameLogsBatch,
        payload: {
          'reason': 'trigger',
          'triggerLevel': 'error',
          'truncated': true,
          'logs': [
            {'level': 'log', 'message': 'before error'},
            {'level': 'warning', 'message': 'careful'},
          ],
        },
      ).toJsonString(),
    );

    await batchExpectation;
  });

  test('event stream rejects malformed event values', () {
    final client = CytoidGameCoreClient(
      methodChannel: channel,
      eventStream: events.stream,
    );

    expect(client.events, emitsError(isA<FormatException>()));
    events.add(<String, Object?>{});
  });
}
