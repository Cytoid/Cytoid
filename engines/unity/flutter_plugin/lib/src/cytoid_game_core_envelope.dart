import 'dart:convert';

import 'wire_message_type.dart';

/// JSON envelope on the bridge ↔ game wire.
class CytoidGameCoreEnvelope {
  const CytoidGameCoreEnvelope({
    required this.v,
    required this.id,
    required this.type,
    required this.payload,
  });

  static const currentVersion = 1;

  final int v;
  final String id;
  final String type;
  final Map<String, Object?> payload;

  factory CytoidGameCoreEnvelope.fromJson(Map<String, dynamic> json) {
    final version = json['v'];
    final id = json['id'];
    final type = json['type'];
    final payload = json['payload'];

    if (version is! int) {
      throw FormatException('Envelope "v" must be an int.');
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
      v: version,
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
    'v': v,
    'id': id,
    'type': type,
    'payload': payload,
  };

  String toJsonString() => jsonEncode(toJson());

  bool get isReady => type == WireMessageType.gameReady;
  bool get isStatusResult => type == WireMessageType.gameStatus;
  bool get isPong => type == WireMessageType.gamePong;
  bool get isPlayResult => type == WireMessageType.gamePlayResult;
  bool get isSettingsApplied => type == WireMessageType.gameSettingsUpdated;
  bool get isPlayRouteEnded => type == WireMessageType.gamePlayEnded;
  bool get isLogBatch => type == WireMessageType.gameLogsBatch;

  static CytoidGameCoreEnvelope create({
    required String id,
    required String type,
    Map<String, Object?> payload = const {},
    int v = currentVersion,
  }) {
    return CytoidGameCoreEnvelope(v: v, id: id, type: type, payload: payload);
  }
}
