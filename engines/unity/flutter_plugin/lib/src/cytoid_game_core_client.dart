// ignore_for_file: prefer_initializing_formals

import 'dart:async';

import 'package:flutter/services.dart';

import 'cytoid_game_core_envelope.dart';
import 'cytoid_game_core_lost_exception.dart';
import 'cytoid_game_core_liveness_config.dart';
import 'cytoid_game_core_timeout_exception.dart';
import 'game_runtime_status.dart';
import 'models/cytoid_game_core_log_entry.dart';

/// Flutter plugin client for bridge ↔ Unity game core messaging.
class CytoidGameCoreClient {
  CytoidGameCoreClient({
    MethodChannel? methodChannel,
    MethodChannel? waitForReadyMethodChannel,
    EventChannel? eventChannel,
    Stream<dynamic>? eventStream,
    CytoidGameCoreLivenessConfig? livenessConfig,
  }) : _methodChannel =
           methodChannel ?? const MethodChannel(_methodChannelName),
       _waitForReadyChannel = waitForReadyMethodChannel ??
           const MethodChannel(_waitForReadyChannelName),
       _eventChannel = eventChannel ?? const EventChannel(_eventChannelName),
       _eventStream = eventStream;

  static const _methodChannelName = 'cytoid/game_core';
  static const _eventChannelName = 'cytoid/game_core/events';
  static const _waitForReadyChannelName = 'cytoid_game_core/waitForReady';

  /// Default `waitForReady` timeout, matching the iOS native default
  /// (`CytoidGameCoreBridge.waitForReadyDefaultTimeout`).
  static const defaultReadyTimeout = Duration(seconds: 30);

  // FlutterError codes returned by the iOS `cytoid_game_core/waitForReady`
  // method channel helper (registered by T6).
  static const _waitForReadyTimeoutErrorCode = 'waitForReadyTimeout';

  final MethodChannel _methodChannel;
  final MethodChannel _waitForReadyChannel;
  final EventChannel _eventChannel;
  final Stream<dynamic>? _eventStream;

  Stream<CytoidGameCoreEnvelope>? _events;

  Stream<CytoidGameCoreEnvelope> get events {
    return _events ??= (_eventStream ?? _eventChannel.receiveBroadcastStream())
        .map((event) {
          if (event is! String) {
            throw FormatException(
              'Expected envelope JSON string from event channel.',
            );
          }
          return CytoidGameCoreEnvelope.fromJsonString(event);
        })
        .asBroadcastStream();
  }

  Stream<CytoidGameCoreEnvelope> get readyEvents {
    return events.where((envelope) => envelope.isEngineReady);
  }

  Stream<CytoidGameCoreLogBatch> get logBatchEvents {
    return events
        .where((envelope) => envelope.isLogsBatch)
        .map(CytoidGameCoreLogBatch.fromEnvelope);
  }

  Future<void> ensureRuntimeStarted() async {
    await _methodChannel.invokeMethod<void>('ensureRuntimeStarted');

    final status = await queryStatus();
    if (status.isReady) {
      return;
    }
    if (status.isUnavailable) {
      throw CytoidGameCoreLostException('Game core runtime is unavailable.');
    }
  }

  Future<void> showGameSurface() async {
    await _methodChannel.invokeMethod<void>('showGameSurface');
  }

  Future<void> hideGameSurface() async {
    await _methodChannel.invokeMethod<void>('hideGameSurface');
  }

  /// Waits for the engine to acknowledge readiness.
  ///
  /// Calls [ensureRuntimeStarted] first, then either:
  /// - (a) on iOS, calls the `cytoid_game_core/waitForReady` method channel
  ///   helper registered by T6, which is state-machine-aware (handles
  ///   already-failed runtimes and native timeouts); or
  /// - (b) on Android (and any platform without the iOS helper), subscribes
  ///   to [readyEvents] until the first ready event arrives.
  ///
  /// Throws [CytoidGameCoreTimeoutException] on timeout. NEVER silently
  /// continues — callers cannot fall through to `session.start` against an
  /// engine that has not acknowledged readiness.
  ///
  /// On iOS, `PlatformException` codes other than `waitForReadyTimeout`
  /// (e.g. `runtime_unavailable`, `waitForReadyFailed`) are rethrown verbatim
  /// so the host can branch on the typed failure.
  Future<void> waitForReady({Duration? timeout}) async {
    await ensureRuntimeStarted();

    final effectiveTimeout = timeout ?? defaultReadyTimeout;

    try {
      // Fractional seconds: the iOS helper accepts TimeInterval (Double), and
      // truncating to whole seconds would collapse sub-second durations to 0.
      final timeoutSeconds = effectiveTimeout.inMilliseconds / 1000.0;
      await _waitForReadyChannel.invokeMethod<void>(
        'waitForReady',
        timeoutSeconds,
      );
    } on MissingPluginException {
      // No native helper registered (Android, tests, or pre-T6 iOS). Fall
      // back to subscribing the engine-ready event stream.
      await _awaitReadyEvent(effectiveTimeout);
    } on PlatformException catch (e) {
      if (e.code == _waitForReadyTimeoutErrorCode) {
        throw CytoidGameCoreTimeoutException(
          'iOS waitForReady helper timed out after '
          '${effectiveTimeout.inMilliseconds}ms.',
          timeout: effectiveTimeout,
        );
      }
      // alreadyFailed / waitForReadyFailed carry their own typed codes;
      // surface them so the host can branch on the runtime failure.
      rethrow;
    }
  }

  /// Android (and any platform without the iOS `waitForReady` helper) cannot
  /// rely on a fresh engine-ready event: the warm-resident runtime stays
  /// initialized across sessions, and an `Activity` resume after
  /// `showGameSurface` transitions SUSPENDED→READY without re-emitting
  /// `engine.ready`. The runtime snapshot is the only reliable signal, so we
  /// poll [queryStatus] until it reports ready, the runtime fails, or the
  /// deadline elapses.
  Future<void> _awaitReadyEvent(Duration timeout) async {
    final deadline = DateTime.now().add(timeout);
    while (true) {
      final status = await queryStatus();
      if (status.isReady) {
        return;
      }
      // Surface the real failure immediately instead of burning the full
      // timeout. Mirrors the iOS helper's alreadyFailed path and
      // ensureRuntimeStarted's unavailable handling.
      if (status.isFailed) {
        throw PlatformException(
          code: status.error?.code ?? 'runtime_failed',
          message: status.error?.message ?? 'Runtime is in a failed state.',
          details: status.error?.toJson(),
        );
      }
      if (status.isUnavailable) {
        throw CytoidGameCoreLostException('Game core runtime is unavailable.');
      }
      if (DateTime.now().isAfter(deadline)) {
        throw CytoidGameCoreTimeoutException(
          'Engine did not reach the ready state within '
          '${timeout.inMilliseconds}ms.',
          timeout: timeout,
        );
      }
      await Future<void>.delayed(_readyPollInterval);
    }
  }

  /// Interval between [queryStatus] probes while awaiting readiness on
  /// platforms without a native ready-wait helper. Bounds the worst-case
  /// latency to observe a resume→ready transition that fires no event.
  static const _readyPollInterval = Duration(milliseconds: 200);

  Future<void> send(CytoidGameCoreEnvelope envelope) async {
    await _methodChannel.invokeMethod<void>('send', envelope.toJsonString());
  }

  Future<String> getEngineMode() async {
    final mode = await _methodChannel.invokeMethod<String>('getEngineMode');
    return mode ?? 'unknown';
  }

  Future<GameRuntimeStatus> queryStatus() async {
    final nativeStatus = await _methodChannel.invokeMapMethod<String, Object?>(
      'queryRuntimeStatus',
    );
    if (nativeStatus == null) {
      throw CytoidGameCoreLostException('Game core runtime status is unavailable.');
    }
    return GameRuntimeStatus.fromJson(nativeStatus);
  }
}
