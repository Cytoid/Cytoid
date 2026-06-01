/// Unity [NoteType] enum ids used in launch settings dictionaries.
abstract final class NoteTypeWire {
  static const click = '0';
  static const hold = '1';
  static const longHold = '2';
  static const dragHead = '3';
  static const dragChild = '4';
  static const flick = '5';
  static const cDragHead = '6';
  static const cDragChild = '7';
}

enum HoldHitSoundTiming { begin, end, both }

enum GraphicsQuality { veryLow, low, medium, high, ultra }

extension HoldHitSoundTimingWire on HoldHitSoundTiming {
  String get wireName => switch (this) {
    HoldHitSoundTiming.begin => 'Begin',
    HoldHitSoundTiming.end => 'End',
    HoldHitSoundTiming.both => 'Both',
  };
}

extension GraphicsQualityWire on GraphicsQuality {
  String get wireName => switch (this) {
    GraphicsQuality.veryLow => 'VeryLow',
    GraphicsQuality.low => 'Low',
    GraphicsQuality.medium => 'Medium',
    GraphicsQuality.high => 'High',
    GraphicsQuality.ultra => 'Ultra',
  };
}
