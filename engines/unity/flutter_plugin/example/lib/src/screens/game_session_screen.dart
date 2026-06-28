import 'dart:async';
import 'dart:io';

import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../game_routes.dart';
import '../widgets/mock_engine_badge.dart';

/// Handoff screen that orchestrates a single gameplay session.
///
/// Flutter owns the handoff UI; v2 gameplay orchestration is delegated to
/// [PlaySession.run].
class GameSessionScreen extends StatefulWidget {
  const GameSessionScreen({super.key, required this.args});

  final GameRouteArgs args;

  @override
  State<GameSessionScreen> createState() => _GameSessionScreenState();
}

class _GameSessionScreenState extends State<GameSessionScreen> {
  String _status = 'Preparing';
  bool _leaving = false;

  CytoidGameCoreClient get _client => widget.args.client;
  late final PlaySession _playSession = PlaySession(_client);

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      unawaited(_startSession());
    });
  }

  Future<void> _startSession() async {
    SessionLaunchPayload? payload;
    SessionResultPayload? result;

    try {
      payload = await widget.args.level.createLaunchPayload(
        difficulty: widget.args.difficulty,
        settings: widget.args.settings,
        mods: widget.args.mods,
        tierPlay: widget.args.tierPlay,
      );

      if (!mounted) return;
      setState(() => _status = 'Starting runtime');

      if (Platform.isIOS) {
        await SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky);
      }

      if (!mounted) return;
      setState(() => _status = 'Loading chart');

      result = await _playSession.run(
        launch: payload,
        readyTimeout: const Duration(seconds: 20),
      );
      if (_isCalibrationResult(result)) {
        widget.args.onCalibrationResult?.call(result);
      }
    } on Object catch (error) {
      result = _errorResult(error, payload);
    } finally {
      await _restorePresentation();
    }

    if (!mounted || _leaving) return;
    if (_isCalibrationResult(result)) {
      _leaving = true;
      Navigator.of(context).pop();
      return;
    }

    if (result.outcome.kind == OutcomePayload.tierRetryKind) {
      _leaving = true;
      Navigator.of(context).pop(result);
      return;
    }

    if (!mounted) return;
    _leaving = true;
    await Navigator.of(context).pushReplacementNamed(
      ExampleRoutes.result,
      arguments: ResultRouteArgs(
        level: widget.args.level,
        difficulty: widget.args.difficulty,
        result: result,
      ),
    );
  }

  bool _isCalibrationResult(SessionResultPayload result) {
    return result.outcome.kind == OutcomePayload.calibrationKind;
  }

  Future<void> _cancelAndPop() async {
    if (_leaving) return;
    _leaving = true;
    setState(() => _status = 'Closing');
    try {
      await _playSession.cancel(reason: 'userBack');
    } on StateError {
      // No active session id exists yet; the route can still close.
    } catch (error) {
      debugPrint('[GameSession] session.cancel failed: $error');
    }
    await _restorePresentation();
    if (mounted) {
      Navigator.of(context).pop();
    }
  }

  Future<void> _restorePresentation() async {
    await SystemChrome.setEnabledSystemUIMode(SystemUiMode.edgeToEdge);
  }

  SessionResultPayload _errorResult(
    Object error,
    SessionLaunchPayload? payload,
  ) {
    return SessionResultPayload(
      sessionId: 'example-error-${DateTime.now().microsecondsSinceEpoch}',
      mode: payload?.mode.wireName ?? SessionMode.ranked.wireName,
      mods: payload?.mods.map((mod) => mod.v2WireName).toList(growable: false) ??
          const [],
      outcome: const OutcomePayload.rejected(),
      flags: const FlagsPayload(usedAutoMod: false),
      telemetry: const ResultTelemetryPayload(
        available: false,
        eventsRecorded: 0,
        bytes: 0,
      ),
      timestamp: DateTime.now().millisecondsSinceEpoch,
      error: GameCoreError(code: 'example_session_error', message: '$error'),
    );
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (_, _) {
        unawaited(_cancelAndPop());
      },
      child: Scaffold(
        backgroundColor: Colors.black,
        body: Stack(
          children: [
            Positioned.fill(
              child: DecoratedBox(
                decoration: BoxDecoration(color: Colors.black),
              ),
            ),
            Positioned(
              right: 24,
              bottom: 18,
              child: AnimatedSwitcher(
                duration: const Duration(milliseconds: 160),
                child: _HandoffStatus(key: ValueKey(_status), status: _status),
              ),
            ),
            // Debug-only mock-engine indicator. Hidden in release builds and
            // whenever the real Unity runtime is mounted. See MockEngineBadge.
            Positioned(
              top: 18,
              left: 0,
              right: 0,
              child: Center(child: MockEngineBadge(client: _client)),
            ),
          ],
        ),
      ),
    );
  }
}

class _HandoffStatus extends StatelessWidget {
  const _HandoffStatus({super.key, required this.status});

  final String status;

  @override
  Widget build(BuildContext context) {
    return IgnorePointer(
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: Colors.black.withValues(alpha: 0.28),
          borderRadius: BorderRadius.circular(6),
          border: Border.all(color: Colors.white.withValues(alpha: 0.08)),
        ),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
          child: Text(
            status,
            style: Theme.of(context).textTheme.labelSmall?.copyWith(
              color: Colors.white.withValues(alpha: 0.42),
              fontWeight: FontWeight.w500,
            ),
          ),
        ),
      ),
    );
  }
}
