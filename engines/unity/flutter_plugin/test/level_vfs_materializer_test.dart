import 'dart:convert';
import 'dart:io';

import 'package:crypto/crypto.dart';
import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:path/path.dart' as p;

/// Minimal [AssetBundle] that serves programmable bytes for known keys
/// and counts [load] invocations so cache-hit vs cache-miss is observable.
class _CountingAssetBundle extends AssetBundle {
  _CountingAssetBundle(this._bytes);

  final Map<String, Uint8List> _bytes;
  int loadCount = 0;

  @override
  Future<ByteData> load(String key) async {
    loadCount++;
    final bytes = _bytes[key];
    if (bytes == null) {
      throw StateError('Missing asset key in test bundle: $key');
    }
    return ByteData.sublistView(bytes);
  }

  @override
  Future<String> loadString(String key, {bool cache = true}) async {
    throw UnimplementedError();
  }

  @override
  Future<T> loadStructuredData<T>(
    String key,
    Future<T> Function(String value) parser,
  ) async {
    throw UnimplementedError();
  }
}

String _hashFor(Map<String, int> assetKeys) {
  // Mirrors LevelVfsMaterializer's content hash so tests can pin the
  // cache directory path without re-deriving it through the public API.
  final sortedKeys = assetKeys.keys.toList()..sort();
  final payload = sortedKeys.map((k) => '$k:${assetKeys[k]}').join('\n');
  return sha256.convert(utf8.encode(payload)).toString();
}

const folderAssetPath = 'assets/levels/foo';

void main() {
  late Directory cacheRoot;
  late _CountingAssetBundle bundle;

  setUp(() async {
    cacheRoot = await Directory.systemTemp.createTemp('vfs_materializer_');
    // Bundle stores assets under FULL keys, mirroring how Flutter's
    // rootBundle namespaces level assets (assets/levels/<id>/<relative>).
    // The materializer must combine [folderAssetPath] + relative key to
    // resolve these; loading the bare relative key must fail.
    bundle = _CountingAssetBundle({
      '$folderAssetPath/charts/hard.json': Uint8List.fromList([1, 2, 3, 4]),
      '$folderAssetPath/audio/song.ogg': Uint8List.fromList([10, 20, 30]),
    });
  });

  tearDown(() async {
    if (cacheRoot.existsSync()) {
      await cacheRoot.delete(recursive: true);
    }
  });

  test(
    '(a) first call materializes assets into a fresh cache directory',
    () async {
      const materializer = LevelVfsMaterializer();
      final assetKeys = {'charts/hard.json': 4, 'audio/song.ogg': 3};

      final vfsPath = await materializer.materialize(
        levelId: 'level.foo',
        version: 'v1',
        folderAssetPath: folderAssetPath,
        assetKeys: assetKeys,
        bundle: bundle,
        cacheRootOverride: cacheRoot,
      );

      final chartFile = File(p.join(vfsPath, 'charts', 'hard.json'));
      final musicFile = File(p.join(vfsPath, 'audio', 'song.ogg'));

      expect(chartFile.existsSync(), isTrue);
      expect(musicFile.existsSync(), isTrue);
      expect(chartFile.readAsBytesSync(), [1, 2, 3, 4]);
      expect(musicFile.readAsBytesSync(), [10, 20, 30]);
      expect(bundle.loadCount, assetKeys.length);
    },
  );

  test(
    '(b) second call with same version reuses the cache without reload',
    () async {
      const materializer = LevelVfsMaterializer();
      final assetKeys = {'charts/hard.json': 4, 'audio/song.ogg': 3};

      await materializer.materialize(
        levelId: 'level.foo',
        version: 'v1',
        folderAssetPath: folderAssetPath,
        assetKeys: assetKeys,
        bundle: bundle,
        cacheRootOverride: cacheRoot,
      );
      final firstLoadCount = bundle.loadCount;

      final secondPath = await materializer.materialize(
        levelId: 'level.foo',
        version: 'v1',
        folderAssetPath: folderAssetPath,
        assetKeys: assetKeys,
        bundle: bundle,
        cacheRootOverride: cacheRoot,
      );

      // Cache hit: no additional bundle reads.
      expect(bundle.loadCount, firstLoadCount);
      // Pinned cache directory path.
      expect(
        secondPath,
        '${p.join(cacheRoot.path, 'cytoid', 'levels', 'level.foo', 'v1', _hashFor(assetKeys))}${p.separator}',
      );
    },
  );

  test('(c) second call with a new version re-materializes', () async {
    const materializer = LevelVfsMaterializer();
    final assetKeys = {'charts/hard.json': 4, 'audio/song.ogg': 3};

    final firstPath = await materializer.materialize(
      levelId: 'level.foo',
      version: 'v1',
      folderAssetPath: folderAssetPath,
      assetKeys: assetKeys,
      bundle: bundle,
      cacheRootOverride: cacheRoot,
    );
    final firstLoadCount = bundle.loadCount;

    final secondPath = await materializer.materialize(
      levelId: 'level.foo',
      version: 'v2',
      folderAssetPath: folderAssetPath,
      assetKeys: assetKeys,
      bundle: bundle,
      cacheRootOverride: cacheRoot,
    );

    // Different cache dir, fresh materialization.
    expect(secondPath, isNot(firstPath));
    expect(bundle.loadCount, firstLoadCount + assetKeys.length);

    // Both versions are independently cached on disk.
    expect(Directory(firstPath).existsSync(), isTrue);
    expect(Directory(secondPath).existsSync(), isTrue);
  });

  test('(f) regression: resolves assets via folderAssetPath + relative key '
      '(production rootBundle namespacing)', () async {
    // Reproduces the reported Android bug: the materializer was loading
    // assets by their bare VFS-relative key, which rootBundle cannot
    // resolve. It must combine folderAssetPath + relative key.
    const materializer = LevelVfsMaterializer();
    final assetKeys = {'ex.txt': 7};

    final vfsPath = await materializer.materialize(
      levelId: 'level.bar',
      version: 'v1',
      folderAssetPath: folderAssetPath,
      assetKeys: assetKeys,
      bundle: _CountingAssetBundle({
        '$folderAssetPath/ex.txt': Uint8List.fromList([1, 2, 3, 4, 5, 6, 7]),
      }),
      cacheRootOverride: cacheRoot,
    );

    final chartFile = File(p.join(vfsPath, 'ex.txt'));
    expect(chartFile.existsSync(), isTrue, reason: 'chart file must exist');
    expect(chartFile.readAsBytesSync(), [1, 2, 3, 4, 5, 6, 7]);
  });

  test('(g) regression: a stale empty cache directory is re-materialized, not '
      'treated as a cache hit', () async {
    // Reproduces the cascade symptom: a previous failed materialization
    // left an empty cache directory on disk; the directory's bare
    // existence must NOT be trusted as a hit.
    const materializer = LevelVfsMaterializer();
    final assetKeys = {'ex.txt': 3};
    final fullBundle = _CountingAssetBundle({
      '$folderAssetPath/ex.txt': Uint8List.fromList([10, 20, 30]),
    });

    // Pre-create the cache directory exactly where the materializer
    // expects it, but leave it empty (simulating a prior failed run).
    final cacheDir = Directory(
      p.join(
        cacheRoot.path,
        'cytoid',
        'levels',
        'level.bar',
        'v1',
        _hashFor(assetKeys),
      ),
    );
    await cacheDir.create(recursive: true);
    expect(cacheDir.listSync(), isEmpty);

    final vfsPath = await materializer.materialize(
      levelId: 'level.bar',
      version: 'v1',
      folderAssetPath: folderAssetPath,
      assetKeys: assetKeys,
      bundle: fullBundle,
      cacheRootOverride: cacheRoot,
    );

    expect(vfsPath, '${cacheDir.path}${p.separator}');
    final chartFile = File(p.join(vfsPath, 'ex.txt'));
    expect(chartFile.existsSync(), isTrue);
    expect(chartFile.readAsBytesSync(), [10, 20, 30]);
  });

  test('(d) canonicalizeVfsPath rejects absolute paths', () {
    expect(
      () => canonicalizeVfsPath('file:///tmp/cache/', '/etc/passwd'),
      throwsA(isA<ArgumentError>()),
    );
  });

  test('(e) canonicalizeVfsPath rejects parent-traversal segments', () {
    expect(
      () => canonicalizeVfsPath('file:///tmp/cache/', '../etc/passwd'),
      throwsA(isA<ArgumentError>()),
    );
  });

  test(
    '(h) materialize rejects unsafe asset keys before bundle load',
    () async {
      const materializer = LevelVfsMaterializer();
      for (final assetKey in [
        '../escape.txt',
        '/absolute.txt',
        r'..\escape.txt',
      ]) {
        final rejectingBundle = _CountingAssetBundle({});
        await expectLater(
          materializer.materialize(
            levelId: 'level.bad',
            version: 'v1',
            folderAssetPath: folderAssetPath,
            assetKeys: {assetKey: 1},
            bundle: rejectingBundle,
            cacheRootOverride: cacheRoot,
          ),
          throwsA(isA<ArgumentError>()),
        );
        expect(rejectingBundle.loadCount, 0);
      }
    },
  );
}
