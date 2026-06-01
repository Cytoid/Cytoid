/// Player settings applied when starting a chart.
class GameLaunchSettings {
  const GameLaunchSettings({
    this.baseNoteOffset,
    this.levelNoteOffset,
    this.headsetNoteOffset,
    this.judgmentOffset,
    this.noteSize,
    this.horizontalMargin,
    this.verticalMargin,
    this.restrictPlayAreaAspectRatio,
    this.coverOpacity,
    this.musicVolume,
    this.soundEffectsVolume,
    this.hitSound,
    this.displayStoryboardEffects,
    this.displayBoundaries,
    this.skipMusicOnCompletion,
    this.displayEarlyLateIndicators,
    this.displayNoteIds,
    this.useExperimentalNoteAr,
    this.useExperimentalNoteAnimations,
    this.clearEffectsSize,
    this.displayProfiler,
    this.adaptOverlayToSafeArea,
    this.hitboxSizes,
    this.noteRingColors,
    this.noteFillColors,
    this.noteFillColorsAlt,
    this.useFillColorForDragChildNodes,
    this.holdHitSoundTiming,
    this.graphicsQuality,
    this.hitTapticFeedback,
    this.useNativeAudio,
    this.androidDspBufferSize,
  });

  final double? baseNoteOffset;
  final double? levelNoteOffset;
  final double? headsetNoteOffset;
  final double? judgmentOffset;
  final double? noteSize;
  final int? horizontalMargin;
  final int? verticalMargin;
  final bool? restrictPlayAreaAspectRatio;
  final double? coverOpacity;
  final double? musicVolume;
  final double? soundEffectsVolume;
  final String? hitSound;
  final bool? displayStoryboardEffects;
  final bool? displayBoundaries;
  final bool? skipMusicOnCompletion;
  final bool? displayEarlyLateIndicators;
  final bool? displayNoteIds;
  final bool? useExperimentalNoteAr;
  final bool? useExperimentalNoteAnimations;
  final double? clearEffectsSize;
  final bool? displayProfiler;
  final bool? adaptOverlayToSafeArea;
  final Map<String, int>? hitboxSizes;
  final Map<String, String>? noteRingColors;
  final Map<String, String>? noteFillColors;
  final Map<String, String>? noteFillColorsAlt;
  final bool? useFillColorForDragChildNodes;
  final String? holdHitSoundTiming;
  final String? graphicsQuality;
  final bool? hitTapticFeedback;
  final bool? useNativeAudio;
  final int? androidDspBufferSize;

  factory GameLaunchSettings.fromJson(Map<String, dynamic> json) {
    return GameLaunchSettings(
      baseNoteOffset: _readDouble(json, 'baseNoteOffset'),
      levelNoteOffset: _readDouble(json, 'levelNoteOffset'),
      headsetNoteOffset: _readDouble(json, 'headsetNoteOffset'),
      judgmentOffset: _readDouble(json, 'judgmentOffset'),
      noteSize: _readDouble(json, 'noteSize'),
      horizontalMargin: _readInt(json, 'horizontalMargin'),
      verticalMargin: _readInt(json, 'verticalMargin'),
      restrictPlayAreaAspectRatio: json['restrictPlayAreaAspectRatio'] as bool?,
      coverOpacity: _readDouble(json, 'coverOpacity'),
      musicVolume: _readDouble(json, 'musicVolume'),
      soundEffectsVolume: _readDouble(json, 'soundEffectsVolume'),
      hitSound: json['hitSound'] as String?,
      displayStoryboardEffects: json['displayStoryboardEffects'] as bool?,
      displayBoundaries: json['displayBoundaries'] as bool?,
      skipMusicOnCompletion: json['skipMusicOnCompletion'] as bool?,
      displayEarlyLateIndicators: json['displayEarlyLateIndicators'] as bool?,
      displayNoteIds: json['displayNoteIds'] as bool?,
      useExperimentalNoteAr: json['useExperimentalNoteAr'] as bool?,
      useExperimentalNoteAnimations:
          json['useExperimentalNoteAnimations'] as bool?,
      clearEffectsSize: _readDouble(json, 'clearEffectsSize'),
      displayProfiler: json['displayProfiler'] as bool?,
      adaptOverlayToSafeArea: json['adaptOverlayToSafeArea'] as bool?,
      hitboxSizes: _readStringIntMap(json, 'hitboxSizes'),
      noteRingColors: _readStringStringMap(json, 'noteRingColors'),
      noteFillColors: _readStringStringMap(json, 'noteFillColors'),
      noteFillColorsAlt: _readStringStringMap(json, 'noteFillColorsAlt'),
      useFillColorForDragChildNodes:
          json['useFillColorForDragChildNodes'] as bool?,
      holdHitSoundTiming: json['holdHitSoundTiming'] as String?,
      graphicsQuality: json['graphicsQuality'] as String?,
      hitTapticFeedback: json['hitTapticFeedback'] as bool?,
      useNativeAudio: json['useNativeAudio'] as bool?,
      androidDspBufferSize: _readInt(json, 'androidDspBufferSize'),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      if (baseNoteOffset != null) 'baseNoteOffset': baseNoteOffset,
      if (levelNoteOffset != null) 'levelNoteOffset': levelNoteOffset,
      if (headsetNoteOffset != null) 'headsetNoteOffset': headsetNoteOffset,
      if (judgmentOffset != null) 'judgmentOffset': judgmentOffset,
      if (noteSize != null) 'noteSize': noteSize,
      if (horizontalMargin != null) 'horizontalMargin': horizontalMargin,
      if (verticalMargin != null) 'verticalMargin': verticalMargin,
      if (restrictPlayAreaAspectRatio != null)
        'restrictPlayAreaAspectRatio': restrictPlayAreaAspectRatio,
      if (coverOpacity != null) 'coverOpacity': coverOpacity,
      if (musicVolume != null) 'musicVolume': musicVolume,
      if (soundEffectsVolume != null) 'soundEffectsVolume': soundEffectsVolume,
      if (hitSound != null) 'hitSound': hitSound,
      if (displayStoryboardEffects != null)
        'displayStoryboardEffects': displayStoryboardEffects,
      if (displayBoundaries != null) 'displayBoundaries': displayBoundaries,
      if (skipMusicOnCompletion != null)
        'skipMusicOnCompletion': skipMusicOnCompletion,
      if (displayEarlyLateIndicators != null)
        'displayEarlyLateIndicators': displayEarlyLateIndicators,
      if (displayNoteIds != null) 'displayNoteIds': displayNoteIds,
      if (useExperimentalNoteAr != null)
        'useExperimentalNoteAr': useExperimentalNoteAr,
      if (useExperimentalNoteAnimations != null)
        'useExperimentalNoteAnimations': useExperimentalNoteAnimations,
      if (clearEffectsSize != null) 'clearEffectsSize': clearEffectsSize,
      if (displayProfiler != null) 'displayProfiler': displayProfiler,
      if (adaptOverlayToSafeArea != null)
        'adaptOverlayToSafeArea': adaptOverlayToSafeArea,
      if (hitboxSizes != null) 'hitboxSizes': hitboxSizes,
      if (noteRingColors != null) 'noteRingColors': noteRingColors,
      if (noteFillColors != null) 'noteFillColors': noteFillColors,
      if (noteFillColorsAlt != null) 'noteFillColorsAlt': noteFillColorsAlt,
      if (useFillColorForDragChildNodes != null)
        'useFillColorForDragChildNodes': useFillColorForDragChildNodes,
      if (holdHitSoundTiming != null) 'holdHitSoundTiming': holdHitSoundTiming,
      if (graphicsQuality != null) 'graphicsQuality': graphicsQuality,
      if (hitTapticFeedback != null) 'hitTapticFeedback': hitTapticFeedback,
      if (useNativeAudio != null) 'useNativeAudio': useNativeAudio,
      if (androidDspBufferSize != null)
        'androidDspBufferSize': androidDspBufferSize,
    };
  }

  static double? _readDouble(Map<String, dynamic> json, String key) {
    final value = json[key];
    if (value == null) {
      return null;
    }
    if (value is num) {
      return value.toDouble();
    }
    throw FormatException('Expected number for "$key".');
  }

  static int? _readInt(Map<String, dynamic> json, String key) {
    final value = json[key];
    if (value == null) {
      return null;
    }
    if (value is num) {
      return value.toInt();
    }
    throw FormatException('Expected integer for "$key".');
  }

  static Map<String, int>? _readStringIntMap(
    Map<String, dynamic> json,
    String key,
  ) {
    final value = json[key];
    if (value == null) {
      return null;
    }
    if (value is! Map) {
      throw FormatException('Expected object for "$key".');
    }
    return value.map((k, v) {
      if (v is! num) {
        throw FormatException('Expected number values in "$key".');
      }
      return MapEntry(k.toString(), v.toInt());
    });
  }

  static Map<String, String>? _readStringStringMap(
    Map<String, dynamic> json,
    String key,
  ) {
    final value = json[key];
    if (value == null) {
      return null;
    }
    if (value is! Map) {
      throw FormatException('Expected object for "$key".');
    }
    return value.map((k, v) => MapEntry(k.toString(), v.toString()));
  }
}
