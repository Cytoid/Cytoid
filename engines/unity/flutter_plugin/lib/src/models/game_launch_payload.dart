import 'game_launch_assets.dart';
import 'game_launch_settings.dart';
import 'game_mode.dart';
import 'tier_play_launch.dart';

/// Data required to start a gameplay session.
class GameLaunchPayload {
  const GameLaunchPayload({
    required this.levelMetaJson,
    required this.selectedDifficulty,
    required this.assets,
    this.settings,
    this.mods = const [],
    this.gameMode,
    this.tierPlay,
  });

  final String levelMetaJson;
  final String selectedDifficulty;
  final GameLaunchAssets assets;
  final GameLaunchSettings? settings;
  final List<String> mods;
  final GameMode? gameMode;
  final TierPlayLaunch? tierPlay;

  factory GameLaunchPayload.fromJson(Map<String, dynamic> json) {
    final assetsJson = json['assets'];
    if (assetsJson is! Map) {
      throw const FormatException(
        "Invalid or missing 'assets' field: expected Map<String, dynamic>",
      );
    }

    return GameLaunchPayload(
      levelMetaJson: json['levelMetaJson'] as String,
      selectedDifficulty: json['selectedDifficulty'] as String,
      assets: GameLaunchAssets.fromJson(Map<String, dynamic>.from(assetsJson)),
      settings: json['settings'] is Map<String, dynamic>
          ? GameLaunchSettings.fromJson(
              json['settings'] as Map<String, dynamic>,
            )
          : null,
      mods: _readStringList(json['mods']),
      gameMode: GameMode.fromWireName(json['gameMode'] as String?),
      tierPlay: json['tierPlay'] is Map<String, dynamic>
          ? TierPlayLaunch.fromJson(json['tierPlay'] as Map<String, dynamic>)
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'levelMetaJson': levelMetaJson,
      'selectedDifficulty': selectedDifficulty,
      'assets': assets.toJson(),
      if (settings != null) 'settings': settings!.toJson(),
      if (mods.isNotEmpty) 'mods': mods,
      if (gameMode != null) 'gameMode': gameMode!.wireName,
      if (tierPlay != null) 'tierPlay': tierPlay!.toJson(),
    };
  }

  static List<String> _readStringList(Object? value) {
    if (value == null) {
      return const [];
    }
    if (value is List) {
      return value.map((item) => item as String).toList();
    }
    throw FormatException('mods must be a string array.');
  }
}
