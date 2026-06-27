import '../game_mod.dart';
import 'level_payload.dart';
import 'session_mode.dart';
import 'session_options.dart';
import 'settings_payload.dart';
import 'tier_launch_payload.dart';

/// `session.start` payload (v2 § session.start). One session per envelope.
class SessionLaunchPayload {
  const SessionLaunchPayload({
    required this.mode,
    required this.level,
    required this.mods,
    required this.settings,
    required this.options,
    this.tier,
  });

  /// Session mode.
  final SessionMode mode;

  /// Level metadata, selected difficulty, and VFS assets.
  final LevelPayload level;

  /// Typed mod ids. Empty array if no mods.
  final List<GameMod> mods;

  /// Complete settings snapshot for this session.
  final SettingsPayload settings;

  /// Session intent and telemetry options.
  final SessionOptions options;

  /// Tier stage input. Required when `mode = SessionMode.tier`.
  final TierLaunchPayload? tier;

  factory SessionLaunchPayload.fromJson(Map<String, dynamic> json) {
    final mode = SessionMode.fromWireName(json['mode'] as String?);
    if (mode == null) {
      throw FormatException(
        'SessionLaunchPayload.fromJson: "mode" must be one of '
        '${SessionMode.validWireNames}.',
      );
    }
    if (mode == SessionMode.tier && json['tier'] is! Map) {
      throw FormatException(
        'SessionLaunchPayload.fromJson: "tier" is required when '
        'mode="tier".',
      );
    }
    final levelJson = json['level'];
    if (levelJson is! Map) {
      throw FormatException(
        'SessionLaunchPayload.fromJson: "level" must be an object.',
      );
    }
    final settingsJson = json['settings'];
    if (settingsJson is! Map) {
      throw FormatException(
        'SessionLaunchPayload.fromJson: "settings" must be an object.',
      );
    }
    final optionsJson = json['options'];
    if (optionsJson is! Map) {
      throw FormatException(
        'SessionLaunchPayload.fromJson: "options" must be an object.',
      );
    }
    final modsJson = json['mods'];
    if (modsJson is! List) {
      throw FormatException(
        'SessionLaunchPayload.fromJson: "mods" must be an array.',
      );
    }
    final mods = modsJson.map<GameMod>((raw) {
      final wire = raw as String;
      return GameMod.fromWireNameV2(wire) ??
          (throw FormatException(
            'SessionLaunchPayload.fromJson: unknown mod id "$wire".',
          ));
    }).toList(growable: false);

    return SessionLaunchPayload(
      mode: mode,
      level: LevelPayload.fromJson(Map<String, dynamic>.from(levelJson)),
      mods: mods,
      settings: SettingsPayload.fromJson(Map<String, dynamic>.from(settingsJson)),
      options: SessionOptions.fromJson(Map<String, dynamic>.from(optionsJson)),
      tier: json['tier'] is Map
          ? TierLaunchPayload.fromJson(
              Map<String, dynamic>.from(json['tier'] as Map),
            )
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'mode': mode.wireName,
      'level': level.toJson(),
      'mods': mods.map((m) => m.v2WireName).toList(growable: false),
      'settings': settings.toJson(),
      'options': options.toJson(),
      if (tier != null) 'tier': tier!.toJson(),
    };
  }
}
