import '_validators.dart';
import 'calibration_result_payload.dart';
import 'error_payload.dart';
import 'flags_payload.dart';
import 'level_result_payload.dart';
import 'outcome_payload.dart';
import 'result_telemetry_payload.dart';
import 'score_payload.dart';
import 'tier_result_payload.dart';

/// `session.result` payload (v2 § session.result). This is the only terminal
/// gameplay message for a session.
class SessionResultPayload {
  const SessionResultPayload({
    required this.sessionId,
    required this.mode,
    required this.mods,
    required this.outcome,
    required this.flags,
    required this.telemetry,
    required this.timestamp,
    this.level,
    this.score,
    this.calibration,
    this.tier,
    this.error,
  });

  /// Same as the envelope id.
  final String sessionId;

  /// Echo of session.start `mode`.
  final String mode;

  /// Echo of session.start `mods`. Empty array if none.
  final List<String> mods;

  /// Outcome of the session.
  final OutcomePayload outcome;

  /// Level echo. Required when `mode` is not `calibration` and a level was
  /// supplied to `session.start`.
  final LevelResultPayload? level;

  /// Score data. Includes failed runs. Omit only when no score is meaningful.
  final ScorePayload? score;

  /// Calibration offsets. Required when `outcome.kind = "calibration"`.
  final CalibrationResultPayload? calibration;

  /// Tier stage ending state. Required when `mode = "tier"`, regardless of
  /// outcome kind.
  final TierResultPayload? tier;

  /// Flags.
  final FlagsPayload flags;

  /// Telemetry summary carried on every result.
  final ResultTelemetryPayload telemetry;

  /// Required when `outcome.kind = "rejected"`.
  final GameCoreError? error;

  /// Unix epoch milliseconds.
  final int timestamp;

  factory SessionResultPayload.fromJson(Map<String, dynamic> json) {
    final outcome = OutcomePayload.fromJson(_asMap(json, 'outcome'));
    final kind = outcome.kind;
    final errorJson = json['error'];

    GameCoreError? error;
    if (errorJson is Map) {
      error = GameCoreError.fromJson(Map<String, dynamic>.from(errorJson));
    } else if (errorJson != null) {
      throw FormatException(
        'SessionResultPayload.fromJson: "error" must be an object when present.',
      );
    }
    if (kind == OutcomePayload.rejectedKind) {
      if (error == null) {
        throw FormatException(
          'SessionResultPayload.fromJson: "error" is required when '
          'outcome.kind="$kind".',
        );
      }
    }
    if (kind == OutcomePayload.calibrationKind) {
      if (json['calibration'] is! Map) {
        throw FormatException(
          'SessionResultPayload.fromJson: "calibration" is required when '
          'outcome.kind="calibration".',
        );
      }
    }
    if (json['mode'] == 'tier' && json['tier'] is! Map) {
      throw FormatException(
        'SessionResultPayload.fromJson: "tier" is required when mode="tier".',
      );
    }

    return SessionResultPayload(
      sessionId: json['sessionId'] as String,
      mode: json['mode'] as String,
      mods: _readStringList(json['mods']),
      outcome: outcome,
      level: readOptionalObject(
        json,
        'level',
        'SessionResultPayload.fromJson',
        LevelResultPayload.fromJson,
      ),
      score: readOptionalObject(
        json,
        'score',
        'SessionResultPayload.fromJson',
        ScorePayload.fromJson,
      ),
      calibration: readOptionalObject(
        json,
        'calibration',
        'SessionResultPayload.fromJson',
        CalibrationResultPayload.fromJson,
      ),
      tier: readOptionalObject(
        json,
        'tier',
        'SessionResultPayload.fromJson',
        TierResultPayload.fromJson,
      ),
      flags: FlagsPayload.fromJson(_asMap(json, 'flags')),
      telemetry: ResultTelemetryPayload.fromJson(_asMap(json, 'telemetry')),
      error: error,
      timestamp: readRequiredInt(json, 'timestamp', 'SessionResultPayload'),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'sessionId': sessionId,
      'mode': mode,
      'mods': mods,
      'outcome': outcome.toJson(),
      if (level != null) 'level': level!.toJson(),
      if (score != null) 'score': score!.toJson(),
      if (calibration != null) 'calibration': calibration!.toJson(),
      if (tier != null) 'tier': tier!.toJson(),
      'flags': flags.toJson(),
      'telemetry': telemetry.toJson(),
      if (error != null) 'error': error!.toJson(),
      'timestamp': timestamp,
    };
  }

  static Map<String, dynamic> _asMap(Map<String, dynamic> json, String key) {
    final v = json[key];
    if (v is! Map) {
      throw FormatException(
        'SessionResultPayload.fromJson: "$key" must be an object.',
      );
    }
    return Map<String, dynamic>.from(v);
  }

  static List<String> _readStringList(Object? value) {
    if (value == null) return const [];
    if (value is! List) {
      throw FormatException(
        'SessionResultPayload.fromJson: "mods" must be an array.',
      );
    }
    return value.map((e) => e as String).toList(growable: false);
  }
}
