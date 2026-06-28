import 'dart:convert';

import 'wire_message_type.dart';

/// JSON envelope on the bridge ↔ game wire.
class CytoidGameCoreEnvelope {
  const CytoidGameCoreEnvelope({
    required this.schema,
    required this.id,
    required this.type,
    required this.payload,
  });

  static const currentSchema = 'cytoid.game-core.v2';

  final String schema;
  final String id;
  final String type;
  final Map<String, Object?> payload;

  factory CytoidGameCoreEnvelope.fromJson(Map<String, dynamic> json) {
    final schema = json['schema'];
    final id = json['id'];
    final type = json['type'];
    final payload = json['payload'];

    if (schema is! String) {
      throw FormatException('Envelope "schema" must be a string.');
    }
    if (schema != currentSchema) {
      throw FormatException('Unsupported envelope schema "$schema".');
    }
    if (id is! String) {
      throw FormatException('Envelope "id" must be a string.');
    }
    if (type is! String) {
      throw FormatException('Envelope "type" must be a string.');
    }
    if (payload is! Map) {
      throw FormatException('Envelope "payload" must be an object.');
    }

    return CytoidGameCoreEnvelope(
      schema: schema,
      id: id,
      type: type,
      payload: Map<String, Object?>.from(payload),
    );
  }

  factory CytoidGameCoreEnvelope.fromJsonString(String jsonString) {
    final decoded = jsonDecode(jsonString);
    if (decoded is! Map<String, dynamic>) {
      throw FormatException('Envelope root must be a JSON object.');
    }
    return CytoidGameCoreEnvelope.fromJson(decoded);
  }

  Map<String, dynamic> toJson() => {
    'schema': schema,
    'id': id,
    'type': type,
    'payload': payload,
  };

  String toJsonString() => jsonEncode(toJson());

  bool get isEngineReady => type == WireMessageType.engineReady;
  bool get isHealthOk => type == WireMessageType.healthOk;
  bool get isSessionStarted => type == WireMessageType.sessionStarted;
  bool get isSessionResult => type == WireMessageType.sessionResult;
  bool get isSessionFailed => type == WireMessageType.sessionFailed;
  bool get isSettingsApplied => type == WireMessageType.settingsApplied;
  bool get isLogsBatch => type == WireMessageType.logsBatch;

  static CytoidGameCoreEnvelope create({
    required String id,
    required String type,
    Map<String, Object?> payload = const {},
    String schema = currentSchema,
  }) {
    return CytoidGameCoreEnvelope(
      schema: schema,
      id: id,
      type: type,
      payload: payload,
    );
  }
}
