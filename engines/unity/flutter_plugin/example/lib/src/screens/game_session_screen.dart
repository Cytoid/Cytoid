import 'dart:async';
import 'dart:io';

import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../game_routes.dart';

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
      unawaited(_runGame());
    });
  }

  Future<void> _runGame() async {
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
      await _awaitHostReady();
      unawaited(
        _client
            .updateSettings(widget.args.settings.toLaunchSettings())
            .catchError((_) {}),
      );

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

  Future<void> _cancelAndPop() async {
    if (_leaving) return;
    _leaving = true;
    setState(() => _status = 'Closing');
    await _client.endPlayRoute().catchError((_) {});
    await _hideSurface();
    await _restorePresentation();
    if (mounted) {
      Navigator.of(context).pop();
    }
  }

  Future<void> _awaitHostReady() async {
    final readyCompleter = Completer<void>();
    late StreamSubscription<CytoidGameCoreEnvelope> readySubscription;
    readySubscription = _client.readyEvents.listen((_) {
      if (!readyCompleter.isCompleted) {
        readyCompleter.complete();
      }
    });

    try {
      final deadline = DateTime.now().add(const Duration(seconds: 20));
      while (!readyCompleter.isCompleted) {
        final status = await _client.queryStatus();
        if (status.state == GameRuntimeStatus.ready) {
          return;
        }
        unawaited(_pingReady());
        final remaining = deadline.difference(DateTime.now());
        if (remaining <= Duration.zero) {
          return;
        }
        await Future.any<void>([
          readyCompleter.future,
          Future<void>.delayed(
            remaining < const Duration(milliseconds: 500)
                ? remaining
                : const Duration(milliseconds: 500),
          ),
        ]);
      }
    } finally {
      await readySubscription.cancel();
    }
  }

  Future<void> _pingReady() async {
    try {
      await _client.ping(text: 'ready?');
    } catch (_) {}
  }

  Future<void> _hideSurface() async {
    if (!_surfaceVisible) return;
    _surfaceVisible = false;
    await _client.hideGameSurface().catchError((_) {});
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
