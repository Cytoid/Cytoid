import 'asset_payload.dart';
import 'level_meta_payload.dart';

/// `session.start.level` block (v2 § LevelPayload).
class LevelPayload {
  const LevelPayload({
    required this.meta,
    required this.selectedDifficulty,
    required this.assets,
  });

  /// Structured level metadata.
  final LevelMetaPayload meta;

  /// Difficulty/chart id in `meta.charts[].type`.
  final String selectedDifficulty;

  /// VFS root and selected relative paths.
  final AssetPayload assets;

  factory LevelPayload.fromJson(Map<String, dynamic> json) {
    final metaJson = json['meta'];
    if (metaJson is! Map) {
      throw FormatException(
        'LevelPayload.fromJson: "meta" must be an object.',
      );
    }
    final meta = LevelMetaPayload.fromJson(Map<String, dynamic>.from(metaJson));
    final selected = json['selectedDifficulty'] as String;
    if (!meta.charts.any((c) => c.type == selected)) {
      throw FormatException(
        'LevelPayload.fromJson: "selectedDifficulty"="$selected" does not '
        'match any chart type in meta.charts.',
      );
    }
    final assetsJson = json['assets'];
    if (assetsJson is! Map) {
      throw FormatException(
        'LevelPayload.fromJson: "assets" must be an object.',
      );
    }
    return LevelPayload(
      meta: meta,
      selectedDifficulty: selected,
      assets: AssetPayload.fromJson(Map<String, dynamic>.from(assetsJson)),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'meta': meta.toJson(),
      'selectedDifficulty': selectedDifficulty,
      'assets': assets.toJson(),
    };
  }
}
