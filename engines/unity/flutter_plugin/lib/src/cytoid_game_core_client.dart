// ignore_for_file: prefer_initializing_formals

import 'dart:async';

import 'package:flutter/services.dart';

import 'cytoid_game_core_envelope.dart';
import 'cytoid_game_core_lost_exception.dart';
import 'cytoid_game_core_liveness_config.dart';
import 'cytoid_game_core_play_route_ended_exception.dart';
import 'game_runtime_status.dart';
import 'models/cytoid_game_core_log_entry.dart';
import 'models/game_launch_payload.dart';
import 'models/game_launch_settings.dart';
import 'models/game_result_payload.dart';
import 'wire_message_type.dart';

/// Flutter plugin client for bridge ↔ Unity game core messaging.
class CytoidGameCoreClient {
  CytoidGameCoreClient({
    MethodChannel? methodChannel,
    EventChannel? eventChannel,
    Stream<dynamic>? eventStream,
    CytoidGameCoreLivenessConfig? livenessConfig,
  }) : _methodChannel =
           methodChannel ?? const MethodChannel(_methodChannelName),
       _eventChannel = eventChannel ?? const EventChannel(_eventChannelName),
       _eventStream = eventStream,
       _livenessConfig = livenessConfig ?? const CytoidGameCoreLivenessConfig();

  static const _methodChannelName = 'cytoid/game_core';
  static const _eventChannelName = 'cytoid/game_core/events';

  final MethodChannel _methodChannel;
  final EventChannel _eventChannel;
  final Stream<dynamic>? _eventStream;
  final CytoidGameCoreLivenessConfig _livenessConfig;

  Stream<CytoidGameCoreEnvelope>? _events;
  int _messageCounter = 0;

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
    return events.where((envelope) => envelope.isReady);
  }

  Stream<CytoidGameCoreLogBatch> get logBatchEvents {
    return events
        .where((envelope) => envelope.isLogBatch)
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
    if (nativeStatus != null) {
      return GameRuntimeStatus.fromJson(nativeStatus);
    }

    final id = _nextMessageId();
    final responseFuture = _awaitMatchingEnvelope(
      id: id,
      matches: (envelope) => envelope.isStatusResult,
      timeout: _livenessConfig.pingTimeout,
    );
    await send(
      CytoidGameCoreEnvelope.create(id: id, type: WireMessageType.bridgeStatus),
    );
    final envelope = await responseFuture;
    return GameRuntimeStatus.fromJson(envelope.payload);
  }

  Future<CytoidGameCoreEnvelope> ping({
    String text = 'ping',
    Duration? timeout,
  }) async {
    final id = _nextMessageId();
    final responseFuture = _awaitMatchingEnvelope(
      id: id,
      matches: (envelope) => envelope.isPong,
      timeout: timeout ?? _livenessConfig.pingTimeout,
    );

    await send(
      CytoidGameCoreEnvelope.create(
        id: id,
        type: WireMessageType.bridgePing,
        payload: {'text': text},
      ),
    );

    return responseFuture;
  }

  Future<GameResultPayload> startPlay(GameLaunchPayload launch) async {
    final id = _nextMessageId();

    await send(
      CytoidGameCoreEnvelope.create(
        id: id,
        type: WireMessageType.bridgePlayStart,
        payload: launch.toJson(),
      ),
    );

    final resultEnvelope = await _waitForPlayResultWithLiveness(playId: id);
    return GameResultPayload.fromJson(
      Map<String, dynamic>.from(resultEnvelope.payload),
    );
  }

  Future<void> endPlayRoute({
    Duration timeout = const Duration(seconds: 3),
  }) async {
    final id = _nextMessageId();
    final responseFuture = _awaitMatchingEnvelope(
      id: id,
      matches: (envelope) => envelope.isPlayRouteEnded,
      timeout: timeout,
    );
    await send(
      CytoidGameCoreEnvelope.create(
        id: id,
        type: WireMessageType.bridgePlayEnd,
      ),
    );
    await responseFuture;
  }

  Future<void> updateSettings(GameLaunchSettings settings) async {
    final id = _nextMessageId();
    final responseFuture = _awaitMatchingEnvelope(
      id: id,
      matches: (envelope) => envelope.isSettingsApplied,
      timeout: _livenessConfig.pingTimeout,
    );
    await send(
      CytoidGameCoreEnvelope.create(
        id: id,
        type: WireMessageType.bridgeSettingsUpdate,
        payload: settings.toJson(),
      ),
    );
    await responseFuture;
  }

  Future<CytoidGameCoreEnvelope> _waitForPlayResultWithLiveness({
    required String playId,
  }) async {
    final completer = Completer<CytoidGameCoreEnvelope>();
    late StreamSubscription<CytoidGameCoreEnvelope> subscription;
    Timer? watchdogTimer;
    var consecutiveFailures = 0;
    var checkInFlight = false;

    Future<void> runLivenessCheck() async {
      if (completer.isCompleted || checkInFlight) {
        return;
      }

      checkInFlight = true;
      try {
        var alive = false;
        try {
          final status = await queryStatus();
          alive = status.isBusy || status.activePlayId == playId;
        } catch (_) {
          alive = false;
        }

        if (alive) {
          try {
            await ping(text: 'heartbeat', timeout: _livenessConfig.pingTimeout);
          } catch (_) {
            alive = false;
          }
        }

        if (!alive) {
          consecutiveFailures += 1;
        } else {
          consecutiveFailures = 0;
        }

        if (!completer.isCompleted &&
            consecutiveFailures >= _livenessConfig.maxConsecutiveFailures) {
          watchdogTimer?.cancel();
          completer.completeError(
            CytoidGameCoreLostException(
              'Game core stopped responding during gameplay.',
              consecutiveFailures: consecutiveFailures,
            ),
          );
        }
      } finally {
        checkInFlight = false;
      }
    }

    subscription = events.listen((envelope) {
      if (envelope.id == playId &&
          envelope.isPlayResult &&
          !completer.isCompleted) {
        watchdogTimer?.cancel();
        unawaited(subscription.cancel());
        completer.complete(envelope);
      } else if (envelope.id == playId &&
          envelope.isPlayRouteEnded &&
          !completer.isCompleted) {
        watchdogTimer?.cancel();
        unawaited(subscription.cancel());
        completer.completeError(
          const CytoidGameCorePlayRouteEndedException(
            'Play route ended without result.',
          ),
        );
      }
    });

    watchdogTimer = Timer.periodic(
      _livenessConfig.checkInterval,
      (_) => unawaited(runLivenessCheck()),
    );

    try {
      return await completer.future;
    } finally {
      watchdogTimer.cancel();
      await subscription.cancel();
    }
  }

  Future<CytoidGameCoreEnvelope> _awaitMatchingEnvelope({
    required String id,
    required bool Function(CytoidGameCoreEnvelope envelope) matches,
    required Duration timeout,
  }) async {
    final completer = Completer<CytoidGameCoreEnvelope>();
    late StreamSubscription<CytoidGameCoreEnvelope> subscription;

    subscription = events.listen((envelope) {
      if (envelope.id == id && matches(envelope) && !completer.isCompleted) {
        unawaited(subscription.cancel());
        completer.complete(envelope);
      }
    });

    try {
      return await completer.future.timeout(timeout);
    } catch (_) {
      await subscription.cancel();
      rethrow;
    }
  }

  String _nextMessageId() {
    _messageCounter += 1;
    return 'msg-$_messageCounter-${DateTime.now().microsecondsSinceEpoch}';
  }
}
