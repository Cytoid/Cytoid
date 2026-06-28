import 'dart:async';

import 'cytoid_game_core_client.dart';
import 'cytoid_game_core_envelope.dart';
import 'cytoid_game_core_session_failed_exception.dart';
import 'health_check_watchdog_config.dart';
import 'models/v2/session_failed_payload.dart';
import 'models/v2/session_launch_payload.dart';
import 'models/v2/session_result_payload.dart';
import 'wire_message_type.dart';

/// High-level orchestration for one v2 gameplay session.
///
/// Owns the canonical session lifecycle:
/// 1. ensure runtime is started,
/// 2. show the game surface,
/// 3. wait for the engine to acknowledge readiness,
/// 4. send a typed v2 `session.start` envelope,
/// 5. await the matching `session.result` OR `session.failed`,
/// 6. ALWAYS hide the surface, even when an earlier step throws.
///
/// v2 envelopes are composed here from the T3 typed payload models so callers
/// never have to touch raw `Map<String, dynamic>` wire shapes. Lower-level
/// primitives ([CytoidGameCoreClient.ensureRuntimeStarted],
/// [CytoidGameCoreClient.showGameSurface], [CytoidGameCoreClient.send]) remain
/// available for advanced callers that need to deviate from this sequence.
class PlaySession {
  PlaySession(
    this.client, {
    this.watchdogConfig = HealthCheckWatchdogConfig.defaults,
  });

  /// The low-level client used to drive runtime, surface, and envelope I/O.
  final CytoidGameCoreClient client;

  /// Tuning for the v2 `health.check` watchdog armed during
  /// [_awaitSessionResult]. When `null`, the watchdog is DISABLED — no
  /// `health.check` envelopes are sent and the session awaits a terminal
  /// envelope indefinitely (the v2 back-compat escape hatch; defaults to ON).
  final HealthCheckWatchdogConfig? watchdogConfig;

  int _sessionIdCounter = 0;
  int _healthCheckCounter = 0;
  String? _activeSessionId;

  /// Runs a full v2 session lifecycle and returns the typed terminal result.
  ///
  /// The surface is ALWAYS hidden via [CytoidGameCoreClient.hideGameSurface]
  /// in a `finally` block, including when [waitForReady] throws
  /// [CytoidGameCoreTimeoutException] or when `session.start` itself fails.
  /// Errors propagate — this method never swallows them.
  ///
  /// [readyTimeout] is forwarded to [CytoidGameCoreClient.waitForReady];
  /// `null` uses the client default (30s).
  Future<SessionResultPayload> run({
    required SessionLaunchPayload launch,
    Duration? readyTimeout,
  }) async {
    if (_activeSessionId != null) {
      throw StateError('A session is already active on this PlaySession.');
    }
    final sessionId = _nextSessionId();
    _activeSessionId = sessionId;
    var surfaceShown = false;
    try {
      await client.ensureRuntimeStarted();
      await client.showGameSurface();
      surfaceShown = true;
      await client.waitForReady(timeout: readyTimeout);

      final resultWait = _awaitSessionResult(sessionId, watchdogConfig);
      try {
        await client.send(
          CytoidGameCoreEnvelope.create(
            id: sessionId,
            type: WireMessageType.sessionStart,
            payload: launch.toJson(),
          ),
        );
        resultWait.armWatchdog?.call();
        return await resultWait.future;
      } finally {
        await resultWait.cancelSubscription();
      }
    } finally {
      try {
        // Only hide if the surface was actually shown — otherwise a hide
        // failure here could mask the real startup error.
        if (surfaceShown) {
          await client.hideGameSurface();
        }
      } finally {
        if (_activeSessionId == sessionId) {
          _activeSessionId = null;
        }
      }
    }
  }

  /// Sends a v2 `session.cancel` envelope for an in-flight session.
  ///
  /// The engine responds asynchronously with a `session.result` whose
  /// `outcome.kind = "cancelled"` (matching this `sessionId` and `reason`),
  /// or with an `engine.error` for the cancel edge cases listed in v2 §
  /// Cancel Edge Cases (`unknown_session`, `not_active`, `already_cancelling`).
  /// This method does NOT wait for that response — callers observe it on the
  /// [CytoidGameCoreClient.events] stream.
  ///
  /// Allowed `reason` values per v2 spec: `userBack`, `hostNavigation`,
  /// `appBackgrounded`, `surfaceLost`, `unknown`.
  Future<void> cancel({String? sessionId, String reason = 'userBack'}) async {
    final id = sessionId ?? _activeSessionId;
    if (id == null) {
      throw StateError('No active session id is available to cancel.');
    }
    await client.send(
      CytoidGameCoreEnvelope.create(
        id: id,
        type: WireMessageType.sessionCancel,
        payload: {'reason': reason},
      ),
    );
  }

  /// Subscribes to [CytoidGameCoreClient.events] and completes with the
  /// [SessionResultPayload] for [sessionId], OR completes with an error when
  /// a matching `session.failed` envelope arrives. The first terminal envelope
  /// of either type wins; a late duplicate of the other type is ignored
  /// (enforced by the [Completer.isCompleted] guard — the v2 "exactly one of
  /// result OR failed, never both" lockstep invariant). Errors during payload
  /// parsing propagate via the returned future.
  _SessionResultWait _awaitSessionResult(
    String sessionId,
    HealthCheckWatchdogConfig? watchdogConfig,
  ) {
    final completer = Completer<SessionResultPayload>();
    DateTime? lastInboundAt;
    Timer? watchdogTimer;
    // In-flight per-check resources lifted to the outer scope so the
    // session-cancel path can release them immediately. Without this, a
    // session that completes via session.result / session.failed / stream
    // error while runCheck is inside sendAndAwaitHealthOk would leave a
    // passive listener + pending timer lingering for up to firstResponseTimeout.
    Timer? inFlightCheckTimer;
    StreamSubscription<dynamic>? inFlightCheckSub;
    var isFirstCheck = true;
    var disposed = false;

    late StreamSubscription<dynamic> subscription;
    subscription = client.events.listen(
      (envelope) {
        // Last-message shortcut: record ANY non-terminal engine-envelope
        // arrival BEFORE the sessionId guard. Real engine envelopes that prove
        // liveness carry ids that are NOT the sessionId — logs.batch and
        // engine.error carry fresh UUIDs (GameLogBridge emits Guid.NewGuid),
        // settings.applied carries the settings-apply id, and health.ok carries
        // the checkId ("health-..."). Updating after the id guard would never
        // refresh from real log flow, defeating the optimization. Terminal
        // envelopes (sessionResult / sessionFailed) are excluded because they
        // complete the completer below.
        if (envelope.type != WireMessageType.sessionResult &&
            envelope.type != WireMessageType.sessionFailed) {
          lastInboundAt = DateTime.now();
        }
        if (envelope.id != sessionId || completer.isCompleted) {
          return;
        }
        if (envelope.type == WireMessageType.sessionResult) {
          try {
            completer.complete(
              SessionResultPayload.fromJson(
                Map<String, dynamic>.from(envelope.payload),
              ),
            );
          } catch (e, st) {
            completer.completeError(e, st);
          }
          return;
        }
        if (envelope.type == WireMessageType.sessionFailed) {
          try {
            final failed = SessionFailedPayload.fromJson(
              Map<String, dynamic>.from(envelope.payload),
            );
            completer.completeError(
              CytoidGameCoreSessionFailedException(
                sessionId: sessionId,
                code: failed.error.code,
                message: failed.error.message,
                details: failed.error.details,
              ),
            );
          } catch (e, st) {
            completer.completeError(e, st);
          }
          return;
        }
      },
      onError: (Object error, StackTrace stackTrace) {
        // Guard: a stream error that arrives after the completer was already
        // completed (by session.result / session.failed / watchdog timeout)
        // MUST be silently dropped. Calling completeError on an already-
        // completed completer throws StateError; this guard enforces the same
        // lockstep invariant the rest of the method uses.
        if (!completer.isCompleted) {
          completer.completeError(error, stackTrace);
        }
      },
    );

    // Sends one `health.check` envelope and awaits the matching `health.ok`.
    // CRITICAL ORDERING (race fix): the per-check listener is attached
    // BEFORE `client.send` is called. `client.events` is a broadcast stream
    // (`asBroadcastStream()` at cytoid_game_core_client.dart:58) and drops
    // events with no listener — an engine that responds instantaneously
    // during the `send` await would otherwise have its `health.ok` lost,
    // causing a false-positive timeout.
    Future<bool> sendAndAwaitHealthOk(String checkId, Duration timeout) async {
      final okCompleter = Completer<bool>();
      // Assign to the OUTER inFlight* locals (not local finals) so the
      // session-cancel closure can release these resources immediately if
      // the session completes via another path while we're awaiting. The
      // local finally block below still cancels them on normal completion.
      inFlightCheckSub = client.events.listen(
        (envelope) {
          if (!completer.isCompleted &&
              envelope.id == checkId &&
              envelope.type == WireMessageType.healthOk &&
              !okCompleter.isCompleted) {
            okCompleter.complete(true);
          }
        },
        onError: (Object _, StackTrace _) {
          // Silently ignore stream errors on the per-check listener. Stream
          // errors are delivered to EVERY subscription on a broadcast stream;
          // the MAIN subscription owns stream-error completion of the session
          // completer (with the isCompleted guard). This listener's only job
          // is to match health.ok — without this no-op onError, a stream
          // error would surface as an uncaught async error on this listener.
        },
      );
      inFlightCheckTimer = Timer(timeout, () {
        if (!okCompleter.isCompleted) okCompleter.complete(false);
      });
      try {
        try {
          await client.send(
            CytoidGameCoreEnvelope.create(
              id: checkId,
              type: WireMessageType.healthCheck,
              payload: {'activeSessionId': sessionId},
            ),
          );
        } catch (_) {
          // Native send failed: the native bridge synthesizes session.failed
          // (runtime_unreachable) asynchronously per AGENTS.md § Active-Session
          // Runtime Failure. Don't short-circuit — let the timeout run; the
          // native synthesis will complete the main completer first.
        }
        return await okCompleter.future;
      } finally {
        inFlightCheckTimer?.cancel();
        inFlightCheckTimer = null;
        await inFlightCheckSub?.cancel();
        inFlightCheckSub = null;
      }
    }

    // Self-scheduling watchdog. Declare the two mutually-recursive closures
    // as `late` variables so each can reference the other without tripping
    // Dart's referenced_before_declaration rule for local functions.
    late final void Function() scheduleNextCheck;
    late final Future<void> Function() runCheck;

    scheduleNextCheck = () {
      if (disposed || completer.isCompleted) return;
      final cfg = watchdogConfig;
      if (cfg == null) return;
      watchdogTimer = Timer(cfg.pollInterval, runCheck);
    };

    runCheck = () async {
      if (disposed || completer.isCompleted) return;
      final cfg = watchdogConfig;
      if (cfg == null) return;
      // Skip sending when a non-terminal engine envelope arrived within the
      // last pollInterval AND we're past the first check. The first check is
      // NEVER skipped so the watchdog establishes a deterministic baseline
      // (firstResponseTimeout) regardless of lastInboundAt.
      if (!isFirstCheck &&
          lastInboundAt != null &&
          DateTime.now().difference(lastInboundAt!) < cfg.pollInterval) {
        scheduleNextCheck();
        return;
      }
      final timeout = isFirstCheck
          ? cfg.firstResponseTimeout
          : cfg.steadyResponseTimeout;
      // Set isFirstCheck=false BEFORE the await so a concurrent fire sees the
      // post-first state (subsequent checks always use steadyResponseTimeout).
      isFirstCheck = false;
      final checkId = 'health-${_nextHealthCheckId()}';
      final gotResponse = await sendAndAwaitHealthOk(checkId, timeout);
      // The main subscription may have completed the completer via
      // session.result/session.failed while we were awaiting health.ok.
      if (disposed || completer.isCompleted) return;
      if (!gotResponse) {
        // The watchdog completes the completer with an EXCEPTION only — it
        // MUST NOT inject a synthetic session.failed envelope onto
        // client.events. Other listeners on the broadcast stream observe the
        // failure via the PlaySession.run future throwing. The reused
        // `runtime_unreachable` code carries watchdog provenance in `message`
        // (the host cannot distinguish a frozen main loop from a process-
        // level unreachable at this layer).
        completer.completeError(
          CytoidGameCoreSessionFailedException(
            sessionId: sessionId,
            code: 'runtime_unreachable',
            message:
                'Engine did not respond to health.check (id=$checkId) '
                'within ${timeout.inSeconds}s; watchdog synthesized '
                'runtime_unreachable.',
          ),
        );
        return;
      }
      scheduleNextCheck();
    };

    return _SessionResultWait(
      future: completer.future,
      cancelSubscription: () async {
        // Set disposed FIRST so a timer that fires during teardown no-ops
        // (its runCheck continuation early-returns on the disposed guard).
        disposed = true;
        watchdogTimer?.cancel();
        // Release in-flight per-check resources too — covers the race where
        // session.result / session.failed / stream error completes the
        // session while runCheck is inside sendAndAwaitHealthOk. Cancel is
        // idempotent (no-op on already-cancelled sub/timer).
        inFlightCheckTimer?.cancel();
        await inFlightCheckSub?.cancel();
        await subscription.cancel();
      },
      armWatchdog: watchdogConfig != null ? scheduleNextCheck : null,
    );
  }

  String _nextSessionId() {
    _sessionIdCounter += 1;
    return 'session-$_sessionIdCounter-'
        '${DateTime.now().microsecondsSinceEpoch}';
  }

  String _nextHealthCheckId() {
    _healthCheckCounter += 1;
    return '$_healthCheckCounter-${DateTime.now().microsecondsSinceEpoch}';
  }
}

/// Bundle returned by [PlaySession._awaitSessionResult] so the caller can
/// cancel the inbound subscription from a `finally` block without exposing
/// the [StreamSubscription] itself.
class _SessionResultWait {
  const _SessionResultWait({
    required this.future,
    required this.cancelSubscription,
    this.armWatchdog,
  });

  final Future<SessionResultPayload> future;
  final Future<void> Function() cancelSubscription;

  /// Called by `run()` AFTER `session.start` is sent, to arm the first
  /// health.check. Null when the watchdog is disabled (watchdogConfig == null).
  /// Splitting setup from arming prevents the watchdog from firing before the
  /// engine has received `session.start` (CodeRabbit finding on PR #179).
  final void Function()? armWatchdog;
}
