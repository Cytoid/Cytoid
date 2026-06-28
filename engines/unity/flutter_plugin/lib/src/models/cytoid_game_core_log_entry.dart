import '../cytoid_game_core_envelope.dart';
import '../wire_message_type.dart';

enum CytoidGameCoreLogLevel {
  debug,
  info,
  warning,
  error,
  fatal;

  static CytoidGameCoreLogLevel fromString(String value) {
    switch (value) {
      case 'debug':
        return CytoidGameCoreLogLevel.debug;
      case 'info':
        return CytoidGameCoreLogLevel.info;
      case 'warning':
        return CytoidGameCoreLogLevel.warning;
      case 'error':
        return CytoidGameCoreLogLevel.error;
      case 'fatal':
        return CytoidGameCoreLogLevel.fatal;
      default:
        throw FormatException('Unknown game-core log level "$value".');
    }
  }
}

/// A single game-core log line carried inside [WireMessageType.logsBatch].
class CytoidGameCoreLogEntry {
  const CytoidGameCoreLogEntry({
    required this.level,
    required this.message,
    required this.timestamp,
    this.stackTrace,
    this.sessionId,
    this.envelopeId,
  });

  final CytoidGameCoreLogLevel level;
  final String message;
  final String? stackTrace;
  final int timestamp;
  final String? sessionId;
  final String? envelopeId;
}

/// A batch of recent game-core logs forwarded after a diagnostic trigger.
class CytoidGameCoreLogBatch {
  const CytoidGameCoreLogBatch({
    required this.reason,
    required this.timestamp,
    required this.logs,
    this.triggerLevel,
    this.truncated = false,
    this.envelopeId,
  });

  final String reason;
  final CytoidGameCoreLogLevel? triggerLevel;
  final int timestamp;
  final bool truncated;
  final List<CytoidGameCoreLogEntry> logs;
  final String? envelopeId;

  static const validReasons = {'periodic', 'trigger', 'flush', 'crash'};

  factory CytoidGameCoreLogBatch.fromEnvelope(CytoidGameCoreEnvelope envelope) {
    if (envelope.type != WireMessageType.logsBatch) {
      throw FormatException(
        'Expected logs.batch envelope, got ${envelope.type}.',
      );
    }

    final payload = envelope.payload;
    final reason = payload['reason'];
    final timestamp = payload['timestamp'];
    final logsRaw = payload['logs'];
    if (reason is! String || !validReasons.contains(reason)) {
      throw FormatException(
        'logs.batch payload "reason" must be one of $validReasons.',
      );
    }
    if (timestamp is! int) {
      throw FormatException(
        'logs.batch payload "timestamp" must be an integer.',
      );
    }
    if (logsRaw is! List) {
      throw FormatException('logs.batch payload "logs" must be a list.');
    }

    final triggerLevelRaw = payload['triggerLevel'];
    if (reason == 'trigger' && triggerLevelRaw is! String) {
      throw FormatException(
        'logs.batch payload "triggerLevel" is required for trigger batches.',
      );
    }
    return CytoidGameCoreLogBatch(
      reason: reason,
      timestamp: timestamp,
      triggerLevel: triggerLevelRaw is String
          ? CytoidGameCoreLogLevel.fromString(triggerLevelRaw)
          : null,
      truncated: payload['truncated'] == true,
      logs: logsRaw
          .map((entry) {
            if (entry is! Map) {
              throw FormatException(
                'logs.batch payload "logs" entries must be objects.',
              );
            }
            return _logEntryFromPayload(
              Map<String, Object?>.from(entry),
              envelopeId: envelope.id,
            );
          })
          .toList(growable: false),
      envelopeId: envelope.id,
    );
  }
}

CytoidGameCoreLogEntry _logEntryFromPayload(
  Map<String, Object?> payload, {
  String? envelopeId,
}) {
  final levelRaw = payload['level'];
  final message = payload['message'];
  final timestamp = payload['timestamp'];
  if (levelRaw is! String) {
    throw FormatException('game log payload "level" must be a string.');
  }
  if (message is! String) {
    throw FormatException('game log payload "message" must be a string.');
  }
  if (timestamp is! int) {
    throw FormatException('game log payload "timestamp" must be an integer.');
  }

  return CytoidGameCoreLogEntry(
    level: CytoidGameCoreLogLevel.fromString(levelRaw),
    message: message,
    stackTrace: payload['stackTrace'] as String?,
    timestamp: timestamp,
    sessionId: payload['sessionId'] as String?,
    envelopeId: envelopeId,
  );
}
