import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/material.dart';

import '../game_routes.dart';

class ResultScreen extends StatelessWidget {
  const ResultScreen({super.key, required this.args});

  final ResultRouteArgs args;

  @override
  Widget build(BuildContext context) {
    final result = args.result;
    final success = result.outcome.kind == OutcomePayload.completedKind;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Result'),
        leading: IconButton(
          icon: const Icon(Icons.close),
          onPressed: () => Navigator.of(context).pop(),
        ),
      ),
      body: Padding(
        padding: const EdgeInsets.fromLTRB(18, 8, 18, 18),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Expanded(
              flex: 5,
              child: _ResultHero(args: args, success: success),
            ),
            const SizedBox(width: 18),
            Expanded(
              flex: 4,
              child: ListView(
                children: [
                  _ResultTile(
                    icon: Icons.flag_circle_outlined,
                    label: 'Outcome',
                    value: result.outcome.kind,
                  ),
                  if (result.error != null)
                    _ResultTile(
                      icon: Icons.error_outline,
                      label: 'Error',
                      value: result.error!.message,
                    ),
                  if (result.calibration?.baseNoteOffset != null)
                    _ResultTile(
                      icon: Icons.timer,
                      label: 'Base note offset',
                      value:
                          '${result.calibration!.baseNoteOffset!.toStringAsFixed(3)} s',
                    ),
                  if (result.calibration?.levelNoteOffset != null)
                    _ResultTile(
                      icon: Icons.tune,
                      label: 'Level note offset',
                      value:
                          '${result.calibration!.levelNoteOffset!.toStringAsFixed(2)} s',
                    ),
                  if (result.tier != null) ...[
                    _ResultTile(
                      icon: Icons.favorite,
                      label: 'Final health',
                      value:
                          '${result.tier!.health?.toStringAsFixed(1) ?? '—'} / ${result.tier!.maxHealth.toStringAsFixed(0)}',
                    ),
                    _ResultTile(
                      icon: Icons.bolt,
                      label: 'Ending combo',
                      value: '${result.tier!.combo}',
                    ),
                    _ResultTile(
                      icon: Icons.military_tech,
                      label: 'Tier id',
                      value: result.tier!.tierId,
                    ),
                    _ResultTile(
                      icon: Icons.looks_one,
                      label: 'Stage index',
                      value: '${result.tier!.stageIndex}',
                    ),
                  ],
                  _ScoreBlock(result: result),
                  _EventTelemetryBlock(result: result),
                  const SizedBox(height: 16),
                  FilledButton.icon(
                    onPressed: () => Navigator.of(context).pop(),
                    icon: const Icon(Icons.library_music),
                    label: const Text('Back to songs'),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _EventTelemetryBlock extends StatelessWidget {
  const _EventTelemetryBlock({required this.result});

  final SessionResultPayload result;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        _ResultTile(
          icon: Icons.touch_app,
          label: 'Play events',
          value: '${result.telemetry.eventsRecorded}',
        ),
        _ResultTile(
          icon: Icons.data_object,
          label: 'Telemetry bytes',
          value: _formatBytes(result.telemetry.bytes),
        ),
      ],
    );
  }
}

class _ResultHero extends StatelessWidget {
  const _ResultHero({required this.args, required this.success});

  final ResultRouteArgs args;
  final bool success;

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(8),
      child: Stack(
        fit: StackFit.expand,
        children: [
          Image.asset(args.level.backgroundPath, fit: BoxFit.cover),
          DecoratedBox(
            decoration: BoxDecoration(
              color: Colors.black.withValues(alpha: 0.35),
            ),
          ),
          Positioned(
            left: 18,
            right: 18,
            bottom: 18,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  success ? 'Clear' : 'Session Ended',
                  style: Theme.of(context).textTheme.displaySmall?.copyWith(
                    color: Colors.white,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                Text(
                  '${args.level.title} · ${args.difficulty.label} ${args.difficulty.level}',
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(color: Colors.white70),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ScoreBlock extends StatelessWidget {
  const _ScoreBlock({required this.result});

  final SessionResultPayload result;

  @override
  Widget build(BuildContext context) {
    final score = result.score?.score.toString().padLeft(6, '0') ?? '-';
    final accuracy = result.score?.accuracy == null
        ? '-'
        : '${(result.score!.accuracy * 100).toStringAsFixed(2)}%';
    return Column(
      children: [
        _ResultTile(icon: Icons.score, label: 'Score', value: score),
        _ResultTile(icon: Icons.percent, label: 'Accuracy', value: accuracy),
        _ResultTile(
          icon: Icons.local_fire_department,
          label: 'Max combo',
          value: result.score?.maxCombo.toString() ?? '-',
        ),
        _ResultTile(
          icon: Icons.timer_outlined,
          label: 'Average timing',
          value: result.score?.averageTimingError == null
              ? '-'
              : '${result.score!.averageTimingError!.toStringAsFixed(2)} ms',
        ),
      ],
    );
  }
}

class _ResultTile extends StatelessWidget {
  const _ResultTile({
    required this.icon,
    required this.label,
    required this.value,
  });

  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      contentPadding: EdgeInsets.zero,
      leading: Icon(icon),
      title: Text(label),
      trailing: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 220),
        child: Text(
          value,
          textAlign: TextAlign.end,
          overflow: TextOverflow.ellipsis,
          maxLines: 2,
          style: Theme.of(context).textTheme.titleMedium,
        ),
      ),
    );
  }
}

String _formatBytes(int bytes) {
  if (bytes < 1024) {
    return '$bytes B';
  }
  final kib = bytes / 1024;
  if (kib < 1024) {
    return '${kib.toStringAsFixed(1)} KiB';
  }
  return '${(kib / 1024).toStringAsFixed(2)} MiB';
}
