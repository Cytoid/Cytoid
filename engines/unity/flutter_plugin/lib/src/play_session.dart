import 'dart:async';

import 'cytoid_game_core_client.dart';
import 'cytoid_game_core_envelope.dart';
import 'cytoid_game_core_session_failed_exception.dart';
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
  PlaySession(this.client);

  /// The low-level client used to drive runtime, surface, and envelope I/O.
  final CytoidGameCoreClient client;

  int _sessionIdCounter = 0;
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

      final resultWait = _awaitSessionResult(sessionId);
      try {
        await client.send(
          CytoidGameCoreEnvelope.create(
            id: sessionId,
            type: WireMessageType.sessionStart,
            payload: launch.toJson(),
          ),
        );
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
  _SessionResultWait _awaitSessionResult(String sessionId) {
    final completer = Completer<SessionResultPayload>();
    late StreamSubscription<dynamic> subscription;
    subscription = client.events.listen((envelope) {
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
    }, onError: completer.completeError);

    return _SessionResultWait(
      future: completer.future,
      cancelSubscription: () async {
        await subscription.cancel();
      },
    );
  }

  String _nextSessionId() {
    _sessionIdCounter += 1;
    return 'session-$_sessionIdCounter-'
        '${DateTime.now().microsecondsSinceEpoch}';
  }
}

/// Bundle returned by [PlaySession._awaitSessionResult] so the caller can
/// cancel the inbound subscription from a `finally` block without exposing
/// the [StreamSubscription] itself.
class _SessionResultWait {
  const _SessionResultWait({
    required this.future,
    required this.cancelSubscription,
  });

  final Future<SessionResultPayload> future;
  final Future<void> Function() cancelSubscription;
}
