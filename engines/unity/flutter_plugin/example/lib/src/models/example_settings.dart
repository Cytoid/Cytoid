import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/foundation.dart';

import 'note_type_wire.dart';

@immutable
class ExampleSettings {
  /// Default for [hitTapticFeedback]: on for iOS, off for Android (coarse motor vibration).
  static bool get defaultHitTapticFeedback =>
      defaultTargetPlatform == TargetPlatform.iOS;

  factory ExampleSettings.initial() => ExampleSettings(
        hitTapticFeedback: defaultHitTapticFeedback,
      );

  const ExampleSettings({
    this.baseNoteOffset = 0,
    this.levelNoteOffset = 0,
    this.headsetNoteOffset = -0.05,
    this.judgmentOffset = 0,
    this.noteSize = 0,
    this.horizontalMargin = 3,
    this.verticalMargin = 3,
    this.restrictPlayAreaAspectRatio = true,
    this.coverOpacity = 0.15,
    this.musicVolume = 0.85,
    this.soundEffectsVolume = 1,
    this.hitSound = 'click1',
    this.displayStoryboardEffects = true,
    this.displayBoundaries = false,
    this.skipMusicOnCompletion = true,
    this.displayEarlyLateIndicators = true,
    this.displayNoteIds = false,
    this.useExperimentalNoteAr = false,
    this.useExperimentalNoteAnimations = true,
    this.clearEffectsSize = 0,
    this.displayProfiler = false,
    this.adaptOverlayToSafeArea = true,
    this.hitboxClick = 2,
    this.hitboxDrag = 2,
    this.hitboxHold = 2,
    this.hitboxFlick = 1,
    this.ringColor = '#FFFFFF',
    this.clickFill = '#35A7FF',
    this.clickFillAlt = '#FF5964',
    this.dragFill = '#39E59E',
    this.dragFillAlt = '#39E59E',
    this.cDragFill = '#39E59E',
    this.cDragFillAlt = '#39E59E',
    this.holdFill = '#35A7FF',
    this.holdFillAlt = '#FF5964',
    this.longHoldFill = '#F2C85A',
    this.longHoldFillAlt = '#F2C85A',
    this.flickFill = '#35A7FF',
    this.flickFillAlt = '#FF5964',
    this.useFillColorForDragChildNodes = true,
    this.holdHitSoundTiming = HoldHitSoundTiming.both,
    this.graphicsQuality = GraphicsQuality.high,
    this.hitTapticFeedback = true,
    this.useNativeAudio = false,
    this.androidDspBufferSize = -1,
  });

  final double baseNoteOffset;
  final double levelNoteOffset;
  final double headsetNoteOffset;
  final double judgmentOffset;
  final double noteSize;
  final int horizontalMargin;
  final int verticalMargin;
  final bool restrictPlayAreaAspectRatio;
  final double coverOpacity;
  final double musicVolume;
  final double soundEffectsVolume;
  final String hitSound;
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

  /// Hitbox tier: 0 small, 1 medium, 2 large.
  final int hitboxClick;
  final int hitboxDrag;
  final int hitboxHold;
  final int hitboxFlick;

  final String ringColor;
  final String clickFill;
  final String clickFillAlt;
  final String dragFill;
  final String dragFillAlt;
  final String cDragFill;
  final String cDragFillAlt;
  final String holdFill;
  final String holdFillAlt;
  final String longHoldFill;
  final String longHoldFillAlt;
  final String flickFill;
  final String flickFillAlt;
  final bool useFillColorForDragChildNodes;
  final HoldHitSoundTiming holdHitSoundTiming;
  final GraphicsQuality graphicsQuality;
  final bool hitTapticFeedback;
  final bool useNativeAudio;
  final int androidDspBufferSize;

  static const hitSoundOptions = <String>[
    'none',
    'click1',
    'click2',
    'click3',
    'shaker',
    'tambourine',
    'rim',
    'hat',
    'clap',
    'donk',
    '8bit',
    'quack',
  ];

  static const hitboxSizeLabels = ['Small', 'Medium', 'Large'];

  static const colorPresets = <String>[
    '#FFFFFF',
    '#35A7FF',
    '#39E59E',
    '#FF5964',
    '#F2C85A',
    '#000000',
  ];

  static const androidDspBufferOptions = <int>[-1, 128, 256, 512, 1024, 2048];

  ExampleSettings copyWith({
    double? baseNoteOffset,
    double? levelNoteOffset,
    double? headsetNoteOffset,
    double? judgmentOffset,
    double? noteSize,
    int? horizontalMargin,
    int? verticalMargin,
    bool? restrictPlayAreaAspectRatio,
    double? coverOpacity,
    double? musicVolume,
    double? soundEffectsVolume,
    String? hitSound,
    bool? displayStoryboardEffects,
    bool? displayBoundaries,
    bool? skipMusicOnCompletion,
    bool? displayEarlyLateIndicators,
    bool? displayNoteIds,
    bool? useExperimentalNoteAr,
    bool? useExperimentalNoteAnimations,
    double? clearEffectsSize,
    bool? displayProfiler,
    bool? adaptOverlayToSafeArea,
    int? hitboxClick,
    int? hitboxDrag,
    int? hitboxHold,
    int? hitboxFlick,
    String? ringColor,
    String? clickFill,
    String? clickFillAlt,
    String? dragFill,
    String? dragFillAlt,
    String? cDragFill,
    String? cDragFillAlt,
    String? holdFill,
    String? holdFillAlt,
    String? longHoldFill,
    String? longHoldFillAlt,
    String? flickFill,
    String? flickFillAlt,
    bool? useFillColorForDragChildNodes,
    HoldHitSoundTiming? holdHitSoundTiming,
    GraphicsQuality? graphicsQuality,
    bool? hitTapticFeedback,
    bool? useNativeAudio,
    int? androidDspBufferSize,
  }) {
    return ExampleSettings(
      baseNoteOffset: baseNoteOffset ?? this.baseNoteOffset,
      levelNoteOffset: levelNoteOffset ?? this.levelNoteOffset,
      headsetNoteOffset: headsetNoteOffset ?? this.headsetNoteOffset,
      judgmentOffset: judgmentOffset ?? this.judgmentOffset,
      noteSize: noteSize ?? this.noteSize,
      horizontalMargin: horizontalMargin ?? this.horizontalMargin,
      verticalMargin: verticalMargin ?? this.verticalMargin,
      restrictPlayAreaAspectRatio:
          restrictPlayAreaAspectRatio ?? this.restrictPlayAreaAspectRatio,
      coverOpacity: coverOpacity ?? this.coverOpacity,
      musicVolume: musicVolume ?? this.musicVolume,
      soundEffectsVolume: soundEffectsVolume ?? this.soundEffectsVolume,
      hitSound: hitSound ?? this.hitSound,
      displayStoryboardEffects:
          displayStoryboardEffects ?? this.displayStoryboardEffects,
      displayBoundaries: displayBoundaries ?? this.displayBoundaries,
      skipMusicOnCompletion:
          skipMusicOnCompletion ?? this.skipMusicOnCompletion,
      displayEarlyLateIndicators:
          displayEarlyLateIndicators ?? this.displayEarlyLateIndicators,
      displayNoteIds: displayNoteIds ?? this.displayNoteIds,
      useExperimentalNoteAr:
          useExperimentalNoteAr ?? this.useExperimentalNoteAr,
      useExperimentalNoteAnimations:
          useExperimentalNoteAnimations ?? this.useExperimentalNoteAnimations,
      clearEffectsSize: clearEffectsSize ?? this.clearEffectsSize,
      displayProfiler: displayProfiler ?? this.displayProfiler,
      adaptOverlayToSafeArea:
          adaptOverlayToSafeArea ?? this.adaptOverlayToSafeArea,
      hitboxClick: hitboxClick ?? this.hitboxClick,
      hitboxDrag: hitboxDrag ?? this.hitboxDrag,
      hitboxHold: hitboxHold ?? this.hitboxHold,
      hitboxFlick: hitboxFlick ?? this.hitboxFlick,
      ringColor: ringColor ?? this.ringColor,
      clickFill: clickFill ?? this.clickFill,
      clickFillAlt: clickFillAlt ?? this.clickFillAlt,
      dragFill: dragFill ?? this.dragFill,
      dragFillAlt: dragFillAlt ?? this.dragFillAlt,
      cDragFill: cDragFill ?? this.cDragFill,
      cDragFillAlt: cDragFillAlt ?? this.cDragFillAlt,
      holdFill: holdFill ?? this.holdFill,
      holdFillAlt: holdFillAlt ?? this.holdFillAlt,
      longHoldFill: longHoldFill ?? this.longHoldFill,
      longHoldFillAlt: longHoldFillAlt ?? this.longHoldFillAlt,
      flickFill: flickFill ?? this.flickFill,
      flickFillAlt: flickFillAlt ?? this.flickFillAlt,
      useFillColorForDragChildNodes:
          useFillColorForDragChildNodes ?? this.useFillColorForDragChildNodes,
      holdHitSoundTiming: holdHitSoundTiming ?? this.holdHitSoundTiming,
      graphicsQuality: graphicsQuality ?? this.graphicsQuality,
      hitTapticFeedback: hitTapticFeedback ?? this.hitTapticFeedback,
      useNativeAudio: useNativeAudio ?? this.useNativeAudio,
      androidDspBufferSize: androidDspBufferSize ?? this.androidDspBufferSize,
    );
  }

  Map<String, int> get _hitboxWireMap => {
    NoteTypeWire.click: hitboxClick,
    NoteTypeWire.dragChild: hitboxDrag,
    NoteTypeWire.hold: hitboxHold,
    NoteTypeWire.flick: hitboxFlick,
  };

  Map<String, String> get _noteRingColorsWire => {NoteTypeWire.click: ringColor};

  Map<String, String> get _noteFillColorsWire => {
    NoteTypeWire.click: clickFill,
    NoteTypeWire.dragChild: dragFill,
    NoteTypeWire.cDragChild: cDragFill,
    NoteTypeWire.hold: holdFill,
    NoteTypeWire.longHold: longHoldFill,
    NoteTypeWire.flick: flickFill,
  };

  Map<String, String> get _noteFillColorsAltWire => {
    NoteTypeWire.click: clickFillAlt,
    NoteTypeWire.dragChild: dragFillAlt,
    NoteTypeWire.cDragChild: cDragFillAlt,
    NoteTypeWire.hold: holdFillAlt,
    NoteTypeWire.longHold: longHoldFillAlt,
    NoteTypeWire.flick: flickFillAlt,
  };

  GameLaunchSettings toLaunchSettings() {
    return GameLaunchSettings(
      baseNoteOffset: baseNoteOffset,
      levelNoteOffset: levelNoteOffset,
      headsetNoteOffset: headsetNoteOffset,
      judgmentOffset: judgmentOffset,
      noteSize: noteSize,
      horizontalMargin: horizontalMargin,
      verticalMargin: verticalMargin,
      restrictPlayAreaAspectRatio: restrictPlayAreaAspectRatio,
      coverOpacity: coverOpacity,
      musicVolume: musicVolume,
      soundEffectsVolume: soundEffectsVolume,
      hitSound: hitSound,
      displayStoryboardEffects: displayStoryboardEffects,
      displayBoundaries: displayBoundaries,
      skipMusicOnCompletion: skipMusicOnCompletion,
      displayEarlyLateIndicators: displayEarlyLateIndicators,
      displayNoteIds: displayNoteIds,
      useExperimentalNoteAr: useExperimentalNoteAr,
      useExperimentalNoteAnimations: useExperimentalNoteAnimations,
      clearEffectsSize: clearEffectsSize,
      displayProfiler: displayProfiler,
      adaptOverlayToSafeArea: adaptOverlayToSafeArea,
      hitboxSizes: _hitboxWireMap,
      noteRingColors: _noteRingColorsWire,
      noteFillColors: _noteFillColorsWire,
      noteFillColorsAlt: _noteFillColorsAltWire,
      useFillColorForDragChildNodes: useFillColorForDragChildNodes,
      holdHitSoundTiming: holdHitSoundTiming.wireName,
      graphicsQuality: graphicsQuality.wireName,
      hitTapticFeedback: hitTapticFeedback,
      useNativeAudio: useNativeAudio,
      androidDspBufferSize: androidDspBufferSize,
    );
  }
}
