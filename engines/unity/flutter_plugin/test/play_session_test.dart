import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';

const _primaryChannelName = 'cytoid/game_core';
const _waitForReadyChannelName = 'cytoid_game_core/waitForReady';

/// Loads a v2 fixture and strips the leading `_comment` field so model
/// factories see only their recognized fields.
Map<String, dynamic> _loadFixture(String name) {
  final file = File('test/fixtures/v2/$name');
  final raw = jsonDecode(file.readAsStringSync()) as Map<String, dynamic>;
  raw.remove('_comment');
  return raw;
}

SessionLaunchPayload _buildTestLaunch() {
  return SessionLaunchPayload.fromJson(
    _loadFixture('session_launch_payload.valid.json'),
  );
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  const primaryChannel = MethodChannel(_primaryChannelName);
  const readyChannel = MethodChannel(_waitForReadyChannelName);

  late StreamController<dynamic> events;
  late List<MethodCall> primaryCalls;
  late List<MethodCall> readyCalls;

  setUp(() {
    events = StreamController<dynamic>.broadcast();
    primaryCalls = <MethodCall>[];
    readyCalls = <MethodCall>[];

    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(primaryChannel, (call) async {
      primaryCalls.add(call);
      switch (call.method) {
        case 'ensureRuntimeStarted':
        case 'showGameSurface':
        case 'hideGameSurface':
        case 'send':
          return null;
        case 'getEngineMode':
          return 'mock';
        case 'queryRuntimeStatus':
          return <String, Object?>{'state': 'ready', 'engine': 'mock'};
      }
      throw PlatformException(code: 'not_implemented');
    });
  });

  tearDown(() async {
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(primaryChannel, null);
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(readyChannel, null);
    await events.close();
  });

  CytoidGameCoreEnvelope? sentEnvelopeOfType(String type) {
    final sendCalls = primaryCalls.where((c) => c.method == 'send').toList();
    for (final call in sendCalls.reversed) {
      final envelope = CytoidGameCoreEnvelope.fromJsonString(
        call.arguments as String,
      );
      if (envelope.type == type) return envelope;
    }
    return null;
  }

  /// Polls until [sentEnvelopeOfType] returns non-null for [type] or
  /// [deadline] elapses. Synchronizes the test on PlaySession's layered
  /// awaits without guessing microtask counts.
  Future<CytoidGameCoreEnvelope> awaitSentEnvelope(
    String type, {
    Duration deadline = const Duration(seconds: 2),
  }) async {
    final end = DateTime.now().add(deadline);
    while (DateTime.now().isBefore(end)) {
      final envelope = sentEnvelopeOfType(type);
      if (envelope != null) return envelope;
      await Future<void>.delayed(const Duration(milliseconds: 5));
    }
    throw TimeoutException('No envelope of type "$type" sent within $deadline');
  }

  group('PlaySession.run happy path', () {
    test('Android readyEvents fallback completes on session.result', () async {
      // No iOS helper registered → MissingPluginException → readyEvents path.
      final client = CytoidGameCoreClient(
        methodChannel: primaryChannel,
        eventStream: events.stream,
      );
      final session = PlaySession(client);

      // Repeatedly offer engine.ready so the first readyEvents subscriber
      // sees it regardless of when subscription happens relative to the
      // layered awaits in run().
      final readyEnvelope = CytoidGameCoreEnvelope.create(
        id: 'ready-1',
        type: WireMessageType.gameReady,
      ).toJsonString();
      final readyOffer = Timer.periodic(
        const Duration(milliseconds: 5),
        (_) => events.add(readyEnvelope),
      );

      final runFuture = session.run(
        launch: _buildTestLaunch(),
        readyTimeout: const Duration(seconds: 2),
      );

      try {
        final startEnvelope =
            await awaitSentEnvelope(WireMessageType.sessionStart);
        expect(startEnvelope.v, 2);
        expect(startEnvelope.payload['mode'], 'ranked');

        // Engine returns a typed completed result with matching session id.
        final resultJson = _loadFixture('session_result_payload.valid.json');
        resultJson['sessionId'] = startEnvelope.id;
        events.add(
          CytoidGameCoreEnvelope.create(
            id: startEnvelope.id,
            type: WireMessageType.sessionResult,
            payload: resultJson,
            v: 2,
          ).toJsonString(),
        );
      } finally {
        readyOffer.cancel();
      }

      final result = await runFuture.timeout(
        const Duration(seconds: 2),
        onTimeout: () => throw TimeoutException('run() never returned'),
      );
      expect(result.outcome.kind, OutcomePayload.completedKind);
      expect(result.mode, 'ranked');

      // hideGameSurface MUST be called in the finally block.
      expect(
        primaryCalls.map((c) => c.method),
        contains('hideGameSurface'),
      );
    });

    test('iOS waitForReady helper channel completes on session.result',
        () async {
      // Register a mock iOS helper that returns success.
      TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
          .setMockMethodCallHandler(readyChannel, (call) async {
        readyCalls.add(call);
        if (call.method == 'waitForReady') {
          return null; // success
        }
        throw PlatformException(code: 'not_implemented');
      });

      final client = CytoidGameCoreClient(
        methodChannel: primaryChannel,
        eventStream: events.stream,
      );
      final session = PlaySession(client);

      final runFuture = session.run(
        launch: _buildTestLaunch(),
        readyTimeout: const Duration(seconds: 5),
      );

      final startEnvelope =
          await awaitSentEnvelope(WireMessageType.sessionStart);

      // iOS helper was invoked with the timeout in seconds.
      expect(readyCalls.single.method, 'waitForReady');
      expect(readyCalls.single.arguments, 5);

      final resultJson = _loadFixture('session_result_payload.valid.json');
      resultJson['sessionId'] = startEnvelope.id;
      events.add(
        CytoidGameCoreEnvelope.create(
          id: startEnvelope.id,
          type: WireMessageType.sessionResult,
          payload: resultJson,
          v: 2,
        ).toJsonString(),
      );

      final result = await runFuture.timeout(
        const Duration(seconds: 2),
        onTimeout: () => throw TimeoutException('run() never returned'),
      );
      expect(result.outcome.kind, OutcomePayload.completedKind);

      // No engine.ready event was emitted; iOS path did not consult readyEvents.
      expect(
        primaryCalls.map((c) => c.method),
        contains('hideGameSurface'),
      );
    });
  });

  group('PlaySession.cancel', () {
    test('sends v2 session.cancel envelope with id and reason', () async {
      final client = CytoidGameCoreClient(
        methodChannel: primaryChannel,
        eventStream: events.stream,
      );
      final session = PlaySession(client);

      await session.cancel(sessionId: 'session-xyz', reason: 'userBack');

      final cancelEnvelope = sentEnvelopeOfType(WireMessageType.sessionCancel);
      expect(cancelEnvelope, isNotNull);
      expect(cancelEnvelope!.id, 'session-xyz');
      expect(cancelEnvelope.v, 2);
      expect(cancelEnvelope.payload, {'reason': 'userBack'});
    });
  });

  group('PlaySession.run ready timeout', () {
    test(
        'Android readyEvents path throws CytoidGameCoreTimeoutException '
        'AND hides the surface', () async {
      // No iOS helper → readyEvents path. No engine.ready ever emitted.
      final client = CytoidGameCoreClient(
        methodChannel: primaryChannel,
        eventStream: events.stream,
      );
      final session = PlaySession(client);

      final runFuture = session.run(
        launch: _buildTestLaunch(),
        readyTimeout: const Duration(milliseconds: 300),
      );

      await expectLater(
        runFuture,
        throwsA(isA<CytoidGameCoreTimeoutException>()),
      );

      // The surface MUST be hidden despite the ready wait throwing.
      expect(
        primaryCalls.map((c) => c.method),
        contains('hideGameSurface'),
      );
      // session.start MUST NOT have been sent.
      expect(
        sentEnvelopeOfType(WireMessageType.sessionStart),
        isNull,
      );
    });

    test(
        'iOS waitForReady helper timeout rethrows as '
        'CytoidGameCoreTimeoutException', () async {
      TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
          .setMockMethodCallHandler(readyChannel, (call) async {
        readyCalls.add(call);
        if (call.method == 'waitForReady') {
          throw PlatformException(
            code: 'waitForReadyTimeout',
            message: 'deadline elapsed',
          );
        }
        throw PlatformException(code: 'not_implemented');
      });

      final client = CytoidGameCoreClient(
        methodChannel: primaryChannel,
        eventStream: events.stream,
      );
      final session = PlaySession(client);

      final runFuture = session.run(
        launch: _buildTestLaunch(),
        readyTimeout: const Duration(seconds: 30),
      );

      await expectLater(
        runFuture,
        throwsA(
          isA<CytoidGameCoreTimeoutException>().having(
            (e) => e.timeout,
            'timeout carries the configured duration',
            const Duration(seconds: 30),
          ),
        ),
      );

      expect(
        primaryCalls.map((c) => c.method),
        contains('hideGameSurface'),
      );
    });
  });
}
