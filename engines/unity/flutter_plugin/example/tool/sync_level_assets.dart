// ignore_for_file: avoid_print
//
// Regenerates pubspec.yaml entries for each assets/levels/<folder>/.
// Flutter bundles one directory per line (subfolders are not implied by the parent).
//
// Run manually from engines/unity/flutter_plugin/example after adding or removing a level folder:
//   dart run tool/sync_level_assets.dart
//   flutter pub get

import 'dart:io';

const _pubspecPath = 'pubspec.yaml';
const _levelsRoot = 'assets/levels';
const _beginMarker =
    '    # BEGIN example_level_assets (generated — dart run tool/sync_level_assets.dart)';
const _endMarker = '    # END example_level_assets';

void main() {
  final pubspecFile = File(_pubspecPath);
  if (!pubspecFile.existsSync()) {
    stderr.writeln('Run from engines/unity/flutter_plugin/example (pubspec.yaml not found).');
    exitCode = 1;
    return;
  }

  final levelsDir = Directory(_levelsRoot);
  if (!levelsDir.existsSync()) {
    stderr.writeln('Missing $_levelsRoot');
    exitCode = 1;
    return;
  }

  final entries = <String>[];

  for (final entity in levelsDir.listSync()) {
    if (entity is! Directory) {
      continue;
    }
    final folderName = entity.path.split(Platform.pathSeparator).last;
    if (folderName.startsWith('.')) {
      continue;
    }
    if (!File('${entity.path}/level.json').existsSync()) {
      continue;
    }
    final folder = entity.path.replaceAll(r'\', '/');
    entries.add('    - $folder/');
  }

  entries.sort();

  final block = StringBuffer()
    ..writeln(_beginMarker)
    ..writeAll(entries.map((line) => '$line\n'))
    ..writeln(_endMarker);

  final original = pubspecFile.readAsStringSync();
  final begin = original.indexOf(_beginMarker);
  final end = original.indexOf(_endMarker);

  if (begin == -1 || end == -1 || end < begin) {
    stderr.writeln(
      'pubspec.yaml is missing $_beginMarker / $_endMarker markers.',
    );
    exitCode = 1;
    return;
  }

  final endLine = end + _endMarker.length;
  final updated = StringBuffer()
    ..write(original.substring(0, begin))
    ..write(block)
    ..write(original.substring(endLine));

  pubspecFile.writeAsStringSync(updated.toString());
  print('Registered ${entries.length} level folder(s) in $_pubspecPath');
  for (final entry in entries) {
    print('  ${entry.trim()}');
  }
}
