import 'package:flutter/foundation.dart';

import 'models/artifact_manifest.dart';

/// Result of [checkArtifactManifest]. Carries enough detail for callers (or
/// tests) to inspect what was warned about without re-parsing.
class ArtifactManifestCheckResult {
  const ArtifactManifestCheckResult({
    required this.ok,
    required this.warnings,
  });

  /// `true` when no divergence was detected. `false` when one or more
  /// warnings were emitted. Never throw from [checkArtifactManifest]; a
  /// manifest mismatch is a warning, not a crash.
  final bool ok;

  /// Structured warning lines emitted via [warningSink]. Empty when [ok].
  final List<String> warnings;
}

/// Compare a bundled [manifest] (when present) against the running
/// [expectedPluginVersion].
///
/// Emits a structured warning via [warningSink] (defaults to [debugPrint]) if:
/// - the manifest's `pluginVersion` differs from [expectedPluginVersion];
/// - the manifest's `protocolSchema` differs from
///   [ArtifactManifest.expectedProtocolSchema];
/// - the manifest's `platform` is unrecognized;
/// - [manifest] is `null` (no bundle present) — diagnostic only, still `ok`.
///
/// Never throws. A missing or malformed manifest is treated as a soft
/// warning so a stale artifact bundle cannot crash the app at startup.
///
/// Tests should pass a list-capturing [warningSink] instead of scraping
/// debugPrint output.
ArtifactManifestCheckResult checkArtifactManifest({
  ArtifactManifest? manifest,
  required String expectedPluginVersion,
  void Function(String message)? warningSink,
}) {
  final sink = warningSink ?? _defaultSink;
  final warnings = <String>[];

  if (manifest == null) {
    final message =
        '[CytoidGameCore] No artifact manifest bundled; '
        'running without Unity artifact version checks.';
    sink(message);
    return ArtifactManifestCheckResult(ok: true, warnings: const []);
  }

  if (manifest.pluginVersion != expectedPluginVersion) {
    final message =
        '[CytoidGameCore] Artifact manifest pluginVersion '
        '"${manifest.pluginVersion}" diverges from runtime plugin version '
        '"$expectedPluginVersion". Re-run setup_unity_artifacts.sh against '
        'the current plugin.';
    sink(message);
    warnings.add(message);
  }

  if (manifest.protocolSchema != ArtifactManifest.expectedProtocolSchema) {
    final message =
        '[CytoidGameCore] Artifact manifest protocolSchema '
        '"${manifest.protocolSchema}" is not '
        '"${ArtifactManifest.expectedProtocolSchema}". '
        'Treat the artifact as incompatible.';
    sink(message);
    warnings.add(message);
  }

  if (!ArtifactManifest.supportedPlatforms.contains(manifest.platform)) {
    final message =
        '[CytoidGameCore] Artifact manifest platform '
        '"${manifest.platform}" is not recognized.';
    sink(message);
    warnings.add(message);
  }

  return ArtifactManifestCheckResult(
    ok: warnings.isEmpty,
    warnings: List<String>.unmodifiable(warnings),
  );
}

void _defaultSink(String message) {
  // debugPrint is throttled and only fires in debug/profile builds; identical
  // to how the rest of the plugin surfaces diagnostics.
  debugPrint(message);
}
