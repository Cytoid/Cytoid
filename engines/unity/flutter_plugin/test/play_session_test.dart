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
    void emitSessionFailed(
      String sessionId, {
      String code = 'runtime_unreachable',
    }) {
      events.add(
        CytoidGameCoreEnvelope.create(
          id: sessionId,
          type: WireMessageType.sessionFailed,
          payload: {
            'sessionId': sessionId,
            'error': {'code': code, 'message': 'Unity process gone'},
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
          isA<CytoidGameCoreSessionFailedException>().having(
            (e) => e.code,
            'code',
            'runtime_unreachable',
          ),
        ),
      );
    });
  });

  group('PlaySession.run stream error lockstep', () {
    // Regression: the main subscription's onError MUST guard against
    // completer.isCompleted. A stream error arriving after session.result /
    // session.failed / watchdog-timeout completed the completer MUST be
    // silently dropped — calling completeError on an already-completed
    // completer throws StateError, which would propagate out of run() and
    // mask the real result/failure.
    //
    // Triggering the race deterministically requires a SYNC broadcast
    // controller: with sync=true, events.addError dispatches to listeners
    // SYNCHRONOUSLY (not via microtask). Combined with events.add firing
    // onData synchronously, we can deliver session.result (completing the
    // completer) and then immediately an error in the SAME synchronous
    // call frame — before run()'s finally block has a chance to cancel the
    // subscription. Without the production guard, the synchronous
    // completeError-on-completed-completer throws StateError out of
    // events.addError; with the guard, the error is silently dropped.
    test('late stream error after session.result is silently dropped — '
        'no StateError', () async {
      // Local sync broadcast controller — does NOT replace the suite-level
      // `events` (which tearDown still owns). Bypasses async microtask
      // reordering that would otherwise cancel S1 before M2 fires.
      final syncEvents = StreamController<dynamic>.broadcast(sync: true);
      addTearDown(syncEvents.close);

      final client = CytoidGameCoreClient(
        methodChannel: primaryChannel,
        eventStream: syncEvents.stream,
      );
      final session = PlaySession(client);

      final runFuture = session.run(
        launch: _buildTestLaunch(),
        readyTimeout: const Duration(seconds: 2),
      );

      final startEnvelope = await awaitSentEnvelope(
        WireMessageType.sessionStart,
      );

      // Complete the session cleanly with a valid session.result. add() on a
      // SYNC broadcast controller delivers onData IMMEDIATELY — the
      // subscription's onData fires synchronously here, completing the
      // completer before this line returns.
      final resultJson = _loadFixture('session_result_payload.valid.json');
      resultJson['sessionId'] = startEnvelope.id;
      syncEvents.add(
        CytoidGameCoreEnvelope.create(
          id: startEnvelope.id,
          type: WireMessageType.sessionResult,
          payload: resultJson,
        ).toJsonString(),
      );

      // Inject a stream error in the SAME synchronous call frame. The
      // subscription is STILL alive (run()'s finally block hasn't run yet
      // — it's scheduled as a microtask continuation of completer.future).
      // Without the production guard, this throws StateError out of addError.
      Object? addErrorThrown;
      try {
        syncEvents.addError(StateError('late stream error after completion'));
      } catch (e) {
        addErrorThrown = e;
      }

      // The error from inside the listener dispatcher is NOT propagated via
      // addError's synchronous return (the dispatcher catches it and routes
      // to the subscription's zone). Drain any microtasks so a deferred
      // StateError reaches the test framework's uncaught-error sink.
      await Future<void>.delayed(const Duration(milliseconds: 50));

      expect(
        addErrorThrown,
        isNull,
        reason:
            'stream listener dispatcher must not propagate the '
            'completeError-on-completed StateError synchronously; got: '
            '$addErrorThrown',
      );

      // run() must return the result cleanly, NOT a StateError. Without the
      // guard, the uncaught StateError masks the result.
      final result = await runFuture.timeout(
        const Duration(seconds: 2),
        onTimeout: () => throw TimeoutException('run() never returned'),
      );
      expect(result.outcome.kind, OutcomePayload.completedKind);

      // Surface MUST still be hidden via the finally block.
      expect(primaryCalls.map((c) => c.method), contains('hideGameSurface'));
    });
  });

  // Millisecond-scale watchdog config for deterministic tests (NOT real 30s;
  // NOT FakeAsync — the latter is scope-OUT per the plan). Hundreds of ms, not
  // single-digit ms, to avoid CI flakiness.
  final msConfig = HealthCheckWatchdogConfig(
    firstResponseTimeout: const Duration(milliseconds: 2000),
    steadyResponseTimeout: const Duration(milliseconds: 200),
    pollInterval: const Duration(milliseconds: 100),
  );

  group('PlaySession.run health.check watchdog', () {
    /// Replaces the default mock `primaryChannel` handler with one that
    /// delegates the standard setUp behavior but additionally routes every
    /// `send` envelope through [onSend]. Tests use this to intercept
    /// `health.check` envelopes and decide whether to emit a matching
    /// `health.ok` (or to record timing).
    void installSendInterceptor({
      required void Function(CytoidGameCoreEnvelope env) onSend,
    }) {
      TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
          .setMockMethodCallHandler(primaryChannel, (call) async {
            primaryCalls.add(call);
            switch (call.method) {
              case 'ensureRuntimeStarted':
              case 'showGameSurface':
              case 'hideGameSurface':
                return null;
              case 'send':
                final env = CytoidGameCoreEnvelope.fromJsonString(
                  call.arguments as String,
                );
                onSend(env);
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
    }

    int healthCheckSendCount() {
      var count = 0;
      for (final call in primaryCalls) {
        if (call.method != 'send') continue;
        final env = CytoidGameCoreEnvelope.fromJsonString(
          call.arguments as String,
        );
        if (env.type == WireMessageType.healthCheck) count += 1;
      }
      return count;
    }

    Future<void> waitForHealthCheckCount(
      int target, {
      Duration deadline = const Duration(seconds: 2),
    }) async {
      final end = DateTime.now().add(deadline);
      while (DateTime.now().isBefore(end)) {
        if (healthCheckSendCount() >= target) return;
        await Future<void>.delayed(const Duration(milliseconds: 5));
      }
      throw TimeoutException(
        'Never saw $target health.check envelopes within $deadline',
      );
    }

    void emitHealthOk(String checkId) {
      events.add(
        CytoidGameCoreEnvelope.create(
          id: checkId,
          type: WireMessageType.healthOk,
          payload: const {'state': 'ready'},
        ).toJsonString(),
      );
    }

    test('watchdog: first check sends, engine responds, then session.result '
        'completes', () async {
      installSendInterceptor(
        onSend: (env) {
          if (env.type == WireMessageType.healthCheck) {
            Future.microtask(() => emitHealthOk(env.id));
          }
        },
      );

      final client = CytoidGameCoreClient(
        methodChannel: primaryChannel,
        eventStream: events.stream,
      );
      final session = PlaySession(client, watchdogConfig: msConfig);

      final runFuture = session.run(
        launch: _buildTestLaunch(),
        readyTimeout: const Duration(seconds: 2),
      );

      final startEnvelope = await awaitSentEnvelope(
        WireMessageType.sessionStart,
      );

      // Engine acknowledges session start (non-terminal — fresh lastInboundAt).
      events.add(
        CytoidGameCoreEnvelope.create(
          id: startEnvelope.id,
          type: WireMessageType.sessionStarted,
          payload: {'sessionId': startEnvelope.id},
        ).toJsonString(),
      );

      // Wait for the first health.check; the mock auto-emits health.ok.
      await awaitSentEnvelope(WireMessageType.healthCheck);

      // Emit session.result to complete the run before any unskipped second
      // check could fire.
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

      // Exactly ONE health.check was sent. After the first response, the next
      // scheduled check is skipped via the last-message shortcut (session.started
      // and health.ok keep lastInboundAt fresh); session.result terminates
      // before any unskipped second fire.
      expect(healthCheckSendCount(), 1);

      // hideGameSurface MUST be called in the finally block.
      expect(primaryCalls.map((c) => c.method), contains('hideGameSurface'));
    });

    test(
      'watchdog: first check response timeout throws '
      'CytoidGameCoreSessionFailedException(code=runtime_unreachable)',
      () async {
        // No health.ok response — the watchdog's first check will time out.
        installSendInterceptor(onSend: (_) {});

        final client = CytoidGameCoreClient(
          methodChannel: primaryChannel,
          eventStream: events.stream,
        );
        final session = PlaySession(client, watchdogConfig: msConfig);

        final runFuture = session.run(
          launch: _buildTestLaunch(),
          readyTimeout: const Duration(seconds: 2),
        );

        // Wait for the first health.check to confirm the watchdog armed and ran.
        await awaitSentEnvelope(WireMessageType.sessionStart);
        await awaitSentEnvelope(WireMessageType.healthCheck);

        await expectLater(
          runFuture.timeout(
            const Duration(seconds: 5),
            onTimeout: () => throw TimeoutException('run() never threw'),
          ),
          throwsA(
            isA<CytoidGameCoreSessionFailedException>()
                .having((e) => e.code, 'code', 'runtime_unreachable')
                .having(
                  (e) => e.message,
                  'message mentions health.check',
                  contains('health.check'),
                )
                .having(
                  (e) => e.message,
                  'message mentions watchdog',
                  contains('watchdog'),
                ),
          ),
        );

        // hideGameSurface MUST be called in the finally block.
        expect(primaryCalls.map((c) => c.method), contains('hideGameSurface'));
      },
    );

    test(
      'watchdog: last-message shortcut skips health.check while logs.flow',
      () async {
        installSendInterceptor(
          onSend: (env) {
            if (env.type == WireMessageType.healthCheck) {
              Future.microtask(() => emitHealthOk(env.id));
            }
          },
        );

        final client = CytoidGameCoreClient(
          methodChannel: primaryChannel,
          eventStream: events.stream,
        );
        final session = PlaySession(client, watchdogConfig: msConfig);

        final runFuture = session.run(
          launch: _buildTestLaunch(),
          readyTimeout: const Duration(seconds: 2),
        );

        final startEnvelope = await awaitSentEnvelope(
          WireMessageType.sessionStart,
        );
        events.add(
          CytoidGameCoreEnvelope.create(
            id: startEnvelope.id,
            type: WireMessageType.sessionStarted,
            payload: {'sessionId': startEnvelope.id},
          ).toJsonString(),
        );

        // First health.check fires at pollInterval (~100ms) and is NEVER skipped.
        await awaitSentEnvelope(WireMessageType.healthCheck);
        // Let the mock's microtask health.ok land + scheduleNextCheck arm.
        await Future<void>.delayed(const Duration(milliseconds: 20));

        // Emit logs.batch every 50ms (faster than pollInterval=100ms) — this
        // keeps lastInboundAt fresh and forces every subsequent scheduled check
        // to be skipped via the last-message shortcut.
        //
        // CRITICAL: real logs.batch envelopes carry a fresh UUID id emitted by
        // GameLogBridge.cs (NOT the sessionId). Using startEnvelope.id here
        // would bypass the production code path: lastInboundAt must refresh
        // from ANY non-terminal envelope regardless of its id. The UUID id
        // proves the shortcut works against real envelope shapes.
        var logsBatchCounter = 0;
        final logsTimer = Timer.periodic(const Duration(milliseconds: 50), (_) {
          logsBatchCounter += 1;
          events.add(
            CytoidGameCoreEnvelope.create(
              id:
                  'log-batch-$logsBatchCounter-'
                  '${DateTime.now().microsecondsSinceEpoch}',
              type: WireMessageType.logsBatch,
              payload: const {'entries': []},
            ).toJsonString(),
          );
        });

        try {
          // 4x pollInterval elapsed with logs flowing — only the FIRST check
          // should have been sent; all subsequent scheduled checks were skipped.
          await Future<void>.delayed(const Duration(milliseconds: 400));
          expect(healthCheckSendCount(), 1);
        } finally {
          logsTimer.cancel();
        }

        // Logs stopped — the next scheduled check is no longer skipped because
        // lastInboundAt is now staler than pollInterval.
        await waitForHealthCheckCount(2);

        // Cleanup: emit session.result so run() completes cleanly.
        final resultJson = _loadFixture('session_result_payload.valid.json');
        resultJson['sessionId'] = startEnvelope.id;
        events.add(
          CytoidGameCoreEnvelope.create(
            id: startEnvelope.id,
            type: WireMessageType.sessionResult,
            payload: resultJson,
          ).toJsonString(),
        );
        await runFuture.timeout(
          const Duration(seconds: 2),
          onTimeout: () => throw TimeoutException('run() never returned'),
        );
      },
    );

    test('watchdog: second check uses steadyResponseTimeout (shorter) not '
        'firstResponseTimeout', () async {
      var healthCheckCount = 0;
      DateTime? secondCheckSentAt;
      installSendInterceptor(
        onSend: (env) {
          if (env.type != WireMessageType.healthCheck) return;
          healthCheckCount += 1;
          if (healthCheckCount == 1) {
            // Respond promptly to mark the first check as completed — subsequent
            // checks use steadyResponseTimeout.
            Future.microtask(() => emitHealthOk(env.id));
          } else {
            // Second check: record send time, do NOT respond. The watchdog's
            // steadyResponseTimeout timer will fire and complete the session
            // with runtime_unreachable.
            secondCheckSentAt ??= DateTime.now();
          }
        },
      );

      final client = CytoidGameCoreClient(
        methodChannel: primaryChannel,
        eventStream: events.stream,
      );
      final session = PlaySession(client, watchdogConfig: msConfig);

      final runFuture = session.run(
        launch: _buildTestLaunch(),
        readyTimeout: const Duration(seconds: 2),
      );

      await awaitSentEnvelope(WireMessageType.sessionStart);
      // First check fires at ~pollInterval; respond promptly.
      await waitForHealthCheckCount(1);
      // Second check fires ~pollInterval after the first response — do not
      // respond. secondCheckSentAt is now set inside the interceptor closure.
      await waitForHealthCheckCount(2);

      Object? caught;
      try {
        await runFuture;
      } catch (e) {
        caught = e;
      }
      final throwAt = DateTime.now();

      expect(caught, isA<CytoidGameCoreSessionFailedException>());

      final elapsedMs = throwAt.difference(secondCheckSentAt!).inMilliseconds;
      // steadyResponseTimeout=200ms. The throw should land in
      // [0.8*steady, 3*steady] and well below firstResponseTimeout=2000ms.
      expect(
        elapsedMs,
        greaterThanOrEqualTo(160),
        reason: 'should not fire faster than 0.8*steadyResponseTimeout',
      );
      expect(
        elapsedMs,
        lessThan(1500),
        reason: 'should fire well before firstResponseTimeout',
      );
    });

    test('watchdog: terminal session.result cancels pending watchdog, no late '
        'session.failed fires', () async {
      installSendInterceptor(
        onSend: (env) {
          if (env.type == WireMessageType.healthCheck) {
            Future.microtask(() => emitHealthOk(env.id));
          }
        },
      );

      final client = CytoidGameCoreClient(
        methodChannel: primaryChannel,
        eventStream: events.stream,
      );
      final session = PlaySession(client, watchdogConfig: msConfig);

      final runFuture = session.run(
        launch: _buildTestLaunch(),
        readyTimeout: const Duration(seconds: 2),
      );

      final startEnvelope = await awaitSentEnvelope(
        WireMessageType.sessionStart,
      );
      events.add(
        CytoidGameCoreEnvelope.create(
          id: startEnvelope.id,
          type: WireMessageType.sessionStarted,
          payload: {'sessionId': startEnvelope.id},
        ).toJsonString(),
      );

      // Wait for first health.check (and let mock respond with health.ok).
      await awaitSentEnvelope(WireMessageType.healthCheck);
      // Let the response land and scheduleNextCheck arm the second check.
      await Future<void>.delayed(const Duration(milliseconds: 10));

      // Emit session.result BEFORE the second check would fire (pollInterval=
      // 100ms). The watchdog's cancelSubscription closure sets disposed=true
      // and cancels watchdogTimer.
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

      // Wait past what would have been the second check window — if disposal
      // didn't cancel the timer, a second health.check would have fired by now.
      await Future<void>.delayed(const Duration(milliseconds: 400));

      // Exactly ONE health.check was sent — the disposed/completer.isCompleted
      // guard cancelled the scheduled second check before it could run.
      expect(healthCheckSendCount(), 1);
      expect(primaryCalls.map((c) => c.method), contains('hideGameSurface'));
    });

    test('watchdog: default constructor arms the watchdog — health.check is '
        'sent (no watchdogConfig kwarg)', () async {
      // Verify the default `PlaySession(client)` ctor ENABLES the watchdog.
      // The default pollInterval is 10s — use a generous deadline on
      // awaitSentEnvelope so the test still bounds total runtime while
      // accommodating the real default cadence.
      installSendInterceptor(
        onSend: (env) {
          if (env.type == WireMessageType.healthCheck) {
            Future.microtask(() => emitHealthOk(env.id));
          }
        },
      );

      final client = CytoidGameCoreClient(
        methodChannel: primaryChannel,
        eventStream: events.stream,
      );
      // NO watchdogConfig kwarg — relies on the default = const
      // HealthCheckWatchdogConfig(). A regression that flipped the default to
      // null would make this test fail (no health.check ever sent).
      final session = PlaySession(client);
      expect(session.watchdogConfig, isNotNull,
          reason: 'default constructor must set a non-null config (default-on)');

      final runFuture = session.run(
        launch: _buildTestLaunch(),
        readyTimeout: const Duration(seconds: 2),
      );

      final startEnvelope = await awaitSentEnvelope(
        WireMessageType.sessionStart,
      );

      // Default pollInterval is 10s — wait up to 15s for the first check.
      await awaitSentEnvelope(
        WireMessageType.healthCheck,
        deadline: const Duration(seconds: 15),
      );

      // Cleanup: emit session.result so run() completes cleanly.
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
      expect(healthCheckSendCount(), greaterThanOrEqualTo(1));
      expect(primaryCalls.map((c) => c.method), contains('hideGameSurface'));
    });

    test('watchdog: null watchdogConfig disables the watchdog — no '
        'health.check sent', () async {
      // Verify the OPT-OUT escape hatch. Passing watchdogConfig: null MUST
      // disable the watchdog entirely — no health.check envelopes are sent
      // regardless of how long the session runs.
      installSendInterceptor(onSend: (_) {});

      final client = CytoidGameCoreClient(
        methodChannel: primaryChannel,
        eventStream: events.stream,
      );
      final session = PlaySession(client, watchdogConfig: null);
      expect(session.watchdogConfig, isNull,
          reason: 'null watchdogConfig must leave the field null (opt-out)');

      final runFuture = session.run(
        launch: _buildTestLaunch(),
        readyTimeout: const Duration(seconds: 2),
      );

      final startEnvelope = await awaitSentEnvelope(
        WireMessageType.sessionStart,
      );

      // Emit session.started + session.result promptly so the run completes.
      events.add(
        CytoidGameCoreEnvelope.create(
          id: startEnvelope.id,
          type: WireMessageType.sessionStarted,
          payload: {'sessionId': startEnvelope.id},
        ).toJsonString(),
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
        const Duration(seconds: 2),
        onTimeout: () => throw TimeoutException('run() never returned'),
      );
      expect(result.outcome.kind, OutcomePayload.completedKind);

      // Wait past what would have been the first pollInterval if the watchdog
      // were armed with msConfig (100ms). 300ms gives comfortable margin.
      await Future<void>.delayed(const Duration(milliseconds: 300));

      // ZERO health.check envelopes — the watchdog was disabled.
      expect(healthCheckSendCount(), 0);
      expect(
        sentEnvelopeOfType(WireMessageType.healthCheck),
        isNull,
        reason: 'watchdogConfig: null MUST disable health.check sends',
      );
      expect(primaryCalls.map((c) => c.method), contains('hideGameSurface'));
    });
  });
}
