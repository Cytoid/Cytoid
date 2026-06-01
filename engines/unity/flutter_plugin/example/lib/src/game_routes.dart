import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/foundation.dart';

import 'models/example_level.dart';
import 'models/example_settings.dart';
import 'models/example_mods.dart';

abstract final class ExampleRoutes {
  static const home = '/';
  static const game = '/game';
  static const result = '/result';
}

class GameRouteArgs {
  const GameRouteArgs({
    required this.client,
    required this.level,
    required this.difficulty,
    required this.settings,
    this.mods,
    this.tierPlay,
    this.onCalibrationResult,
  });

  final CytoidGameCoreClient client;
  final ExampleLevel level;
  final ExampleDifficulty difficulty;
  final ExampleSettings settings;
  final ExampleMods? mods;
  final TierPlayLaunch? tierPlay;
  final ValueChanged<GameResultPayload>? onCalibrationResult;
}

class ResultRouteArgs {
  const ResultRouteArgs({
    required this.level,
    required this.difficulty,
    required this.result,
  });

  final ExampleLevel level;
  final ExampleDifficulty difficulty;
  final GameResultPayload result;
}
