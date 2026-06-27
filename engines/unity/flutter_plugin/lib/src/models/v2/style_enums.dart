/// Graphics quality enum (v2 § Enum Values → Graphics Quality).
enum GraphicsQuality {
  veryLow,
  low,
  medium,
  high,
  ultra;

  String get wireName => name;

  static GraphicsQuality? fromWireName(String? value) {
    if (value == null) return null;
    return GraphicsQuality.values.cast<GraphicsQuality?>().firstWhere(
          (q) => q?.wireName == value,
          orElse: () => null,
        );
  }

  static const validWireNames = {'veryLow', 'low', 'medium', 'high', 'ultra'};
}

/// Hold hit sound timing enum (v2 § Enum Values → Hold Hit Sound Timing).
enum HoldHitSoundTiming {
  begin,
  end,
  both;

  String get wireName => name;

  static HoldHitSoundTiming? fromWireName(String? value) {
    if (value == null) return null;
    return HoldHitSoundTiming.values.cast<HoldHitSoundTiming?>().firstWhere(
          (t) => t?.wireName == value,
          orElse: () => null,
        );
  }

  static const validWireNames = {'begin', 'end', 'both'};
}

/// Hitbox size enum (v2 § Enum Values → Hitbox Size).
enum HitboxSize {
  small,
  medium,
  large;

  String get wireName => name;

  static HitboxSize? fromWireName(String? value) {
    if (value == null) return null;
    return HitboxSize.values.cast<HitboxSize?>().firstWhere(
          (s) => s?.wireName == value,
          orElse: () => null,
        );
  }

  static const validWireNames = {'small', 'medium', 'large'};
}

/// Note type keys (v2 § NoteStyleSettings). All 8 keys are REQUIRED in every
/// note style map.
enum NoteType {
  click,
  hold,
  longHold,
  dragHead,
  dragChild,
  flick,
  cDragHead,
  cDragChild;

  String get wireName => name;

  static NoteType? fromWireName(String? value) {
    if (value == null) return null;
    return NoteType.values.cast<NoteType?>().firstWhere(
          (n) => n?.wireName == value,
          orElse: () => null,
        );
  }

  /// All 8 keys, in spec order, for note-style map validation.
  static const requiredKeys = {
    'click',
    'hold',
    'longHold',
    'dragHead',
    'dragChild',
    'flick',
    'cDragHead',
    'cDragChild',
  };
}
