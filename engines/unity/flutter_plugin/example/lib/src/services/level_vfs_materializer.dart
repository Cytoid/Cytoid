import 'dart:io';

import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/services.dart';
import 'package:path/path.dart' as p;
import 'package:path_provider/path_provider.dart';

/// Copies bundled level folder assets into a temp directory for Unity VFS access.
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

    if (!levelDir.existsSync()) {
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
    }

    final vfsPath = '${levelDir.path}${Platform.pathSeparator}';
    return GameLaunchAssets(
      vfsUri: Uri.file(vfsPath, windows: false).toString(),
      chartPath: chartPath,
      musicPath: musicPath,
      storyboardPath: storyboardPath,
    );
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
