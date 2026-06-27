import 'log_entry_payload.dart';

/// `logs.batch` payload (v2 § LogsBatchPayload).
class LogsBatchPayload {
  const LogsBatchPayload({
    required this.reason,
    required this.timestamp,
    required this.truncated,
    required this.logs,
    this.triggerLevel,
  });

  /// Why this batch was emitted: `periodic`, `trigger`, `flush`, or `crash`.
  final String reason;

  /// Required when `reason = "trigger"`. The log level that caused the trigger.
  final String? triggerLevel;

  /// Unix epoch milliseconds. Time the batch was produced.
  final int timestamp;

  /// Whether the batch hit a size or count limit and dropped earlier entries.
  final bool truncated;

  /// Log entries.
  final List<LogEntryPayload> logs;

  static const validReasons = {'periodic', 'trigger', 'flush', 'crash'};

  factory LogsBatchPayload.fromJson(Map<String, dynamic> json) {
    final reason = json['reason'];
    if (reason is! String || !validReasons.contains(reason)) {
      throw FormatException(
        'LogsBatchPayload.fromJson: "reason" must be one of $validReasons.',
      );
    }
    final timestamp = json['timestamp'];
    if (timestamp is! int) {
      throw FormatException(
        'LogsBatchPayload.fromJson: "timestamp" must be an integer.',
      );
    }
    final truncated = json['truncated'];
    if (truncated is! bool) {
      throw FormatException(
        'LogsBatchPayload.fromJson: "truncated" must be a boolean.',
      );
    }
    final triggerLevel = json['triggerLevel'];
    if (reason == 'trigger' && (triggerLevel is! String || triggerLevel.isEmpty)) {
      throw FormatException(
        'LogsBatchPayload.fromJson: "triggerLevel" is required when '
        'reason = "trigger".',
      );
    }
    final logsJson = json['logs'];
    if (logsJson is! List) {
      throw FormatException(
        'LogsBatchPayload.fromJson: "logs" must be an array.',
      );
    }
    return LogsBatchPayload(
      reason: reason,
      timestamp: timestamp,
      truncated: truncated,
      triggerLevel: triggerLevel as String?,
      logs: logsJson
          .map((e) => LogEntryPayload.fromJson(Map<String, dynamic>.from(e as Map)))
          .toList(growable: false),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'reason': reason,
      if (triggerLevel != null) 'triggerLevel': triggerLevel,
      'timestamp': timestamp,
      'truncated': truncated,
      'logs': logs.map((e) => e.toJson()).toList(growable: false),
    };
  }
}
