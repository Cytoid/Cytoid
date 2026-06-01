import 'dart:convert';
import 'dart:typed_data';

import 'game_launch_assets.dart';
import 'game_launch_settings.dart';
import 'game_mode.dart';
import 'tier_play_launch.dart';

/// Data required to start a gameplay session.
class GameLaunchPayload {
  const GameLaunchPayload({
    required this.levelMetaJson,
    required this.selectedDifficulty,
    this.chartText,
    this.musicBytes,
    this.musicFormat = 'mp3',
    this.storyboardText,
    this.settings,
    this.mods = const [],
    this.assets,
    this.gameMode,
    this.tierPlay,
  });

  final String levelMetaJson;
  final String selectedDifficulty;
  final String? chartText;
  final Uint8List? musicBytes;
  final String musicFormat;
  final String? storyboardText;
  final GameLaunchSettings? settings;
  final List<String> mods;
  final GameLaunchAssets? assets;
  final GameMode? gameMode;
  final TierPlayLaunch? tierPlay;

  factory GameLaunchPayload.fromJson(Map<String, dynamic> json) {
    return GameLaunchPayload(
      levelMetaJson: json['levelMetaJson'] as String,
      selectedDifficulty: json['selectedDifficulty'] as String,
      chartText: json['chartText'] as String?,
      musicBytes: _readBytes(json['musicBytes']),
      musicFormat: json['musicFormat'] as String? ?? 'mp3',
      storyboardText: json['storyboardText'] as String?,
      settings: json['settings'] is Map<String, dynamic>
          ? GameLaunchSettings.fromJson(
              json['settings'] as Map<String, dynamic>,
            )
          : null,
      mods: _readStringList(json['mods']),
      assets: json['assets'] is Map<String, dynamic>
          ? GameLaunchAssets.fromJson(json['assets'] as Map<String, dynamic>)
          : null,
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
      if (chartText != null) 'chartText': chartText,
      if (musicBytes != null) 'musicBytes': base64Encode(musicBytes!),
      'musicFormat': musicFormat,
      if (storyboardText != null) 'storyboardText': storyboardText,
      if (settings != null) 'settings': settings!.toJson(),
      if (mods.isNotEmpty) 'mods': mods,
      if (assets != null) 'assets': assets!.toJson(),
      if (gameMode != null) 'gameMode': gameMode!.wireName,
      if (tierPlay != null) 'tierPlay': tierPlay!.toJson(),
    };
  }

  static Uint8List? _readBytes(Object? value) {
    if (value == null) {
      return null;
    }
    if (value is String) {
      return base64Decode(value);
    }
    if (value is List) {
      return Uint8List.fromList(value.cast<int>());
    }
    throw FormatException('musicBytes must be base64 string or byte array.');
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
