import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/material.dart';

import '../models/example_level.dart';
import '../models/example_mods.dart';
import '../models/example_settings.dart';
import '../game_routes.dart';

class LevelSelectScreen extends StatelessWidget {
  const LevelSelectScreen({
    super.key,
    required this.levelsFuture,
    required this.settings,
    required this.mods,
    required this.client,
    required this.onCalibrationResult,
  });

  final Future<List<ExampleLevel>> levelsFuture;
  final ExampleSettings settings;
  final ExampleMods mods;
  final CytoidGameCoreClient client;
  final ValueChanged<SessionResultPayload> onCalibrationResult;

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<List<ExampleLevel>>(
      future: levelsFuture,
      builder: (context, snapshot) {
        final levels = snapshot.data;
        return LayoutBuilder(
          builder: (context, constraints) {
            final width = constraints.maxWidth;
            final columns = width >= 1180 ? 3 : 2;
            return CustomScrollView(
              slivers: [
                SliverAppBar(
                  title: Text(
                    'Cytoid',
                    style: Theme.of(context).textTheme.headlineMedium,
                  ),
                  centerTitle: false,
                  pinned: true,
                ),
                if (snapshot.hasError)
                  SliverFillRemaining(
                    child: Center(
                      child: Text('Failed to load levels: ${snapshot.error}'),
                    ),
                  )
                else if (levels == null)
                  const SliverFillRemaining(
                    child: Center(child: Text('Loading songs...')),
                  )
                else
                  SliverPadding(
                    padding: const EdgeInsets.fromLTRB(18, 8, 18, 24),
                    sliver: SliverGrid.builder(
                      gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                        crossAxisCount: columns,
                        mainAxisSpacing: 14,
                        crossAxisSpacing: 14,
                        childAspectRatio: 1.85,
                      ),
                      itemCount: levels.length,
                      itemBuilder: (context, index) {
                        return _LevelCard(
                          level: levels[index],
                          settings: settings,
                          mods: mods,
                          client: client,
                          onCalibrationResult: onCalibrationResult,
                        );
                      },
                    ),
                  ),
              ],
            );
          },
        );
      },
    );
  }
}

class _LevelCard extends StatelessWidget {
  const _LevelCard({
    required this.level,
    required this.settings,
    required this.mods,
    required this.client,
    required this.onCalibrationResult,
  });

  final ExampleLevel level;
  final ExampleSettings settings;
  final ExampleMods mods;
  final CytoidGameCoreClient client;
  final ValueChanged<SessionResultPayload> onCalibrationResult;

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(8),
      child: Material(
        color: Theme.of(context).colorScheme.surfaceContainerHighest,
        child: InkWell(
          onTap: () => _play(context, level.difficulties.first),
          child: Stack(
            fit: StackFit.expand,
            children: [
              Image.asset(level.backgroundPath, fit: BoxFit.cover),
              const DecoratedBox(
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topCenter,
                    end: Alignment.bottomCenter,
                    colors: [Colors.transparent, Color(0xee000000)],
                  ),
                ),
              ),
              Positioned(
                left: 16,
                right: 16,
                bottom: 58,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      level.title,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: Theme.of(context).textTheme.headlineSmall
                          ?.copyWith(
                            fontWeight: FontWeight.w700,
                            color: Colors.white,
                          ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '${level.artist} · Chart: ${level.charter}',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: Theme.of(
                        context,
                      ).textTheme.bodyMedium?.copyWith(color: Colors.white70),
                    ),
                  ],
                ),
              ),
              Positioned(
                left: 12,
                right: 12,
                bottom: 12,
                child: Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    for (final difficulty in level.difficulties)
                      FilledButton.tonalIcon(
                        onPressed: () => _play(context, difficulty),
                        icon: const Icon(Icons.play_arrow),
                        label: Text('${difficulty.label} ${difficulty.level}'),
                      ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  void _play(BuildContext context, ExampleDifficulty difficulty) {
    Navigator.of(context).pushNamed(
      ExampleRoutes.game,
      arguments: GameRouteArgs(
        client: client,
        level: level,
        difficulty: difficulty,
        settings: settings,
        mods: mods,
        onCalibrationResult: onCalibrationResult,
      ),
    );
  }
}
