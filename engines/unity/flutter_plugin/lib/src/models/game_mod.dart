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
}