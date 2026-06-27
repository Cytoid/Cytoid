/// `session.start.mode` enum (v2 § Session Modes). Lower camel on the wire.
///
/// OD5 (decisions.md): modes are ranked|practice|calibration|globalCalibration|tier
/// with no `standard`. v1's `GameMode.standard` is replaced by `ranked`.
enum SessionMode {
  ranked,
  practice,
  calibration,
  globalCalibration,
  tier;

  /// v2 wire form, e.g. `ranked` → `ranked`.
  String get wireName => name;

  static SessionMode? fromWireName(String? name) {
    if (name == null) return null;
    return SessionMode.values.cast<SessionMode?>().firstWhere(
          (m) => m?.wireName == name,
          orElse: () => null,
        );
  }

  static const validWireNames = {
    'ranked',
    'practice',
    'calibration',
    'globalCalibration',
    'tier',
  };
}
