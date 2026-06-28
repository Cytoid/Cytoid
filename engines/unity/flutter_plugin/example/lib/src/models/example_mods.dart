import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/foundation.dart';

@immutable
class ExampleMods {
  const ExampleMods({
    this.gameMode = GameMode.standard,
    this.enabledMods = const {},
  });

  final GameMode gameMode;
  final Set<GameMod> enabledMods;

  ExampleMods copyWith({
    GameMode? gameMode,
    Set<GameMod>? enabledMods,
  }) {
    return ExampleMods(
      gameMode: gameMode ?? this.gameMode,
      enabledMods: enabledMods ?? this.enabledMods,
    );
  }

  List<String> toModStringList() {
    return enabledMods.map((m) => m.wireName).toList();
  }

  List<GameMod> toGameModList() {
    return enabledMods.toList(growable: false);
  }

  SessionMode toSessionMode() {
    return switch (gameMode) {
      GameMode.standard => SessionMode.ranked,
      GameMode.practice => SessionMode.practice,
      GameMode.calibration => SessionMode.calibration,
      GameMode.globalCalibration => SessionMode.globalCalibration,
      GameMode.tier => SessionMode.tier,
    };
  }
}
