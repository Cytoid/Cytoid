import 'dart:io';

import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/services.dart';
import 'package:path/path.dart' as p;
import 'package:path_provider/path_provider.dart';

/// Copies bundled level folder assets into a temp directory for Unity VFS access.
///
/// On every call the destination is rebuilt from scratch so that added, updated,
/// or removed source assets are always reflected — the total payload per level
/// is a few MB at most.
class LevelVfsMaterializer {
  const LevelVfsMaterializer();

  Future<GameLaunchAssets> materializeFolder({
    required String levelId,
    required String folderAssetPath,
    required String chartPath,
    required String musicPath,
    String? storyboardPath,
    AssetBundle? bundle,
  }) async {
    final assetBundle = bundle ?? rootBundle;
    final assetKeys = listFolderAssetKeys(
      (await AssetManifest.loadFromAssetBundle(assetBundle)).listAssets(),
      folderAssetPath,
    );

    final cacheRoot = await getTemporaryDirectory();
    final levelDir = Directory(
      p.join(cacheRoot.path, 'cytoid', 'levels', levelId),
    );

    // Always rebuild: delete stale directory then recreate so that new or
    // updated source assets are picked up on every launch.
    if (levelDir.existsSync()) {
      await levelDir.delete(recursive: true);
    }
    await levelDir.create(recursive: true);

    for (final assetKey in assetKeys) {
      final relative = assetKey.substring(folderAssetPath.length + 1);
      final output = File(p.join(levelDir.path, relative));
      await output.parent.create(recursive: true);
      final data = await assetBundle.load(assetKey);
      await output.writeAsBytes(
        data.buffer.asUint8List(data.offsetInBytes, data.lengthInBytes),
      );
    }

    final vfsPath = '${levelDir.path}${Platform.pathSeparator}';
    return GameLaunchAssets(
      vfsUri: Uri.file(vfsPath, windows: false).toString(),
      chartPath: chartPath,
      musicPath: musicPath,
      storyboardPath: storyboardPath,
    );
  }

  /// Deletes VFS cache directories for levels no longer present in the asset
  /// bundle. Call after level discovery with the set of active level IDs.
  ///
  /// Best-effort: individual deletion failures are swallowed so that one
  /// stuck directory does not prevent cleanup of the rest.
  static Future<void> pruneOrphanedLevels(
    Set<String> activeLevelIds, {
    Directory? cacheRootOverride,
  }) async {
    final cacheRoot = cacheRootOverride ?? await getTemporaryDirectory();
    final levelsRoot = Directory(p.join(cacheRoot.path, 'cytoid', 'levels'));

    if (!levelsRoot.existsSync()) return;

    await for (final entry in levelsRoot.list()) {
      if (entry is! Directory) continue;
      final name = p.basename(entry.path);
      if (activeLevelIds.contains(name)) continue;
      try {
        await entry.delete(recursive: true);
      } on FileSystemException {
        // Best-effort: skip directories we cannot remove.
      }
    }
  }
}

/// Asset keys directly under [folderAssetPath] (e.g. `assets/levels/foo/bar.txt`).
List<String> listFolderAssetKeys(
  Iterable<String> assetKeys,
  String folderAssetPath,
) {
  final prefix = '$folderAssetPath/';
  return assetKeys.where((key) => key.startsWith(prefix)).toList()..sort();
}
