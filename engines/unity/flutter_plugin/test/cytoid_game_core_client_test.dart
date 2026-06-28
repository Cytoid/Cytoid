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
              return <String, Object?>{
                'state': 'busy',
                'engine': 'mock',
                'mode': 'mock',
                'generation': 1,
                'activeSessionId': 'session-active',
              };
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
        type: WireMessageType.sessionCancel,
        payload: {'reason': 'userBack'},
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
              return <String, Object?>{
                'state': 'starting',
                'engine': 'mock',
                'mode': 'mock',
                'generation': 1,
              };
          }
          return null;
        });

    final client = CytoidGameCoreClient(
      methodChannel: channel,
      eventStream: events.stream,
    );

    await client.ensureRuntimeStarted();
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
        type: WireMessageType.logsBatch,
        payload: {
          'reason': 'trigger',
          'triggerLevel': 'error',
          'timestamp': 1782148800000,
          'truncated': true,
          'logs': [
            {
              'level': 'info',
              'message': 'before error',
              'timestamp': 1782148799000,
              'sessionId': 'session-1',
            },
            {
              'level': 'warning',
              'message': 'careful',
              'timestamp': 1782148799500,
              'sessionId': 'session-1',
            },
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
