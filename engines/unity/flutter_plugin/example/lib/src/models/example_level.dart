import 'dart:convert';
import 'dart:io';

import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/services.dart';
import 'package:path/path.dart' as p;
import 'package:path_provider/path_provider.dart';

import 'example_settings.dart';
import 'example_mods.dart';

/// Folder name under [exampleLevelAssetRoot] reserved for global calibration (not listed).
const exampleHiddenLevelFolderName = 'offset_guide';

/// Root path for all example built-in level assets.
const exampleLevelAssetRoot = 'assets/levels';

class ExampleLevel {
  const ExampleLevel({
    required this.id,
    required this.version,
    required this.title,
    required this.artist,
    required this.charter,
    required this.baseAssetPath,
    required this.backgroundAsset,
    required this.musicAsset,
    required this.difficulties,
  });

  final String id;
  final String version;
  final String title;
  final String artist;
  final String charter;
  final String baseAssetPath;
  final String backgroundAsset;
  final String musicAsset;
  final List<ExampleDifficulty> difficulties;

  String get metaAsset => '$baseAssetPath/level.json';
  String get backgroundPath => '$baseAssetPath/$backgroundAsset';

  ExampleDifficulty get defaultDifficulty => difficulties.last;

  Future<SessionLaunchPayload> createLaunchPayload({
    required ExampleDifficulty difficulty,
    required ExampleSettings settings,
    LevelVfsMaterializer vfsMaterializer = const LevelVfsMaterializer(),
    ExampleMods? mods,
    TierPlayLaunch? tierPlay,
  }) async {
    final assetBundle = rootBundle;
    final manifestKeys = listFolderAssetKeys(
      (await AssetManifest.loadFromAssetBundle(assetBundle)).listAssets(),
      baseAssetPath,
    );

    // Read each asset once to capture its byte length for the cache key.
    // The plugin re-reads on a cache miss; this is acceptable for the
    // example app since per-level payloads are small.
    final assetKeyLengths = <String, int>{};
    for (final fullKey in manifestKeys) {
      final data = await assetBundle.load(fullKey);
      assetKeyLengths[fullKey.substring(baseAssetPath.length + 1)] =
          data.lengthInBytes;
    }

    final vfsRootPath = await vfsMaterializer.materialize(
      levelId: id,
      version: version,
      folderAssetPath: baseAssetPath,
      assetKeys: assetKeyLengths,
      bundle: assetBundle,
    );

    final assets = AssetPayload(
      vfsUri: Uri.file(vfsRootPath, windows: false).toString(),
      chartPath: difficulty.chartAsset,
      musicPath: musicAsset,
      storyboardPath: difficulty.storyboardAsset,
    );
    final meta = LevelMetaPayload.fromJson(
      jsonDecode(await assetBundle.loadString(metaAsset)) as Map<String, dynamic>,
    );
    final mode = tierPlay != null
        ? SessionMode.tier
        : mods?.toSessionMode() ?? SessionMode.ranked;

    return SessionLaunchPayload(
      mode: mode,
      level: LevelPayload(
        meta: meta,
        selectedDifficulty: difficulty.type,
        assets: assets,
      ),
      mods: mods?.toGameModList() ?? const [],
      settings: settings.toSettingsPayload(),
      options: const SessionOptions(recordPlayEvents: true),
      tier: tierPlay == null
          ? null
          : TierLaunchPayload(
              tierId: tierPlay.tierId ?? 'example-tier',
              stageIndex: tierPlay.stageIndex,
              stageCount: tierPlay.stageCount ?? 1,
              maxHealth: tierPlay.maxHealth,
              initialHealth: tierPlay.initialHealth ?? tierPlay.maxHealth,
              initialCombo: tierPlay.initialCombo ?? 0,
              introLabel: tierPlay.introLabel,
            ),
    );
  }

}

class ExampleDifficulty {
  const ExampleDifficulty({
    required this.type,
    required this.label,
    required this.level,
    required this.chartAsset,
    this.storyboardAsset,
  });

  final String type;
  final String label;
  final int level;
  final String chartAsset;
  final String? storyboardAsset;
}

class ExampleLevelRepository {
  const ExampleLevelRepository();

  Future<List<ExampleLevel>> loadLevels() async {
    final manifest = await AssetManifest.loadFromAssetBundle(rootBundle);
    final folderRoots = discoverPlayableLevelFolderRoots(manifest.listAssets());

    final levels = <ExampleLevel>[];
    for (final baseAssetPath in folderRoots) {
      final metaJson = await rootBundle.loadString('$baseAssetPath/level.json');
      final meta = jsonDecode(metaJson) as Map<String, dynamic>;
      levels.add(
        _exampleLevelFromMeta(meta: meta, baseAssetPath: baseAssetPath),
      );
    }

    levels.sort((a, b) => a.title.compareTo(b.title));

    // Reclaim temp space from removed/renamed levels before the user starts
    // playing. Best-effort — failures here do not block level loading.
    //
    // Include the hidden calibration guide's ID so its cache survives pruning;
    // otherwise it would be deleted on every startup and rebuilt on next use.
    final activeIds = levels.map((l) => l.id).toSet();
    try {
      final guideMeta = jsonDecode(
        await rootBundle.loadString(
          '$exampleLevelAssetRoot/$exampleHiddenLevelFolderName/level.json',
        ),
      ) as Map<String, dynamic>;
      activeIds.add(guideMeta['id'] as String);
    } on Object {
      // Hidden level may not be bundled in this build.
    }
    try {
      await _pruneOrphanedLevels(activeIds);
    } on Object {
      // Swallow: GC failure is non-fatal.
    }

    return levels;
  }

  Future<ExampleLevel> loadGlobalCalibrationGuide() async {
    final baseAssetPath =
        '$exampleLevelAssetRoot/$exampleHiddenLevelFolderName';
    final metaJson = await rootBundle.loadString('$baseAssetPath/level.json');
    final meta = jsonDecode(metaJson) as Map<String, dynamic>;

    return _exampleLevelFromMeta(
      meta: meta,
      baseAssetPath: baseAssetPath,
      defaultDifficultyLabelOverride: 'Offset test',
    );
  }

  static ExampleLevel _exampleLevelFromMeta({
    required Map<String, dynamic> meta,
    required String baseAssetPath,
    String? defaultDifficultyLabelOverride,
  }) {
    final charts = (meta['charts'] as List).cast<Map<dynamic, dynamic>>().map((
      chart,
    ) {
      final type = chart['type'] as String;
      final storyboard = chart['storyboard'];
      final name = chart['name'];
      return ExampleDifficulty(
        type: type,
        label: name is String
            ? name
            : defaultDifficultyLabelOverride ?? _difficultyLabel(type),
        level: chart['difficulty'] as int,
        chartAsset: chart['path'] as String,
        storyboardAsset: storyboard is Map
            ? storyboard['path'] as String?
            : null,
      );
    }).toList();

    return ExampleLevel(
      id: meta['id'] as String,
      version: _readVersion(meta),
      title: meta['title'] as String,
      artist: meta['artist'] as String,
      charter: meta['charter'] as String,
      baseAssetPath: baseAssetPath,
      backgroundAsset: (meta['background'] as Map)['path'] as String,
      musicAsset: (meta['music'] as Map)['path'] as String,
      difficulties: charts,
    );
  }

  static String _readVersion(Map<String, dynamic> meta) {
    final value = meta['version'];
    if (value is int) return value.toString();
    if (value is String) return value;
    // Fall back to a stable constant so unknown-version levels still get a
    // usable cache key rather than throwing at launch time.
    return '0';
  }

  static String _difficultyLabel(String type) {
    return switch (type) {
      'easy' => 'Easy',
      'hard' => 'Hard',
      'extreme' => 'Extreme',
      _ => type,
    };
  }
}

/// Asset keys directly under [folderAssetPath]
/// (e.g. `assets/levels/foo/bar.txt`).
List<String> listFolderAssetKeys(
  Iterable<String> assetKeys,
  String folderAssetPath,
) {
  final prefix = '$folderAssetPath/';
  return assetKeys.where((key) => key.startsWith(prefix)).toList()..sort();
}

/// Deletes VFS cache directories for levels no longer present in the asset
/// bundle. Walks `<temp>/cytoid/levels/<levelId>/` so all version + hash
/// subdirectories of an orphaned level are reclaimed together.
///
/// Best-effort: individual deletion failures are swallowed so that one
/// stuck directory does not prevent cleanup of the rest.
Future<void> _pruneOrphanedLevels(Set<String> activeLevelIds) async {
  final cacheRoot = await getTemporaryDirectory();
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

/// Asset folder roots (e.g. `assets/levels/my_level`) that contain `level.json`,
/// excluding [exampleHiddenLevelFolderName].
List<String> discoverPlayableLevelFolderRoots(Iterable<String> assetKeys) {
  const metaSuffix = '/level.json';
  final roots = <String>{};

  for (final key in assetKeys) {
    if (!key.startsWith('$exampleLevelAssetRoot/') ||
        !key.endsWith(metaSuffix)) {
      continue;
    }
    final relative = key.substring(exampleLevelAssetRoot.length + 1);
    final folderName = relative.substring(
      0,
      relative.length - metaSuffix.length,
    );
    if (folderName.isEmpty || folderName.contains('/')) {
      continue;
    }
    if (folderName == exampleHiddenLevelFolderName) {
      continue;
    }
    roots.add('$exampleLevelAssetRoot/$folderName');
  }

  return roots.toList()..sort();
}
