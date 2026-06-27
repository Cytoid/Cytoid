/// Game modifiers that affect gameplay difficulty or behavior.
enum GameMod {
  flipX,
  flipY,
  flipAll,
  slow,
  fast,
  fc,
  ap,
  hard,
  exHard,
  hideScanline,
  hideNotes,
  autoDrag,
  autoHold,
  autoFlick,
  auto;

  /// The PascalCase C# wire format name.
  String get wireName {
    switch (this) {
      case GameMod.flipX:
        return 'FlipX';
      case GameMod.flipY:
        return 'FlipY';
      case GameMod.flipAll:
        return 'FlipAll';
      case GameMod.slow:
        return 'Slow';
      case GameMod.fast:
        return 'Fast';
      case GameMod.fc:
        return 'FC';
      case GameMod.ap:
        return 'AP';
      case GameMod.hard:
        return 'Hard';
      case GameMod.exHard:
        return 'ExHard';
      case GameMod.hideScanline:
        return 'HideScanline';
      case GameMod.hideNotes:
        return 'HideNotes';
      case GameMod.autoDrag:
        return 'AutoDrag';
      case GameMod.autoHold:
        return 'AutoHold';
      case GameMod.autoFlick:
        return 'AutoFlick';
      case GameMod.auto:
        return 'Auto';
    }
  }

  /// v2 lower camel wire name (e.g. `flipX`, `autoDrag`) per v2 § Mods.
  /// v1 [wireName] is retained for backwards compatibility with code paths
  /// that still speak the PascalCase C# format.
  String get v2WireName {
    switch (this) {
      case GameMod.flipX:
        return 'flipX';
      case GameMod.flipY:
        return 'flipY';
      case GameMod.flipAll:
        return 'flipAll';
      case GameMod.slow:
        return 'slow';
      case GameMod.fast:
        return 'fast';
      case GameMod.fc:
        return 'fc';
      case GameMod.ap:
        return 'ap';
      case GameMod.hard:
        return 'hard';
      case GameMod.exHard:
        return 'exHard';
      case GameMod.hideScanline:
        return 'hideScanline';
      case GameMod.hideNotes:
        return 'hideNotes';
      case GameMod.autoDrag:
        return 'autoDrag';
      case GameMod.autoHold:
        return 'autoHold';
      case GameMod.autoFlick:
        return 'autoFlick';
      case GameMod.auto:
        return 'auto';
    }
  }

  /// Parse a v2 lower camel mod id. Returns null for unknown ids.
  static GameMod? fromWireNameV2(String wire) {
    return GameMod.values.cast<GameMod?>().firstWhere(
          (m) => m?.v2WireName == wire,
          orElse: () => null,
        );
  }
}
