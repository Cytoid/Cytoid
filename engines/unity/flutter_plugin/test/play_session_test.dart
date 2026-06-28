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
  // Mutable runtime state returned by the mock queryRuntimeStatus handler.
  // Defaults to 'ready' (warm-resident); tests override to simulate
  // cold-start (starting) or never-ready (timeout).
  late String statusState;

  setUp(() {
    events = StreamController<dynamic>.broadcast();
    primaryCalls = <MethodCall>[];
    readyCalls = <MethodCall>[];
    statusState = 'ready';

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
              return <String, Object?>{
                'state': statusState,
                'engine': 'mock',
                'mode': 'mock',
                'generation': 1,
                if (statusState == 'failed')
                  'error': {
                    'code': 'runtime_failed',
                    'message': 'Runtime failed',
                  },
              };
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
    test('Android warm-resident ready: resolves via queryStatus with no '
        'engine.ready event (second-session regression)', () async {
      // Reproduces the second-session timeout: the warm-resident runtime
      // is already READY after Activity resume, which fires NO engine.ready
      // event. The Android fallback must consult queryStatus rather than
      // wait on an event that will never arrive.
      // (setUp leaves statusState = 'ready'.)
      final client = CytoidGameCoreClient(
        methodChannel: primaryChannel,
        eventStream: events.stream,
      );
      final session = PlaySession(client);

      final runFuture = session.run(
        launch: _buildTestLaunch(),
        readyTimeout: const Duration(seconds: 2),
      );

      final startEnvelope = await awaitSentEnvelope(
        WireMessageType.sessionStart,
      );
      expect(startEnvelope.schema, CytoidGameCoreEnvelope.currentSchema);
      expect(startEnvelope.payload['mode'], 'ranked');

      // Engine returns a typed completed result with matching session id.
      final resultJson = _loadFixture('session_result_payload.valid.json');
      resultJson['sessionId'] = startEnvelope.id;
      events.add(
        CytoidGameCoreEnvelope.create(
          id: startEnvelope.id,
          type: WireMessageType.sessionResult,
          payload: resultJson,
        ).toJsonString(),
      );

      final result = await runFuture.timeout(
        const Duration(seconds: 2),
        onTimeout: () => throw TimeoutException('run() never returned'),
      );
      expect(result.outcome.kind, OutcomePayload.completedKind);
      expect(result.mode, 'ranked');

      // No engine.ready was emitted — readiness came from queryStatus.
      expect(sentEnvelopeOfType(WireMessageType.engineReady), isNull);
      // hideGameSurface MUST be called in the finally block.
      expect(primaryCalls.map((c) => c.method), contains('hideGameSurface'));
    });

    test(
      'Android cold-start: queryStatus poll loop observes starting→ready',
      () async {
        // First-session cold start: the engine takes a moment to boot. The
        // poll loop must keep probing until queryStatus flips to ready.
        statusState = 'starting';
        final readyFlip = Timer(
          const Duration(milliseconds: 350),
          () => statusState = 'ready',
        );

        final client = CytoidGameCoreClient(
          methodChannel: primaryChannel,
          eventStream: events.stream,
        );
        final session = PlaySession(client);

        final runFuture = session.run(
          launch: _buildTestLaunch(),
          readyTimeout: const Duration(seconds: 5),
        );

        try {
          final startEnvelope = await awaitSentEnvelope(
            WireMessageType.sessionStart,
          );

          final resultJson = _loadFixture('session_result_payload.valid.json');
          resultJson['sessionId'] = startEnvelope.id;
          events.add(
            CytoidGameCoreEnvelope.create(
              id: startEnvelope.id,
              type: WireMessageType.sessionResult,
              payload: resultJson,
            ).toJsonString(),
          );

          final result = await runFuture.timeout(
            const Duration(seconds: 5),
            onTimeout: () => throw TimeoutException('run() never returned'),
          );
          expect(result.outcome.kind, OutcomePayload.completedKind);

          // queryStatus was polled more than once before readiness.
          final queryCount = primaryCalls
              .where((c) => c.method == 'queryRuntimeStatus')
              .length;
          expect(queryCount, greaterThan(1));
        } finally {
          readyFlip.cancel();
        }
      },
    );

    test(
      'run rejects a second concurrent run with StateError (single active slot)',
      () async {
        final client = CytoidGameCoreClient(
          methodChannel: primaryChannel,
          eventStream: events.stream,
        );
        final session = PlaySession(client);

        // _activeSessionId is set synchronously before the first await.
        final first = session.run(
          launch: _buildTestLaunch(),
          readyTimeout: const Duration(seconds: 5),
        );

        await expectLater(
          () => session.run(launch: _buildTestLaunch()),
          throwsA(isA<StateError>()),
        );

        // Let the first run complete so it doesn't dangle into sibling tests.
        final startEnvelope = await awaitSentEnvelope(WireMessageType.sessionStart);
        final resultJson = _loadFixture('session_result_payload.valid.json');
        resultJson['sessionId'] = startEnvelope.id;
        events.add(
          CytoidGameCoreEnvelope.create(
            id: startEnvelope.id,
            type: WireMessageType.sessionResult,
            payload: resultJson,
          ).toJsonString(),
        );
        await first.timeout(const Duration(seconds: 2));
      },
    );

    test('iOS waitForReady helper channel completes on session.result', () async {
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

      final startEnvelope = await awaitSentEnvelope(
        WireMessageType.sessionStart,
      );

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
          ).toJsonString(),
        );

      final result = await runFuture.timeout(
        const Duration(seconds: 2),
        onTimeout: () => throw TimeoutException('run() never returned'),
      );
      expect(result.outcome.kind, OutcomePayload.completedKind);

      // No engine.ready event was emitted; iOS path did not consult readyEvents.
      expect(primaryCalls.map((c) => c.method), contains('hideGameSurface'));
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
      expect(cancelEnvelope.schema, CytoidGameCoreEnvelope.currentSchema);
      expect(cancelEnvelope.payload, {'reason': 'userBack'});
    });

    test('cancel without id cancels the active run session', () async {
      final client = CytoidGameCoreClient(
        methodChannel: primaryChannel,
        eventStream: events.stream,
      );
      final session = PlaySession(client);

      final runFuture = session.run(
        launch: _buildTestLaunch(),
        readyTimeout: const Duration(seconds: 2),
      );

      final startEnvelope = await awaitSentEnvelope(
        WireMessageType.sessionStart,
      );
      await session.cancel(reason: 'hostNavigation');

      final cancelEnvelope = sentEnvelopeOfType(WireMessageType.sessionCancel);
      expect(cancelEnvelope, isNotNull);
      expect(cancelEnvelope!.id, startEnvelope.id);
      expect(cancelEnvelope.payload, {'reason': 'hostNavigation'});

      final resultJson = _loadFixture('session_result_payload.valid.json');
      resultJson['sessionId'] = startEnvelope.id;
      events.add(
        CytoidGameCoreEnvelope.create(
            id: startEnvelope.id,
            type: WireMessageType.sessionResult,
            payload: resultJson,
          ).toJsonString(),
        );

      await runFuture.timeout(const Duration(seconds: 2));
    });
  });

  group('PlaySession.run ready timeout', () {
    test('Android poll path throws CytoidGameCoreTimeoutException '
        'AND hides the surface', () async {
      // Runtime never reaches ready — poll loops until the deadline.
      statusState = 'starting';
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
      expect(primaryCalls.map((c) => c.method), contains('hideGameSurface'));
      // session.start MUST NOT have been sent.
      expect(sentEnvelopeOfType(WireMessageType.sessionStart), isNull);
    });

    test('Android poll surfaces a failed runtime immediately instead of '
        'burning the timeout', () async {
      // Runtime is FAILED: the poll must throw the typed failure right
      // away, not wait for the deadline and report a misleading timeout.
      statusState = 'failed';
      final client = CytoidGameCoreClient(
        methodChannel: primaryChannel,
        eventStream: events.stream,
      );
      final session = PlaySession(client);

      final runFuture = session.run(
        launch: _buildTestLaunch(),
        readyTimeout: const Duration(seconds: 30),
      );

      await expectLater(runFuture, throwsA(isA<PlatformException>()));

      expect(primaryCalls.map((c) => c.method), contains('hideGameSurface'));
      expect(sentEnvelopeOfType(WireMessageType.sessionStart), isNull);
    });

    test('iOS waitForReady helper timeout rethrows as '
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

      expect(primaryCalls.map((c) => c.method), contains('hideGameSurface'));
    });
  });

  group('PlaySession.run session.failed', () {
    // Helper: emit a `session.failed` envelope on the broadcast event stream
    // matching the supplied session id. Mirrors how the native bridge
    // synthesizes a runtime-death envelope for an active session.
    void emitSessionFailed(String sessionId, {String code = 'runtime_unreachable'}) {
      events.add(
        CytoidGameCoreEnvelope.create(
          id: sessionId,
          type: WireMessageType.sessionFailed,
          payload: {
            'sessionId': sessionId,
            'error': {
              'code': code,
              'message': 'Unity process gone',
            },
            'timestamp': DateTime.now().millisecondsSinceEpoch,
          },
        ).toJsonString(),
      );
    }

    test('throws CytoidGameCoreSessionFailedException when session.failed '
        'envelope arrives for the active session', () async {
      final client = CytoidGameCoreClient(
        methodChannel: primaryChannel,
        eventStream: events.stream,
      );
      final session = PlaySession(client);

      final runFuture = session.run(
        launch: _buildTestLaunch(),
        readyTimeout: const Duration(seconds: 2),
      );

      final startEnvelope = await awaitSentEnvelope(
        WireMessageType.sessionStart,
      );

      emitSessionFailed(startEnvelope.id);

      await expectLater(
        runFuture.timeout(
          const Duration(seconds: 2),
          onTimeout: () => throw TimeoutException('run() never threw'),
        ),
        throwsA(
          isA<CytoidGameCoreSessionFailedException>()
              .having((e) => e.sessionId, 'sessionId', startEnvelope.id)
              .having((e) => e.code, 'code', 'runtime_unreachable')
              .having((e) => e.message, 'message', 'Unity process gone'),
        ),
      );
    });

    test('finally block still hides the surface when run throws on '
        'session.failed', () async {
      final client = CytoidGameCoreClient(
        methodChannel: primaryChannel,
        eventStream: events.stream,
      );
      final session = PlaySession(client);

      final runFuture = session.run(
        launch: _buildTestLaunch(),
        readyTimeout: const Duration(seconds: 2),
      );

      final startEnvelope = await awaitSentEnvelope(
        WireMessageType.sessionStart,
      );

      emitSessionFailed(startEnvelope.id);

      await expectLater(
        runFuture.timeout(
          const Duration(seconds: 2),
          onTimeout: () => throw TimeoutException('run() never threw'),
        ),
        throwsA(isA<CytoidGameCoreSessionFailedException>()),
      );

      // The outer finally at play_session.dart L67-75 fires even when run
      // throws; the surface MUST be hidden.
      expect(primaryCalls.map((c) => c.method), contains('hideGameSurface'));
    });

    test('active session id is cleared after the throw — cancel() without '
        'explicit id throws StateError', () async {
      final client = CytoidGameCoreClient(
        methodChannel: primaryChannel,
        eventStream: events.stream,
      );
      final session = PlaySession(client);

      final runFuture = session.run(
        launch: _buildTestLaunch(),
        readyTimeout: const Duration(seconds: 2),
      );

      final startEnvelope = await awaitSentEnvelope(
        WireMessageType.sessionStart,
      );

      emitSessionFailed(startEnvelope.id);

      await expectLater(
        runFuture.timeout(
          const Duration(seconds: 2),
          onTimeout: () => throw TimeoutException('run() never threw'),
        ),
        throwsA(isA<CytoidGameCoreSessionFailedException>()),
      );

      // The inner finally at play_session.dart L70-74 clears
      // _activeSessionId. There is no public getter, so verify indirectly:
      // cancel() without an explicit sessionId throws StateError iff no
      // session is active.
      await expectLater(
        session.cancel(reason: 'surfaceLost'),
        throwsA(isA<StateError>()),
      );
    });

    test('lockstep: late session.result after session.failed is ignored — '
        'exactly one terminal fires', () async {
      final client = CytoidGameCoreClient(
        methodChannel: primaryChannel,
        eventStream: events.stream,
      );
      final session = PlaySession(client);

      final runFuture = session.run(
        launch: _buildTestLaunch(),
        readyTimeout: const Duration(seconds: 2),
      );

      final startEnvelope = await awaitSentEnvelope(
        WireMessageType.sessionStart,
      );

      // session.failed fires FIRST — completes the future with the error.
      emitSessionFailed(startEnvelope.id);
      // Late session.result for the SAME session id arrives afterwards. The
      // `!completer.isCompleted` guard at play_session.dart must drop it; the
      // outcome stays a session.failed throw, never a successful result.
      final resultJson = _loadFixture('session_result_payload.valid.json');
      resultJson['sessionId'] = startEnvelope.id;
      events.add(
        CytoidGameCoreEnvelope.create(
          id: startEnvelope.id,
          type: WireMessageType.sessionResult,
          payload: resultJson,
        ).toJsonString(),
      );

      await expectLater(
        runFuture.timeout(
          const Duration(seconds: 2),
          onTimeout: () => throw TimeoutException('run() never threw'),
        ),
        throwsA(
          isA<CytoidGameCoreSessionFailedException>()
              .having((e) => e.code, 'code', 'runtime_unreachable'),
        ),
      );
    });
  });
}
