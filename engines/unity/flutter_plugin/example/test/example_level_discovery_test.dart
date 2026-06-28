import 'package:flutter_test/flutter_test.dart';

import 'package:cytoid_flutter/src/models/example_level.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('discoverPlayableLevelFolderRoots', () {
    test('includes folders with level.json except offset_guide', () {
      final keys = [
        'assets/levels/io.cytoid.8bit_adventurer/level.json',
        'assets/levels/io.cytoid.8bit_adventurer/music.mp3',
        'assets/levels/offset_guide/level.json',
        'assets/levels/my_demo/level.json',
      ];

      expect(
        discoverPlayableLevelFolderRoots(keys),
        [
          'assets/levels/io.cytoid.8bit_adventurer',
          'assets/levels/my_demo',
        ],
      );
    });

    test('ignores nested level.json paths', () {
      final keys = ['assets/levels/foo/bar/level.json'];

      expect(discoverPlayableLevelFolderRoots(keys), isEmpty);
    });
  });
}
