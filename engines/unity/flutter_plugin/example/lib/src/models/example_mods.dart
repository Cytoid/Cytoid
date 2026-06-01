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
}