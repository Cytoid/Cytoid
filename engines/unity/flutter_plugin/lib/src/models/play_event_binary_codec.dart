import 'dart:convert';
import 'dart:typed_data';

/// Compact binary codec for Unity-emitted gameplay input events.
class PlayEventBinaryCodec {
  const PlayEventBinaryCodec._();

  static Uint8List encode(List<Map<String, Object?>> events) {
    if (events.isEmpty) {
      return Uint8List(0);
    }

    final bytes = <int>[0x43, 0x59, 0x54, 0x45, 1]; // CYTE, version 1
    _writeVarUint(bytes, events.length);

    var previousTimeMs = 0;
    for (final event in events) {
      final timeMs = _readInt(event['t']);
      _writeVarInt(bytes, timeMs - previousTimeMs);
      previousTimeMs = timeMs;

      _writeVarInt(bytes, _readInt(event['f']));
      bytes.add(_phaseCode(event['p']));
      _writeVarUint(bytes, _readInt(event['x']));
      _writeVarUint(bytes, _readInt(event['y']));
    }

    return Uint8List.fromList(bytes);
  }

  static int jsonByteLength(List<Map<String, Object?>> events) {
    return utf8.encode(jsonEncode(events)).length;
  }

  static int _readInt(Object? value) {
    if (value is int) {
      return value;
    }
    if (value is num) {
      return value.toInt();
    }
    throw const FormatException('Play event field must be numeric.');
  }

  static int _phaseCode(Object? value) {
    return switch (value) {
      'down' => 1,
      'move' => 2,
      'up' => 3,
      _ => throw FormatException('Unknown play event phase: $value'),
    };
  }

  static void _writeVarInt(List<int> target, int value) {
    _writeVarUint(target, (value << 1) ^ (value >> 31));
  }

  static void _writeVarUint(List<int> target, int value) {
    var remaining = value;
    while (remaining >= 0x80) {
      target.add((remaining & 0x7f) | 0x80);
      remaining >>= 7;
    }
    target.add(remaining);
  }
}
