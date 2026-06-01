/// File URI references for level assets passed to the game core.
class GameLaunchAssets {
  const GameLaunchAssets({
    required this.vfsUri,
    required this.chartPath,
    required this.musicPath,
    this.storyboardPath,
  });

  /// Directory containing level files (charts, storyboard images, etc.).
  final String vfsUri;
  final String chartPath;
  final String musicPath;
  final String? storyboardPath;

  factory GameLaunchAssets.fromJson(Map<String, dynamic> json) {
    final vfsUri = _readRequiredString(json, 'vfsUri');
    final chartPath = _readRequiredString(json, 'chartPath');
    final musicPath = _readRequiredString(json, 'musicPath');
    final storyboardPath = _readOptionalString(json, 'storyboardPath');

    return GameLaunchAssets(
      vfsUri: vfsUri,
      chartPath: chartPath,
      musicPath: musicPath,
      storyboardPath: storyboardPath,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'vfsUri': vfsUri,
      'chartPath': chartPath,
      'musicPath': musicPath,
      if (storyboardPath != null) 'storyboardPath': storyboardPath,
    };
  }

  static String _readRequiredString(Map<String, dynamic> json, String field) {
    final value = json[field];
    if (value is String) {
      return value;
    }
    throw FormatException(
      'GameLaunchAssets.fromJson: missing or invalid field $field',
    );
  }

  static String? _readOptionalString(Map<String, dynamic> json, String field) {
    final value = json[field];
    if (value == null || value is String) {
      return value as String?;
    }
    throw FormatException(
      'GameLaunchAssets.fromJson: missing or invalid field $field',
    );
  }
}
