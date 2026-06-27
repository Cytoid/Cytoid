## 0.1.0 - 2026-06-27

First public release on the v2 host protocol track. The Dart plugin now ships
an artifact manifest so Flutter callers can detect Unity/plugin drift at
runtime, and the v2 protocol marker (`cytoid.game-core.v2`) is the canonical
schema identifier for both wire envelopes and the manifest.

### Breaking

- Plugin version bumped from `0.0.1` to `0.1.0`. The major-minor now tracks
  the public Dart API surface; patch releases are protocol-compatible.
- The artifact bundle layout now includes a typed
  `manifest.<platform>.json` next to the AAR/xcframework. Downstream
  consumers that globbed the artifacts directory verbatim must ignore files
  whose name starts with `manifest.`.

### Added

- `ArtifactManifest` model
  (`lib/src/models/artifact_manifest.dart`) and `checkArtifactManifest()`
  helper (`lib/src/artifact_manifest_check.dart`). The helper emits a
  structured warning — never throws — when a bundled Unity artifact
  diverges from the running plugin version, when the manifest's
  `protocolSchema` is not `cytoid.game-core.v2`, or when no manifest is
  bundled at all.
- `tool/write_manifest.sh` for emitting
  `.cytoid_game_core/artifacts/manifest.<platform>.json` from CI or local
  packaging scripts. Reads `MANIFEST_PLATFORM`, `MANIFEST_VERSION`, and
  optional `MANIFEST_PLUGIN_VERSION` / `MANIFEST_UNITY_VERSION` /
  `MANIFEST_COMMIT_SHA` / `MANIFEST_BUILD_DATE` overrides.
- Unity artifact manifest written by `tool/setup_unity_artifacts.sh`
  alongside the existing `VERSION` file, so downstream tools can read typed
  metadata without scraping filenames.
- CI workflow `.github/workflows/flutter-plugin-artifacts.yml` now writes
  the manifest in both the Android AAR job and the iOS XCFramework
  packaging job.

### Changed

- `protocolSchema` is now the canonical v2 marker — `cytoid.game-core.v2`
  — and is carried by every manifest. This is a fail-fast identifier, not
  a compatibility negotiation mechanism.
- `setup_unity_artifacts.sh` writes a complete manifest in addition to
  the legacy `VERSION` file; `VERSION` is preserved for backwards
  compatibility with existing consumers.
- `unityDependencies` in the manifest is seeded with `["NativeAudio"]`
  (free since 2025-09-22 per AGENTS.md). Paid vendor packages are
  intentionally omitted from the manifest.

### Removed

- The placeholder CHANGELOG body (`TODO: Describe initial release.`).
