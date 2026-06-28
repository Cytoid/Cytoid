import 'style_enums.dart';

import '_validators.dart';

/// `session.start.settings` payload (v2 § SettingsPayload). All 5 groups are
/// required in `session.start`; `settings.apply` allows partial updates.
class SettingsPayload {
  const SettingsPayload({
    required this.profile,
    required this.runtime,
    required this.visual,
    required this.audio,
    required this.noteStyle,
  });

  final ProfileSettings profile;
  final RuntimeSettings runtime;
  final VisualSettings visual;
  final AudioSettings audio;
  final NoteStyleSettings noteStyle;

  factory SettingsPayload.fromJson(Map<String, dynamic> json) {
    return SettingsPayload(
      profile: ProfileSettings.fromJson(_group(json, 'profile')),
      runtime: RuntimeSettings.fromJson(_group(json, 'runtime')),
      visual: VisualSettings.fromJson(_group(json, 'visual')),
      audio: AudioSettings.fromJson(_group(json, 'audio')),
      noteStyle: NoteStyleSettings.fromJson(_group(json, 'noteStyle')),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'profile': profile.toJson(),
      'runtime': runtime.toJson(),
      'visual': visual.toJson(),
      'audio': audio.toJson(),
      'noteStyle': noteStyle.toJson(),
    };
  }

  static Map<String, dynamic> _group(Map<String, dynamic> json, String key) {
    final v = json[key];
    if (v is! Map) {
      throw FormatException(
        'SettingsPayload.fromJson: "$key" must be an object.',
      );
    }
    return Map<String, dynamic>.from(v);
  }
}

/// Profile settings (v2 § ProfileSettings).
class ProfileSettings {
  const ProfileSettings({
    required this.language,
    required this.baseNoteOffset,
    required this.headsetNoteOffset,
    required this.judgmentOffset,
    required this.hitTapticFeedback,
    required this.menuTapticFeedback,
    this.levelNoteOffset,
  });

  final String language;
  final double baseNoteOffset;
  final double? levelNoteOffset;
  final double headsetNoteOffset;
  final double judgmentOffset;
  final bool hitTapticFeedback;
  final bool menuTapticFeedback;

  factory ProfileSettings.fromJson(Map<String, dynamic> json) {
    return ProfileSettings(
      language: json['language'] as String,
      baseNoteOffset: _readDouble(json, 'baseNoteOffset'),
      levelNoteOffset: _readDoubleOrNull(json, 'levelNoteOffset'),
      headsetNoteOffset: _readDouble(json, 'headsetNoteOffset'),
      judgmentOffset: _readDouble(json, 'judgmentOffset'),
      hitTapticFeedback: json['hitTapticFeedback'] as bool,
      menuTapticFeedback: json['menuTapticFeedback'] as bool,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'language': language,
      'baseNoteOffset': baseNoteOffset,
      if (levelNoteOffset != null) 'levelNoteOffset': levelNoteOffset,
      'headsetNoteOffset': headsetNoteOffset,
      'judgmentOffset': judgmentOffset,
      'hitTapticFeedback': hitTapticFeedback,
      'menuTapticFeedback': menuTapticFeedback,
    };
  }
}

/// Runtime settings (v2 § RuntimeSettings). `musicVolume` and
/// `soundEffectsVolume` are realtime-safe (see § Settings Realtime Safety).
class RuntimeSettings {
  const RuntimeSettings({
    required this.musicVolume,
    required this.soundEffectsVolume,
  });

  final double musicVolume;
  final double soundEffectsVolume;

  factory RuntimeSettings.fromJson(Map<String, dynamic> json) {
    return RuntimeSettings(
      musicVolume: _readDouble(json, 'musicVolume'),
      soundEffectsVolume: _readDouble(json, 'soundEffectsVolume'),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'musicVolume': musicVolume,
      'soundEffectsVolume': soundEffectsVolume,
    };
  }
}

/// Visual settings (v2 § VisualSettings).
class VisualSettings {
  const VisualSettings({
    required this.noteSize,
    required this.horizontalMargin,
    required this.verticalMargin,
    required this.restrictPlayAreaAspectRatio,
    required this.coverOpacity,
    required this.displayStoryboardEffects,
    required this.displayBoundaries,
    required this.skipMusicOnCompletion,
    required this.displayEarlyLateIndicators,
    required this.displayNoteIds,
    required this.useExperimentalNoteAr,
    required this.useExperimentalNoteAnimations,
    required this.clearEffectsSize,
    required this.displayProfiler,
    required this.adaptOverlayToSafeArea,
    required this.graphicsQuality,
  });

  final double noteSize;
  final int horizontalMargin;
  final int  verticalMargin;
  final bool restrictPlayAreaAspectRatio;
  final double coverOpacity;
  final bool displayStoryboardEffects;
  final bool displayBoundaries;
  final bool skipMusicOnCompletion;
  final bool displayEarlyLateIndicators;
  final bool displayNoteIds;
  final bool useExperimentalNoteAr;
  final bool useExperimentalNoteAnimations;
  final double clearEffectsSize;
  final bool displayProfiler;
  final bool adaptOverlayToSafeArea;
  final GraphicsQuality graphicsQuality;

  factory VisualSettings.fromJson(Map<String, dynamic> json) {
    final gq = GraphicsQuality.fromWireName(json['graphicsQuality'] as String?);
    if (gq == null) {
      throw FormatException(
        'VisualSettings.fromJson: "graphicsQuality" must be one of '
        '${GraphicsQuality.validWireNames}.',
      );
    }
    return VisualSettings(
      noteSize: _readDouble(json, 'noteSize'),
      horizontalMargin: _readInt(json, 'horizontalMargin'),
      verticalMargin: _readInt(json, 'verticalMargin'),
      restrictPlayAreaAspectRatio: json['restrictPlayAreaAspectRatio'] as bool,
      coverOpacity: _readDouble(json, 'coverOpacity'),
      displayStoryboardEffects: json['displayStoryboardEffects'] as bool,
      displayBoundaries: json['displayBoundaries'] as bool,
      skipMusicOnCompletion: json['skipMusicOnCompletion'] as bool,
      displayEarlyLateIndicators: json['displayEarlyLateIndicators'] as bool,
      displayNoteIds: json['displayNoteIds'] as bool,
      useExperimentalNoteAr: json['useExperimentalNoteAr'] as bool,
      useExperimentalNoteAnimations:
          json['useExperimentalNoteAnimations'] as bool,
      clearEffectsSize: _readDouble(json, 'clearEffectsSize'),
      displayProfiler: json['displayProfiler'] as bool,
      adaptOverlayToSafeArea: json['adaptOverlayToSafeArea'] as bool,
      graphicsQuality: gq,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'noteSize': noteSize,
      'horizontalMargin': horizontalMargin,
      'verticalMargin': verticalMargin,
      'restrictPlayAreaAspectRatio': restrictPlayAreaAspectRatio,
      'coverOpacity': coverOpacity,
      'displayStoryboardEffects': displayStoryboardEffects,
      'displayBoundaries': displayBoundaries,
      'skipMusicOnCompletion': skipMusicOnCompletion,
      'displayEarlyLateIndicators': displayEarlyLateIndicators,
      'displayNoteIds': displayNoteIds,
      'useExperimentalNoteAr': useExperimentalNoteAr,
      'useExperimentalNoteAnimations': useExperimentalNoteAnimations,
      'clearEffectsSize': clearEffectsSize,
      'displayProfiler': displayProfiler,
      'adaptOverlayToSafeArea': adaptOverlayToSafeArea,
      'graphicsQuality': graphicsQuality.wireName,
    };
  }
}

/// Audio settings (v2 § AudioSettings).
class AudioSettings {
  const AudioSettings({
    required this.hitSound,
    required this.holdHitSoundTiming,
    required this.useNativeAudio,
    required this.androidDspBufferSize,
  });

  final String hitSound;
  final HoldHitSoundTiming holdHitSoundTiming;
  final bool useNativeAudio;
  final int androidDspBufferSize;

  factory AudioSettings.fromJson(Map<String, dynamic> json) {
    final t = HoldHitSoundTiming.fromWireName(
      json['holdHitSoundTiming'] as String?,
    );
    if (t == null) {
      throw FormatException(
        'AudioSettings.fromJson: "holdHitSoundTiming" must be one of '
        '${HoldHitSoundTiming.validWireNames}.',
      );
    }
    return AudioSettings(
      hitSound: json['hitSound'] as String,
      holdHitSoundTiming: t,
      useNativeAudio: json['useNativeAudio'] as bool,
      androidDspBufferSize: _readInt(json, 'androidDspBufferSize'),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'hitSound': hitSound,
      'holdHitSoundTiming': holdHitSoundTiming.wireName,
      'useNativeAudio': useNativeAudio,
      'androidDspBufferSize': androidDspBufferSize,
    };
  }
}

/// Note style settings (v2 § NoteStyleSettings). All 8 note type keys are
/// REQUIRED in every map (`hitboxSizes`, `ringColors`, `fillColors`,
/// `fillColorsAlt`); sparse maps MUST be rejected by the engine.
class NoteStyleSettings {
  const NoteStyleSettings({
    required this.hitboxSizes,
    required this.ringColors,
    required this.fillColors,
    required this.fillColorsAlt,
    required this.useFillColorForDragChildNodes,
  });

  final Map<String, HitboxSize> hitboxSizes;
  final Map<String, String> ringColors;
  final Map<String, String> fillColors;
  final Map<String, String> fillColorsAlt;
  final bool useFillColorForDragChildNodes;

  factory NoteStyleSettings.fromJson(Map<String, dynamic> json) {
    final hitbox = _readMap(json, 'hitboxSizes', validateNoteKeys: true);
    return NoteStyleSettings(
      hitboxSizes: hitbox.map(
        (k, v) => MapEntry(
          k,
          HitboxSize.fromWireName(v as String?) ??
              (throw FormatException(
                'NoteStyleSettings.fromJson: hitboxSizes["$k"]="$v" is not '
                'one of ${HitboxSize.validWireNames}.',
              )),
        ),
      ),
      ringColors: _readStringMap(json, 'ringColors'),
      fillColors: _readStringMap(json, 'fillColors'),
      fillColorsAlt: _readStringMap(json, 'fillColorsAlt'),
      useFillColorForDragChildNodes:
          json['useFillColorForDragChildNodes'] as bool,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'hitboxSizes': hitboxSizes.map((k, v) => MapEntry(k, v.wireName)),
      'ringColors': ringColors,
      'fillColors': fillColors,
      'fillColorsAlt': fillColorsAlt,
      'useFillColorForDragChildNodes': useFillColorForDragChildNodes,
    };
  }
}

// --- Shared helpers ---

double _readDouble(Map<String, dynamic> json, String key) {
  final v = json[key];
  if (v is! num) {
    throw FormatException('Settings: "$key" must be a number.');
  }
  return v.toDouble();
}

double? _readDoubleOrNull(Map<String, dynamic> json, String key) {
  final v = json[key];
  if (v == null) return null;
  if (v is! num) {
    throw FormatException('Settings: "$key" must be a number.');
  }
  return v.toDouble();
}

int _readInt(Map<String, dynamic> json, String key) {
  final v = json[key];
  if (v is! int) {
    throw FormatException('Settings: "$key" must be an integer.');
  }
  return v;
}

Map<String, dynamic> _readMap(
  Map<String, dynamic> json,
  String key, {
  required bool validateNoteKeys,
}) {
  final v = json[key];
  if (v is! Map) {
    throw FormatException('Settings: "$key" must be an object.');
  }
  final map = Map<String, dynamic>.from(v);
  if (validateNoteKeys) {
    final missing = NoteType.requiredKeys.difference(map.keys.toSet());
    if (missing.isNotEmpty) {
      throw FormatException(
        'Settings: "$key" is missing required note type keys: $missing.',
      );
    }
  }
  return map;
}

Map<String, String> _readStringMap(Map<String, dynamic> json, String key) {
  final map = readStringMapEntries(json[key], key, 'Settings');
  final missing = NoteType.requiredKeys.difference(map.keys.toSet());
  if (missing.isNotEmpty) {
    throw FormatException(
      'Settings: "$key" is missing required note type keys: $missing.',
    );
  }
  return map;
}
