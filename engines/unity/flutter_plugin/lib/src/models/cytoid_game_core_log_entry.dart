import '../cytoid_game_core_envelope.dart';
import '../wire_message_type.dart';

enum CytoidGameCoreLogLevel {
  log,
  warning,
  error,
  exception;

  static CytoidGameCoreLogLevel fromString(String value) {
    switch (value) {
      case 'warning':
        return CytoidGameCoreLogLevel.warning;
      case 'error':
        return CytoidGameCoreLogLevel.error;
      case 'exception':
        return CytoidGameCoreLogLevel.exception;
      default:
        return CytoidGameCoreLogLevel.log;
    }
  }
}

/// A single game-core log line carried inside [WireMessageType.gameLogsBatch].
class CytoidGameCoreLogEntry {
  const CytoidGameCoreLogEntry({
    required this.level,
    required this.message,
    this.stackTrace,
    this.timestamp,
    this.playId,
    this.envelopeId,
  });

  final CytoidGameCoreLogLevel level;
  final String message;
  final String? stackTrace;
  final String? timestamp;
  final String? playId;
  final String? envelopeId;
}

/// A batch of recent game-core logs forwarded after a diagnostic trigger.
class CytoidGameCoreLogBatch {
  const CytoidGameCoreLogBatch({
    required this.reason,
    required this.logs,
    this.triggerLevel,
    this.timestamp,
    this.truncated = false,
    this.envelopeId,
  });

  final String reason;
  final CytoidGameCoreLogLevel? triggerLevel;
  final String? timestamp;
  final bool truncated;
  final List<CytoidGameCoreLogEntry> logs;
  final String? envelopeId;

  factory CytoidGameCoreLogBatch.fromEnvelope(CytoidGameCoreEnvelope envelope) {
    if (envelope.type != WireMessageType.gameLogsBatch) {
      throw FormatException(
        'Expected game.logs.batch envelope, got ${envelope.type}.',
      );
    }

    final payload = envelope.payload;
    final reason = payload['reason'];
    final logsRaw = payload['logs'];
    if (reason is! String) {
      throw FormatException(
        'game.logs.batch payload "reason" must be a string.',
      );
    }
    if (logsRaw is! List) {
      throw FormatException('game.logs.batch payload "logs" must be a list.');
    }

    final triggerLevelRaw = payload['triggerLevel'];
    return CytoidGameCoreLogBatch(
      reason: reason,
      triggerLevel: triggerLevelRaw is String
          ? CytoidGameCoreLogLevel.fromString(triggerLevelRaw)
          : null,
      timestamp: payload['timestamp'] as String?,
      truncated: payload['truncated'] == true,
      logs: logsRaw
          .map((entry) {
            if (entry is! Map) {
              throw FormatException(
                'game.logs.batch payload "logs" entries must be objects.',
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
  if (levelRaw is! String) {
    throw FormatException('game log payload "level" must be a string.');
  }
  if (message is! String) {
    throw FormatException('game log payload "message" must be a string.');
  }

  return CytoidGameCoreLogEntry(
    level: CytoidGameCoreLogLevel.fromString(levelRaw),
    message: message,
    stackTrace: payload['stackTrace'] as String?,
    timestamp: payload['timestamp'] as String?,
    playId: payload['playId'] as String?,
    envelopeId: envelopeId,
  );
}
