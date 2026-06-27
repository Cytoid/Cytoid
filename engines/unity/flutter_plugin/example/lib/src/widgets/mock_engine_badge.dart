import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';

/// Debug-only badge that surfaces the active engine mode on gameplay
/// screens.
///
/// Renders a compact "MOCK ENGINE" chip when [CytoidGameCoreClient.getEngineMode]
/// reports `'mock'`. The badge is hidden in release builds ([kDebugMode] is
/// `false`) and when the real Unity runtime is mounted. It is intentionally
/// not dismissible — per `docs/mock-engine.md` § Visibility Requirements,
/// any development UI using the mock must make the runtime mode visible so
/// a mock green path is never misread as proof that Unity startup, VFS,
/// scene loading, or native lifecycle works.
///
/// Place at the top of screens that drive gameplay handoff
/// (e.g. [GameSessionScreen]) so the indicator is visible while the mock
/// runtime is responding in place of Unity.
class MockEngineBadge extends StatefulWidget {
  const MockEngineBadge({
    super.key,
    required this.client,
    this.scenarioName,
  });

  final CytoidGameCoreClient client;

  /// Optional active mock scenario name surfaced for debug UIs. Wired when
  /// the host can identify the current scenario; left `null` otherwise.
  final String? scenarioName;

  @override
  State<MockEngineBadge> createState() => _MockEngineBadgeState();
}

class _MockEngineBadgeState extends State<MockEngineBadge> {
  String? _mode;

  @override
  void initState() {
    super.initState();
    _loadEngineMode();
  }

  Future<void> _loadEngineMode() async {
    try {
      final mode = await widget.client.getEngineMode();
      if (mounted) setState(() => _mode = mode);
    } catch (error) {
      debugPrint('[MockEngineBadge] getEngineMode failed: $error');
    }
  }

  @override
  Widget build(BuildContext context) {
    if (!kDebugMode) return const SizedBox.shrink();
    if (_mode != 'mock') return const SizedBox.shrink();

    final theme = Theme.of(context);
    final label = widget.scenarioName == null || widget.scenarioName!.isEmpty
        ? 'MOCK ENGINE'
        : 'MOCK ENGINE · ${widget.scenarioName}';

    return IgnorePointer(
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: Colors.amber.withValues(alpha: 0.92),
          borderRadius: const BorderRadius.all(Radius.circular(6)),
          border: Border.all(color: Colors.black.withValues(alpha: 0.35)),
        ),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(Icons.science_outlined, size: 14, color: Colors.black87),
              const SizedBox(width: 6),
              Text(
                label,
                style: theme.textTheme.labelSmall?.copyWith(
                  color: Colors.black87,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 0.5,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
