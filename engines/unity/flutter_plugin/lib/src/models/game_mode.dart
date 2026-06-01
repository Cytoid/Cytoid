enum GameMode {
  standard,
  practice,
  calibration,
  tier,
  globalCalibration;

  String get wireName => switch (this) {
    GameMode.standard => 'Standard',
    GameMode.practice => 'Practice',
    GameMode.calibration => 'Calibration',
    GameMode.tier => 'Tier',
    GameMode.globalCalibration => 'GlobalCalibration',
  };

  static GameMode? fromWireName(String? name) {
    if (name == null) return null;
    return switch (name.toLowerCase()) {
      'standard' => GameMode.standard,
      'practice' => GameMode.practice,
      'calibration' => GameMode.calibration,
      'tier' => GameMode.tier,
      'globalcalibration' => GameMode.globalCalibration,
      'global_calibration' => GameMode.globalCalibration,
      'global-calibration' => GameMode.globalCalibration,
      _ => null,
    };
  }
}
