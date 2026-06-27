import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter_test/flutter_test.dart';

ArtifactManifest _sample({
  String pluginVersion = '0.1.0',
  String platform = 'android',
  String protocolSchema = ArtifactManifest.expectedProtocolSchema,
}) {
  return ArtifactManifest(
    pluginVersion: pluginVersion,
    unityVersion: '6000.0.75f1',
    commitSha: '36e6061abcebe926749aa48a9e34a7d19243762a',
    artifactVersion: '0.1.0',
    platform: platform,
    buildDate: '2026-06-27T11:11:52Z',
    unityDependencies: const ['NativeAudio'],
    protocolSchema: protocolSchema,
  );
}

void main() {
  group('ArtifactManifest', () {
    test('parses a sample manifest and round-trips through JSON', () {
      final manifest = _sample();

      final encoded = manifest.toJson();
      final decoded = ArtifactManifest.fromJson(
        Map<String, dynamic>.from(encoded),
      );

      expect(decoded.pluginVersion, '0.1.0');
      expect(decoded.unityVersion, '6000.0.75f1');
      expect(decoded.commitSha, manifest.commitSha);
      expect(decoded.artifactVersion, '0.1.0');
      expect(decoded.platform, 'android');
      expect(decoded.buildDate, '2026-06-27T11:11:52Z');
      expect(decoded.unityDependencies, ['NativeAudio']);
      expect(decoded.protocolSchema, 'cytoid.game-core.v2');
    });

    test('validates schema: rejects unknown platform and protocol schema', () {
      // Unknown platform must surface as a FormatException at parse time.
      expect(
        () => ArtifactManifest.fromJson({
          ..._sample().toJson(),
          'platform': 'web',
        }),
        throwsA(
          isA<FormatException>().having(
            (e) => e.message,
            'message',
            contains('unsupported platform "web"'),
          ),
        ),
      );

      // Unknown protocolSchema must surface at parse time.
      expect(
        () => ArtifactManifest.fromJson({
          ..._sample().toJson(),
          'protocolSchema': 'cytoid.game-core.v1',
        }),
        throwsA(
          isA<FormatException>().having(
            (e) => e.message,
            'message',
            contains('unsupported protocolSchema "cytoid.game-core.v1"'),
          ),
        ),
      );

      // Missing required field.
      expect(
        () => ArtifactManifest.fromJson({
          ..._sample().toJson(),
          'commitSha': null,
        }),
        throwsA(
          isA<FormatException>().having(
            (e) => e.message,
            'message',
            contains('missing or invalid field commitSha'),
          ),
        ),
      );

      // unityDependencies must be a list of strings.
      expect(
        () => ArtifactManifest.fromJson({
          ..._sample().toJson(),
          'unityDependencies': 'NativeAudio',
        }),
        throwsA(
          isA<FormatException>().having(
            (e) => e.message,
            'message',
            contains('must be a list of strings'),
          ),
        ),
      );
    });
  });

  group('checkArtifactManifest', () {
    test('emits warning when pluginVersion diverges from runtime version', () {
      final captured = <String>[];
      final manifest = _sample(pluginVersion: '0.0.1');

      final result = checkArtifactManifest(
        manifest: manifest,
        expectedPluginVersion: '0.1.0',
        warningSink: captured.add,
      );

      expect(result.ok, isFalse);
      expect(result.warnings, isNotEmpty);
      expect(
        captured,
        containsAll(result.warnings),
      );
      expect(
        captured.join('\n'),
        allOf([
          contains('pluginVersion'),
          contains('"0.0.1"'),
          contains('"0.1.0"'),
          contains('setup_unity_artifacts.sh'),
        ]),
      );
      // Critical: a version mismatch is a WARNING, never a crash.
      expect(() => checkArtifactManifest(
        manifest: manifest,
        expectedPluginVersion: '0.1.0',
        warningSink: captured.add,
      ), returnsNormally);
    });

    test('passes silently when versions match and schema is valid', () {
      final captured = <String>[];
      final result = checkArtifactManifest(
        manifest: _sample(),
        expectedPluginVersion: '0.1.0',
        warningSink: captured.add,
      );

      expect(result.ok, isTrue);
      expect(result.warnings, isEmpty);
      expect(captured, isEmpty);
    });

    test('warns (does not throw) when manifest is absent', () {
      final captured = <String>[];
      final result = checkArtifactManifest(
        manifest: null,
        expectedPluginVersion: '0.1.0',
        warningSink: captured.add,
      );

      expect(result.ok, isTrue); // missing manifest is informational only
      expect(captured.single, contains('No artifact manifest bundled'));
    });
  });
}
