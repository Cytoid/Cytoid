import 'dart:convert';
import 'dart:io';

import 'package:crypto/crypto.dart';
import 'package:flutter/services.dart';
import 'package:path/path.dart' as p;
import 'package:path_provider/path_provider.dart';

/// Copies level assets into a per-version temp directory for Unity VFS
/// access, with content-hash-based caching so unchanged levels skip the
/// rebuild on repeat launches.
///
/// Cache layout: `<temp>/cytoid/levels/<levelId>/<version>/<contentHash>/`.
/// A directory at that path is treated as a hit; the caller trusts the
/// cache key (level id + version + sorted asset keys with their byte
/// lengths) to mean "the bytes on disk are identical to the source bundle".
class LevelVfsMaterializer {
  const LevelVfsMaterializer();

  /// Materializes [assetKeys] for `[levelId, version]` into a cache dir.
  ///
  /// [assetKeys] maps each VFS-relative path (e.g. `charts/hard.json`,
  /// `audio/song.ogg`) to its source byte length, used only as input to
  /// the cache key hash. The bytes are re-read from [bundle] (defaulting
  /// to [rootBundle]) on a cache miss.
  ///
  /// Returns the materialized VFS root directory path, terminated with a
  /// platform path separator so it can be used directly as a path prefix.
  Future<String> materialize({
    required String levelId,
    required String version,
    required Map<String, int> assetKeys,
    AssetBundle? bundle,
    Directory? cacheRootOverride,
  }) async {
    final assetBundle = bundle ?? rootBundle;
    final cacheRoot = cacheRootOverride ?? await getTemporaryDirectory();
    final contentHash = _computeContentHash(assetKeys);
    final cacheDir = Directory(
      p.join(cacheRoot.path, 'cytoid', 'levels', levelId, version, contentHash),
    );

    if (cacheDir.existsSync()) {
      return _withTrailingSeparator(cacheDir.path);
    }

    await cacheDir.create(recursive: true);

    final sortedKeys = assetKeys.keys.toList()..sort();
    for (final assetKey in sortedKeys) {
      final data = await assetBundle.load(assetKey);
      final output = File(p.joinAll([
        cacheDir.path,
        ...assetKey.split('/'),
      ]));
      await output.parent.create(recursive: true);
      await output.writeAsBytes(
        data.buffer.asUint8List(data.offsetInBytes, data.lengthInBytes),
      );
    }

    return _withTrailingSeparator(cacheDir.path);
  }

  /// Removes the cache directory tree for `[levelId, version]` (all
  /// content hashes underneath). No-op if the directory does not exist.
  Future<void> invalidate({
    required String levelId,
    required String version,
    Directory? cacheRootOverride,
  }) async {
    final cacheRoot = cacheRootOverride ?? await getTemporaryDirectory();
    final versionDir = Directory(
      p.join(cacheRoot.path, 'cytoid', 'levels', levelId, version),
    );
    if (versionDir.existsSync()) {
      await versionDir.delete(recursive: true);
    }
  }

  static String _computeContentHash(Map<String, int> assetKeys) {
    final sortedKeys = assetKeys.keys.toList()..sort();
    final payload = sortedKeys.map((k) => '$k:${assetKeys[k]}').join('\n');
    return sha256.convert(utf8.encode(payload)).toString();
  }

  static String _withTrailingSeparator(String path) {
    return path.endsWith(Platform.pathSeparator)
        ? path
        : '$path${Platform.pathSeparator}';
  }
}

/// Validates and canonicalizes a VFS-relative path against the v2 host
/// protocol AssetPayload rules.
///
/// Rules enforced (see `docs/host-protocol-v2.md` § AssetPayload):
/// - [relative] must not be empty.
/// - [relative] must not be absolute (start with `/` or `\`).
/// - [relative] must not contain a `..` segment.
///
/// Returns the resolved URI produced by combining [uri] and [relative].
///
/// Throws [ArgumentError] when any rule is violated.
String canonicalizeVfsPath(String uri, String relative) {
  if (relative.isEmpty) {
    throw ArgumentError.value(relative, 'relative', 'must not be empty');
  }
  final firstChar = relative.codeUnitAt(0);
  if (firstChar == 0x2F || firstChar == 0x5C) {
    throw ArgumentError.value(
      relative,
      'relative',
      'must be relative, not absolute',
    );
  }
  final segments = relative.split(RegExp(r'[/\\]'));
  if (segments.contains('..')) {
    throw ArgumentError.value(
      relative,
      'relative',
      'must not contain parent-traversal (..) segments',
    );
  }
  return Uri.parse(uri).resolve(relative).toString();
}
