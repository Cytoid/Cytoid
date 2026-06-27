import 'dart:convert';
import 'dart:io';

import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter_test/flutter_test.dart';

/// Round-trip tests for every v2 Dart model. Each fixture under
/// `test/fixtures/v2/` is parsed, re-serialized, re-parsed, and structurally
/// compared. The companion `.expected.json` declares whether the parse should
/// throw or succeed.
///
/// `variantBag` fixtures (outcome, error) hold multiple cases keyed by a
/// top-level name; each variant is exercised independently.

final _fixturesDir =
    Directory('${Directory.current.path}/test/fixtures/v2');

void main() {
  group('v2 models — round-trip fixtures', () {
    test('fixture directory exists with at least 10 .json files', () {
      expect(_fixturesDir.existsSync(), isTrue);
      final jsonFiles = _fixturesDir
          .listSync()
          .whereType<File>()
          .where((f) => f.path.endsWith('.json'))
          .where((f) => !f.path.endsWith('.expected.json'))
          .toList();
      expect(jsonFiles.length, greaterThanOrEqualTo(10),
          reason: 'Spec requires ≥10 fixture files');
    });

    test('GameCoreError round-trips', () {
      final variants = _loadVariantBag('error_payload.valid');
      _expectRoundTrip(
        variants['minimal']! as Map<String, dynamic>,
        GameCoreError.fromJson,
        (e) => e.toJson(),
      );
      _expectRoundTrip(
        variants['withDetails']! as Map<String, dynamic>,
        GameCoreError.fromJson,
        (e) => e.toJson(),
      );
    });

    test('FlagsPayload round-trips', () {
      _roundTripFile(
        'flags_payload.valid.json',
        FlagsPayload.fromJson,
        (x) => x.toJson(),
      );
    });

    test('ResultTelemetryPayload round-trips', () {
      _roundTripFile(
        'result_telemetry_payload.valid.json',
        ResultTelemetryPayload.fromJson,
        (x) => x.toJson(),
      );
    });

    test('ScorePayload round-trips', () {
      _roundTripFile(
        'score_payload.valid.json',
        ScorePayload.fromJson,
        (x) => x.toJson(),
      );
    });

    test('LevelResultPayload round-trips', () {
      _roundTripFile(
        'level_result_payload.valid.json',
        LevelResultPayload.fromJson,
        (x) => x.toJson(),
      );
    });

    test('CalibrationResultPayload round-trips', () {
      _roundTripFile(
        'calibration_result_payload.valid.json',
        CalibrationResultPayload.fromJson,
        (x) => x.toJson(),
      );
    });

    test('TierResultPayload round-trips', () {
      _roundTripFile(
        'tier_result_payload.valid.json',
        TierResultPayload.fromJson,
        (x) => x.toJson(),
      );
    });

    test('LevelMetaPayload round-trips', () {
      _roundTripFile(
        'level_meta_payload.valid.json',
        LevelMetaPayload.fromJson,
        (x) => x.toJson(),
      );
    });

    test('LevelMetaPayload rejects empty id and unknown chart type', () {
      _expectThrows('level_meta_payload.invalid.json');
    });

    test('OutcomePayload — all 9 variants round-trip', () {
      final variants = _loadVariantBag('outcome_payload.valid');
      for (final entry in variants.entries) {
        _expectRoundTrip(
          entry.value as Map<String, dynamic>,
          OutcomePayload.fromJson,
          (x) => x.toJson(),
          label: entry.key,
        );
      }
    });

    test('OutcomePayload rejects malformed variants', () {
      final variants = _loadVariantBag('outcome_payload.invalid');
      for (final entry in variants.entries) {
        final json = entry.value as Map<String, dynamic>;
        expect(
          () => OutcomePayload.fromJson(json),
          throwsA(isA<FormatException>()),
          reason: 'variant "${entry.key}" should reject',
        );
      }
    });

    test('SessionTelemetryPayload round-trips', () {
      _roundTripFile(
        'session_telemetry_payload.valid.json',
        SessionTelemetryPayload.fromJson,
        (x) => x.toJson(),
      );
    });

    test('LogsBatchPayload round-trips', () {
      _roundTripFile(
        'logs_batch_payload.valid.json',
        LogsBatchPayload.fromJson,
        (x) => x.toJson(),
      );
    });

    test('LogsBatchPayload rejects trigger without triggerLevel', () {
      _expectThrows('logs_batch_payload.invalid.json');
    });

    test('SessionResultPayload — completed ranked round-trips', () {
      _roundTripFile(
        'session_result_payload.valid.json',
        SessionResultPayload.fromJson,
        (x) => x.toJson(),
      );
    });

    test('SessionResultPayload — tierRetry round-trips', () {
      _roundTripFile(
        'session_result_payload.tier_retry.valid.json',
        SessionResultPayload.fromJson,
        (x) => x.toJson(),
      );
    });

    test('SessionResultPayload — runtimeFailed round-trips with error', () {
      _roundTripFile(
        'session_result_payload.runtime_failed.valid.json',
        SessionResultPayload.fromJson,
        (x) => x.toJson(),
      );
    });

    test('SessionResultPayload rejects rejected outcome without error', () {
      _expectThrows('session_result_payload.invalid.json');
    });

    test('SessionLaunchPayload — full ranked snapshot round-trips', () {
      _roundTripFile(
        'session_launch_payload.valid.json',
        SessionLaunchPayload.fromJson,
        (x) => x.toJson(),
      );
    });

    test('SessionLaunchPayload rejects tier mode without tier block', () {
      _expectThrows('session_launch_payload.invalid.json');
    });
  });

  group('v2 models — directional serialization', () {
    test('SessionLaunchPayload serializes mods using v2 wire names', () {
      final payload = SessionLaunchPayload.fromJson(
        _decodeJson('session_launch_payload.valid.json') as Map<String, dynamic>,
      );
      expect(payload.mods, [GameMod.fast]);
      expect(payload.toJson()['mods'], ['fast']);
    });

    test('SessionLaunchPayload echoes mode as v2 wire name', () {
      final payload = SessionLaunchPayload.fromJson(
        _decodeJson('session_launch_payload.valid.json') as Map<String, dynamic>,
      );
      expect(payload.mode, SessionMode.ranked);
      expect(payload.toJson()['mode'], 'ranked');
    });

    test('OutcomePayload completed is the const default', () {
      const outcome = OutcomePayload.completed();
      expect(outcome.kind, 'completed');
      expect(outcome.toJson(), {'kind': 'completed'});
    });

    test('OutcomePayload tierRetry carries tierId and stageIndex', () {
      final outcome = OutcomePayload.tierRetry(
        tierId: 'tier.example',
        stageIndex: 2,
      );
      expect(outcome.kind, 'tierRetry');
      expect(outcome.tierId, 'tier.example');
      expect(outcome.stageIndex, 2);
      expect(outcome.toJson(), {
        'kind': 'tierRetry',
        'tierId': 'tier.example',
        'stageIndex': 2,
      });
    });
  });
}

// --- helpers ---

Object _decodeJson(String filename) {
  final file = File('${_fixturesDir.path}/$filename');
  return json.decode(file.readAsStringSync());
}

Map<String, Object?> _loadExpected(String fixtureName) {
  final file = File('${_fixturesDir.path}/$fixtureName.expected.json');
  return Map<String, Object?>.from(json.decode(file.readAsStringSync()) as Map);
}

Map<String, Object> _loadVariantBag(String name) {
  final file = File('${_fixturesDir.path}/$name.json');
  final raw = Map<String, Object>.from(json.decode(file.readAsStringSync()) as Map);
  // Variant-bag fixtures carry a `_comment` top-level key with a String value;
  // strip it so iteration yields only real cases.
  raw.remove('_comment');
  return raw;
}

void _roundTripFile<T>(
  String filename,
  T Function(Map<String, dynamic>) fromJson,
  Map<String, dynamic> Function(T) toJson,
) {
  final raw = _decodeJson(filename);
  expect(raw, isA<Map>(),
      reason: 'fixture $filename must hold a single object');
  _expectRoundTrip(Map<String, dynamic>.from(raw as Map), fromJson, toJson);
}

void _expectRoundTrip<T>(
  Map<String, dynamic> input,
  T Function(Map<String, dynamic>) fromJson,
  Map<String, dynamic> Function(T) toJson, {
  String? label,
}) {
  // Red: parse.
  final parsed = fromJson(input);
  // Green: serialize and re-parse; assert deep equality on the JSON view so
  // we test the parser/serializer pair, not field-by-field equality.
  final encoded = toJson(parsed);
  final reparsed = fromJson(Map<String, dynamic>.from(encoded));
  final reencoded = toJson(reparsed);
  final reason = label == null ? '' : ' (variant "$label")';
  expect(
    _normalize(encoded),
    equals(_normalize(reencoded)),
    reason: 'round-trip JSON must be stable$reason',
  );
  expect(_normalize(encoded), isNot(contains('_comment')),
      reason: 'serialized output must never contain fixture comments');
}

void _expectThrows(String filename) {
  final raw = _decodeJson(filename);
  expect(raw, isA<Map>());
  // Strip the leading _comment so it isn't mistaken for a parse target by
  // any model. The model parsers reject unknown fields by way of typed casts.
  final json = Map<String, dynamic>.from(raw as Map)..remove('_comment');
  final expected = _loadExpected(filename.replaceAll('.json', ''));
  expect(expected['throws'], isTrue,
      reason: '$filename.expected.json must declare throws=true');

  // We do not know which model the fixture is for, but every fixture filename
  // encodes its target type, so we route via the filename prefix.
  expect(
    () => _dispatchParse(filename, json),
    throwsA(isA<FormatException>()),
    reason: '$filename must reject',
  );
}

void _dispatchParse(String filename, Map<String, dynamic> json) {
  final stem = filename.split('.').first;
  switch (stem) {
    case 'level_meta_payload':
      LevelMetaPayload.fromJson(json);
      break;
    case 'logs_batch_payload':
      LogsBatchPayload.fromJson(json);
      break;
    case 'session_result_payload':
      SessionResultPayload.fromJson(json);
      break;
    case 'session_launch_payload':
      SessionLaunchPayload.fromJson(json);
      break;
    default:
      throw StateError(
        '_expectThrows: no parser dispatch for fixture "$filename"',
      );
  }
}

/// Recursively canonicalize a JSON-like value so sets/maps compare stably.
Object _normalize(Object? value) {
  if (value is Map) {
    return value.map((k, v) => MapEntry(k.toString(), _normalize(v)));
  }
  if (value is List) {
    return value.map(_normalize).toList();
  }
  if (value is double && value.truncateToDouble() == value) {
    // Normalize integer-valued doubles so 1.0 and 1 compare equal. The wire
    // form treats them as the same JSON number.
    return value.toInt();
  }
  return value ?? <String>[];
}
