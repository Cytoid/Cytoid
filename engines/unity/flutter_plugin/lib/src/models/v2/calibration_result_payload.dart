/// `calibration` field on `session.result` when `outcome.kind = "calibration"`
/// (v2 § CalibrationResultPayload). Both fields are nullable; the host decides
/// which offset (if any) to apply.
class CalibrationResultPayload {
  const CalibrationResultPayload({
    this.baseNoteOffset,
    this.levelNoteOffset,
  });

  /// Global calibrated offset.
  final double? baseNoteOffset;

  /// Level calibrated offset.
  final double? levelNoteOffset;

  factory CalibrationResultPayload.fromJson(Map<String, dynamic> json) {
    return CalibrationResultPayload(
      baseNoteOffset: _readDoubleOrNull(json, 'baseNoteOffset'),
      levelNoteOffset: _readDoubleOrNull(json, 'levelNoteOffset'),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      if (baseNoteOffset != null) 'baseNoteOffset': baseNoteOffset,
      if (levelNoteOffset != null) 'levelNoteOffset': levelNoteOffset,
    };
  }

  static double? _readDoubleOrNull(Map<String, dynamic> json, String key) {
    final v = json[key];
    if (v == null) return null;
    if (v is! num) {
      throw FormatException(
        'CalibrationResultPayload.fromJson: "$key" must be a number.',
      );
    }
    return v.toDouble();
  }
}
