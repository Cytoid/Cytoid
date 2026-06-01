import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/material.dart';

import '../unity_log_store.dart';

class UnityLogsScreen extends StatelessWidget {
  const UnityLogsScreen({super.key, required this.logStore});

  final UnityLogStore logStore;

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: logStore,
      builder: (context, _) {
        final entries = logStore.entries.reversed.toList(growable: false);

        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 24, 16, 8),
              child: Row(
                children: [
                  Expanded(
                    child: Text(
                      'Unity Logs',
                      style: Theme.of(context).textTheme.displaySmall,
                    ),
                  ),
                  TextButton.icon(
                    onPressed: entries.isEmpty ? null : logStore.clear,
                    icon: const Icon(Icons.delete_outline),
                    label: const Text('Clear'),
                  ),
                ],
              ),
            ),
            Expanded(
              child: entries.isEmpty
                  ? const Center(
                      child: Text(
                        'No Unity logs yet.\nStart a play or wait for Unity log batches.',
                        textAlign: TextAlign.center,
                      ),
                    )
                  : ListView.builder(
                      padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                      itemCount: entries.length,
                      itemBuilder: (context, index) {
                        return _LogTile(entry: entries[index]);
                      },
                    ),
            ),
          ],
        );
      },
    );
  }
}

class _LogTile extends StatelessWidget {
  const _LogTile({required this.entry});

  final CytoidGameCoreLogEntry entry;

  @override
  Widget build(BuildContext context) {
    final color = _levelColor(context, entry.level);
    final timestamp = entry.timestamp ?? '';

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 8,
                    vertical: 2,
                  ),
                  decoration: BoxDecoration(
                    color: color.withValues(alpha: 0.15),
                    borderRadius: BorderRadius.circular(4),
                  ),
                  child: Text(
                    entry.level.name.toUpperCase(),
                    style: Theme.of(context).textTheme.labelSmall?.copyWith(
                      color: color,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
                if (timestamp.isNotEmpty) ...[
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      timestamp,
                      style: Theme.of(context).textTheme.bodySmall,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ],
              ],
            ),
            const SizedBox(height: 8),
            SelectableText(entry.message),
            if (entry.stackTrace != null && entry.stackTrace!.isNotEmpty) ...[
              const SizedBox(height: 8),
              SelectableText(
                entry.stackTrace!,
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  fontFamily: 'monospace',
                  color: Theme.of(context).colorScheme.outline,
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Color _levelColor(BuildContext context, CytoidGameCoreLogLevel level) {
    final scheme = Theme.of(context).colorScheme;
    switch (level) {
      case CytoidGameCoreLogLevel.warning:
        return Colors.orange;
      case CytoidGameCoreLogLevel.error:
      case CytoidGameCoreLogLevel.exception:
        return scheme.error;
      case CytoidGameCoreLogLevel.log:
        return scheme.outline;
    }
  }
}
