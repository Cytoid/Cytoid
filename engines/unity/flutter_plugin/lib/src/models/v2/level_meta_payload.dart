import '_validators.dart';

/// `level.meta` block (v2 § LevelMetaPayload) plus nested
/// [MusicSection], [ChartSection], [StoryboardSection].
///
/// Mirrors the Unity `LevelMeta` model. The host MUST supply a
/// [LevelMetaPayload] that passes the validation rules in the spec.
class LevelMetaPayload {
  const LevelMetaPayload({
    required this.schemaVersion,
    required this.version,
    required this.id,
    required this.title,
    required this.artist,
    required this.music,
    required this.charts,
    this.titleLocalized,
    this.artistLocalized,
    this.artistSource,
    this.illustrator,
    this.illustratorSource,
    this.charter,
    this.storyboarder,
    this.musicPreview,
    this.background,
  });

  final int schemaVersion;
  final int  version;
  final String id;
  final String title;
  final String? titleLocalized;
  final String artist;
  final String? artistLocalized;
  final String? artistSource;
  final String? illustrator;
  final String? illustratorSource;
  final String? charter;
  final String? storyboarder;
  final MusicSection music;
  final MusicSection? musicPreview;
  final MusicSection? background;
  final List<ChartSection> charts;

  factory LevelMetaPayload.fromJson(Map<String, dynamic> json) {
    final id = json['id'];
    if (id is! String || id.isEmpty) {
      throw FormatException(
        'LevelMetaPayload.fromJson: "id" must be a non-empty string.',
      );
    }
    final chartsJson = json['charts'];
    if (chartsJson is! List || chartsJson.isEmpty) {
      throw FormatException(
        'LevelMetaPayload.fromJson: "charts" must be a non-empty array.',
      );
    }
    final charts = chartsJson
        .map((c) => ChartSection.fromJson(Map<String, dynamic>.from(c as Map)))
        .toList(growable: false);
    if (!charts.any((c) => MusicSection.validChartTypes.contains(c.type))) {
      throw FormatException(
        'LevelMetaPayload.fromJson: at least one chart must have type '
        'in ${MusicSection.validChartTypes}.',
      );
    }
    return LevelMetaPayload(
      schemaVersion: readRequiredInt(
        json,
        'schema_version',
        'LevelMetaPayload',
      ),
      version: readRequiredInt(json, 'version', 'LevelMetaPayload'),
      id: id,
      title: json['title'] as String,
      titleLocalized: json['title_localized'] as String?,
      artist: json['artist'] as String,
      artistLocalized: json['artist_localized'] as String?,
      artistSource: json['artist_source'] as String?,
      illustrator: json['illustrator'] as String?,
      illustratorSource: json['illustrator_source'] as String?,
      charter: json['charter'] as String?,
      storyboarder: json['storyboarder'] as String?,
      music: MusicSection.fromJson(_asMap(json['music'], 'music')),
      musicPreview: _optionalMusic(json['music_preview'], 'music_preview'),
      background: _optionalMusic(json['background'], 'background'),
      charts: charts,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'schema_version': schemaVersion,
      'version': version,
      'id': id,
      'title': title,
      if (titleLocalized != null) 'title_localized': titleLocalized,
      'artist': artist,
      if (artistLocalized != null) 'artist_localized': artistLocalized,
      if (artistSource != null) 'artist_source': artistSource,
      if (illustrator != null) 'illustrator': illustrator,
      if (illustratorSource != null) 'illustrator_source': illustratorSource,
      if (charter != null) 'charter': charter,
      if (storyboarder != null) 'storyboarder': storyboarder,
      'music': music.toJson(),
      if (musicPreview != null) 'music_preview': musicPreview!.toJson(),
      if (background != null) 'background': background!.toJson(),
      'charts': charts.map((c) => c.toJson()).toList(growable: false),
    };
  }

  static Map<String, dynamic> _asMap(Object? v, String field) {
    if (v is! Map) {
      throw FormatException(
        'LevelMetaPayload.fromJson: "$field" must be an object.',
      );
    }
    return Map<String, dynamic>.from(v);
  }

  static MusicSection? _optionalMusic(Object? v, String field) {
    if (v == null) return null;
    return MusicSection.fromJson(_asMap(v, field));
  }
}

/// Music clip reference (v2 § MusicSection).
class MusicSection {
  const MusicSection({required this.path});

  /// VFS-relative file path.
  final String path;

  static const validChartTypes = {'easy', 'hard', 'extreme'};

  factory MusicSection.fromJson(Map<String, dynamic> json) {
    final path = json['path'];
    if (path is! String || path.isEmpty) {
      throw FormatException(
        'MusicSection.fromJson: "path" must be a non-empty string.',
      );
    }
    return MusicSection(path: path);
  }

  Map<String, dynamic> toJson() => {'path': path};
}

/// Chart section (v2 § ChartSection).
class ChartSection {
  const ChartSection({
    required this.type,
    required this.difficulty,
    required this.path,
    this.name,
    this.musicOverride,
    this.storyboard,
  });

  /// Difficulty id: `easy`, `hard`, or `extreme`.
  final String type;

  /// Optional display name override for this chart.
  final String? name;

  /// Difficulty level integer.
  final int difficulty;

  /// VFS-relative chart path.
  final String path;

  /// Per-chart music override. Same shape as [MusicSection].
  final MusicSection? musicOverride;

  /// Per-chart storyboard.
  final StoryboardSection? storyboard;

  factory ChartSection.fromJson(Map<String, dynamic> json) {
    final type = json['type'];
    if (type is! String || !MusicSection.validChartTypes.contains(type)) {
      throw FormatException(
        'ChartSection.fromJson: "type" must be one of '
        '${MusicSection.validChartTypes}.',
      );
    }
    final path = json['path'];
    if (path is! String || path.isEmpty) {
      throw FormatException(
        'ChartSection.fromJson: "path" must be a non-empty string.',
      );
    }
    return ChartSection(
      type: type,
      name: json['name'] as String?,
      difficulty: readRequiredInt(json, 'difficulty', 'ChartSection'),
      path: path,
      musicOverride: readOptionalObject(
        json,
        'music_override',
        'ChartSection',
        MusicSection.fromJson,
      ),
      storyboard: readOptionalObject(
        json,
        'storyboard',
        'ChartSection',
        StoryboardSection.fromJson,
      ),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'type': type,
      if (name != null) 'name': name,
      'difficulty': difficulty,
      'path': path,
      if (musicOverride != null) 'music_override': musicOverride!.toJson(),
      if (storyboard != null) 'storyboard': storyboard!.toJson(),
    };
  }
}

/// Storyboard section (v2 § StoryboardSection).
class StoryboardSection {
  const StoryboardSection({required this.path, required this.localizations});

  /// VFS-relative storyboard path. Default `storyboard.json` if omitted by
  /// the source level (host fills this in before send).
  final String path;

  /// Map of BCP-47 locale → VFS-relative localization path. Empty object if
  /// no localizations.
  final Map<String, String> localizations;

  factory StoryboardSection.fromJson(Map<String, dynamic> json) {
    final path = json['path'];
    if (path is! String || path.isEmpty) {
      throw FormatException(
        'StoryboardSection.fromJson: "path" must be a non-empty string.',
      );
    }
    final locRaw = json['localizations'];
    final Map<String, dynamic> locMap = locRaw is Map
        ? Map<String, dynamic>.from(locRaw)
        : {}; // spec: "Empty object if no localizations" — absent/null treated as empty.
    return StoryboardSection(
      path: path,
      localizations: readStringMapEntries(
        locMap,
        'localizations',
        'StoryboardSection',
      ),
    );
  }

  Map<String, dynamic> toJson() {
    return {'path': path, 'localizations': localizations};
  }
}
