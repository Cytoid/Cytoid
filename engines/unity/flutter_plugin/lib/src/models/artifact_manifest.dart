/// Typed view of a Unity build artifact manifest.
///
/// The manifest is written by `tool/write_manifest.sh` (or
/// `tool/setup_unity_artifacts.sh`) into
/// `.cytoid_game_core/artifacts/manifest.<platform>.json` and is read back at
/// runtime by [checkArtifactManifest] to detect plugin/artifact drift.
///
/// Schema is fixed at [protocolSchema] = `cytoid.game-core.v2`. The manifest is
/// the engine-side companion to the v2 wire envelope schema marker.
class ArtifactManifest {
  const ArtifactManifest({
    required this.pluginVersion,
    required this.unityVersion,
    required this.commitSha,
    required this.artifactVersion,
    required this.platform,
    required this.buildDate,
    required this.unityDependencies,
    required this.protocolSchema,
  });

  /// Schema marker carried by every v2 manifest. Receivers must reject any
  /// manifest that carries a different value.
  static const String expectedProtocolSchema = 'cytoid.game-core.v2';

  /// Supported values for [platform].
  static const Set<String> supportedPlatforms = {'android', 'ios'};

  /// Plugin version (from `pubspec.yaml`). Independent of [unityVersion] and
  /// [artifactVersion] so the plugin can rev without a Unity re-export.
  final String pluginVersion;

  /// Unity Editor version that produced the artifact
  /// (e.g. `6000.0.75f1`). Informational; not a compatibility gate.
  final String unityVersion;

  /// Git commit SHA at build time. Full 40-char hex string.
  final String commitSha;

  /// Artifact version (the version label shipped on the AAR/xcframework
  /// bundle). May diverge from [pluginVersion] during a transition.
  final String artifactVersion;

  /// Either `'android'` or `'ios'`.
  final String platform;

  /// ISO 8601 UTC timestamp (`YYYY-MM-DDTHH:MM:SSZ`).
  final String buildDate;

  /// Native dependencies shipped inside the artifact bundle
  /// (initially `['NativeAudio']`).
  final List<String> unityDependencies;

  /// Always [expectedProtocolSchema].
  final String protocolSchema;

  factory ArtifactManifest.fromJson(Map<String, dynamic> json) {
    final pluginVersion = _readRequiredString(json, 'pluginVersion');
    final unityVersion = _readRequiredString(json, 'unityVersion');
    final commitSha = _readRequiredString(json, 'commitSha');
    final artifactVersion = _readRequiredString(json, 'artifactVersion');
    final platform = _readRequiredString(json, 'platform');
    final buildDate = _readRequiredString(json, 'buildDate');
    final protocolSchema = _readRequiredString(json, 'protocolSchema');

    if (!supportedPlatforms.contains(platform)) {
      throw FormatException(
        'ArtifactManifest.fromJson: unsupported platform "$platform" '
        '(expected one of $supportedPlatforms).',
      );
    }
    if (protocolSchema != expectedProtocolSchema) {
      throw FormatException(
        'ArtifactManifest.fromJson: unsupported protocolSchema '
        '"$protocolSchema" (expected "$expectedProtocolSchema").',
      );
    }

    return ArtifactManifest(
      pluginVersion: pluginVersion,
      unityVersion: unityVersion,
      commitSha: commitSha,
      artifactVersion: artifactVersion,
      platform: platform,
      buildDate: buildDate,
      unityDependencies: _readStringList(json, 'unityDependencies'),
      protocolSchema: protocolSchema,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'pluginVersion': pluginVersion,
      'unityVersion': unityVersion,
      'commitSha': commitSha,
      'artifactVersion': artifactVersion,
      'platform': platform,
      'buildDate': buildDate,
      'unityDependencies': List<String>.from(unityDependencies),
      'protocolSchema': protocolSchema,
    };
  }

  static String _readRequiredString(Map<String, dynamic> json, String field) {
    final value = json[field];
    if (value is String && value.isNotEmpty) {
      return value;
    }
    throw FormatException(
      'ArtifactManifest.fromJson: missing or invalid field $field',
    );
  }

  static List<String> _readStringList(Map<String, dynamic> json, String field) {
    final value = json[field];
    if (value == null) {
      throw FormatException(
        'ArtifactManifest.fromJson: missing field $field',
      );
    }
    if (value is! List) {
      throw FormatException(
        'ArtifactManifest.fromJson: field $field must be a list of strings.',
      );
    }
    return value.map((item) => item as String).toList(growable: false);
  }
}
