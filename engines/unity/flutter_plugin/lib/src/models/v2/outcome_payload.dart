/// Discriminated union for `session.result.outcome`
/// (v2 § OutcomePayload). Every result has exactly one [kind].
///
/// Variants match the spec exactly:
/// - [completed] — gameplay finished normally.
/// - [failed] — gameplay failure (HP depleted, manual fail, etc).
/// - [cancelled] — host/user cancellation.
/// - [rejected] — engine rejected `session.start` before play.
/// - [tierRetry] — engine-side retry during a tier stage.
/// - [calibration] — calibration session completed with offsets.
class OutcomePayload {
  const OutcomePayload._({
    required this.kind,
    this.reason,
    this.tierId,
    this.stageIndex,
  });

  /// Variant discriminator. Always non-null.
  final String kind;

  /// Required for [failed] and [cancelled]; null otherwise.
  ///
  /// Failed reasons: `hpDepleted`, `manualFail`, `tierHpDepleted`, `unknown`.
  /// Cancelled reasons: `userBack`, `hostNavigation`, `appBackgrounded`,
  /// `surfaceLost`, `unknown`.
  final String? reason;

  /// Required for [tierRetry]; null otherwise.
  final String? tierId;

  /// Required for [tierRetry]; null otherwise.
  final int? stageIndex;

  static const completedKind = 'completed';
  static const failedKind = 'failed';
  static const cancelledKind = 'cancelled';
  static const rejectedKind = 'rejected';
  static const tierRetryKind = 'tierRetry';
  static const calibrationKind = 'calibration';

  static const validKinds = {
    completedKind,
    failedKind,
    cancelledKind,
    rejectedKind,
    tierRetryKind,
    calibrationKind,
  };

  static const validFailedReasons = {
    'hpDepleted',
    'manualFail',
    'tierHpDepleted',
    'unknown',
  };

  static const validCancelledReasons = {
    'userBack',
    'hostNavigation',
    'appBackgrounded',
    'surfaceLost',
    'unknown',
  };

  const OutcomePayload.completed() : this._(kind: completedKind);

  OutcomePayload.failed(String reason)
      : this._(kind: failedKind, reason: _validate(reason, validFailedReasons, 'failed'));

  OutcomePayload.cancelled(String reason)
      : this._(
          kind: cancelledKind,
          reason: _validate(reason, validCancelledReasons, 'cancelled'),
        );

  const OutcomePayload.rejected() : this._(kind: rejectedKind);

  OutcomePayload.tierRetry({required String tierId, required int stageIndex})
      : this._(kind: tierRetryKind, tierId: tierId, stageIndex: stageIndex);

  const OutcomePayload.calibration() : this._(kind: calibrationKind);

  static String _validate(
    String reason,
    Set<String> allowed,
    String variant,
  ) {
    if (!allowed.contains(reason)) {
      throw FormatException(
        'OutcomePayload.$variant: invalid reason "$reason" '
        '(expected one of $allowed).',
      );
    }
    return reason;
  }

  factory OutcomePayload.fromJson(Map<String, dynamic> json) {
    final kind = json['kind'];
    if (kind is! String || !validKinds.contains(kind)) {
      throw FormatException(
        'OutcomePayload.fromJson: "kind" must be one of $validKinds.',
      );
    }
    switch (kind) {
      case completedKind:
        return const OutcomePayload.completed();
      case failedKind:
        return OutcomePayload.failed(json['reason'] as String);
      case cancelledKind:
        return OutcomePayload.cancelled(json['reason'] as String);
      case rejectedKind:
        return const OutcomePayload.rejected();
      case tierRetryKind:
        final tierId = json['tierId'];
        final stageIndex = json['stageIndex'];
        if (tierId is! String) {
          throw FormatException(
            'OutcomePayload.fromJson: tierRetry requires "tierId".',
          );
        }
        if (stageIndex is! int) {
          throw FormatException(
            'OutcomePayload.fromJson: tierRetry requires "stageIndex".',
          );
        }
        return OutcomePayload.tierRetry(tierId: tierId, stageIndex: stageIndex);
      case calibrationKind:
        return const OutcomePayload.calibration();
    }
    // Unreachable: validKinds check above guards every branch.
    throw FormatException('OutcomePayload.fromJson: unreachable kind "$kind".');
  }

  Map<String, dynamic> toJson() {
    final map = <String, dynamic>{'kind': kind};
    switch (kind) {
      case completedKind:
      case rejectedKind:
      case calibrationKind:
        break;
      case failedKind:
      case cancelledKind:
        map['reason'] = reason;
        break;
      case tierRetryKind:
        map['tierId'] = tierId;
        map['stageIndex'] = stageIndex;
        break;
    }
    return map;
  }
}
