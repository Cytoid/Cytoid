import 'dart:async';
import 'dart:io';

import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../game_routes.dart';

/// Handoff screen that orchestrates a single gameplay session.
///
/// Flutter owns the handoff UI (black overlay + status text). The Unity-side
/// overlay may flash briefly on surface transitions (show/hide) — this is
/// known and accepted per the v2 handoff-ownership decision.
///
/// Session readiness uses [CytoidGameCoreClient.waitForReady], the same
/// primitive that [PlaySession.run] composes. The full [PlaySession.run]
/// lifecycle (v2 `session.start` / `session.result`) requires engine-side v2
/// protocol support; the mock engine and current Unity core still speak v1
/// during the migration window (plan line 38: "Unity-side v2 protocol
/// implementation is a separate body of work tracked outside this plan").
/// Until the engine migrates, the play start/result path stays on the v1
/// `startPlay` API so the example remains functional.
class GameSessionScreen extends StatefulWidget {
  const GameSessionScreen({super.key, required this.args});

  final GameRouteArgs args;

  @override
  State<GameSessionScreen> createState() => _GameSessionScreenState();
}

class _GameSessionScreenState extends State<GameSessionScreen> {
  String _status = 'Preparing';
  bool _leaving = false;
  bool _surfaceVisible = false;

  CytoidGameCoreClient get _client => widget.args.client;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      unawaited(_startSession());
    });
  }

  Future<void> _startSession() async {
    GameResultPayload? result;

    try {
      final payload = await widget.args.level.createLaunchPayload(
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

      await _client.ensureRuntimeStarted();

      if (!mounted) return;
      setState(() => _status = 'Opening surface');

      await _client.showGameSurface();
      _surfaceVisible = true;

      // waitForReady is the readiness gate that PlaySession.run() composes.
      // Throws CytoidGameCoreTimeoutException on timeout — never silently
      // continues (replaces the former manual ready-wait deadline loop).
      await _client.waitForReady(timeout: const Duration(seconds: 20));

      // Fire-and-forget v1 settings update; does not block the session.
      unawaited(_applySettings());

      if (!mounted) return;
      setState(() => _status = 'Loading chart');

      result = await _client.startPlay(payload);
      widget.args.onCalibrationResult?.call(result);
      await _hideSurface();
    } on CytoidGameCorePlayRouteEndedException {
      await _hideSurface();
      await _restorePresentation();
      if (mounted && !_leaving) {
        _leaving = true;
        Navigator.of(context).pop();
      }
      return;
    } on Object catch (error) {
      result = GameResultPayload(
        completed: false,
        failed: true,
        usedAutoMod: false,
        error: error.toString(),
        timestamp: DateTime.now().toIso8601String(),
      );
      await _hideSurface();
    } finally {
      await _restorePresentation();
    }

    if (!mounted || _leaving) return;
    if (_isCalibrationResult(result)) {
      _leaving = true;
      Navigator.of(context).pop();
      return;
    }

    if (result.tierRetry != null) {
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

  bool _isCalibrationResult(GameResultPayload? result) {
    final gameMode = result?.gameMode?.toLowerCase();
    return gameMode == 'calibration' || gameMode == 'globalcalibration';
  }

  /// Applies v1 launch settings without blocking the session. Errors are
  /// logged but do not abort — settings are best-effort pre-play.
  Future<void> _applySettings() async {
    try {
      await _client.updateSettings(widget.args.settings.toLaunchSettings());
    } catch (error) {
      debugPrint('[GameSession] settings update failed: $error');
    }
  }

  Future<void> _cancelAndPop() async {
    if (_leaving) return;
    _leaving = true;
    setState(() => _status = 'Closing');
    try {
      await _client.endPlayRoute();
    } catch (error) {
      debugPrint('[GameSession] endPlayRoute failed: $error');
    }
    await _hideSurface();
    await _restorePresentation();
    if (mounted) {
      Navigator.of(context).pop();
    }
  }

  /// Hides the game surface if currently visible. Guarded against double-hide
  /// via [_surfaceVisible]. Used in non-PlaySession paths (cancel, error
  /// recovery); when PlaySession.run is adopted, its finally block will own
  /// the hide and this guard becomes redundant.
  Future<void> _hideSurface() async {
    if (!_surfaceVisible) return;
    _surfaceVisible = false;
    try {
      await _client.hideGameSurface();
    } catch (error) {
      debugPrint('[GameSession] hideGameSurface failed: $error');
    }
  }

  Future<void> _restorePresentation() async {
    await SystemChrome.setEnabledSystemUIMode(SystemUiMode.edgeToEdge);
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
