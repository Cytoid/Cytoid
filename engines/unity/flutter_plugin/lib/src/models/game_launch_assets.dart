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
    return GameLaunchAssets(
      vfsUri: json['vfsUri'] as String,
      chartPath: json['chartPath'] as String,
      musicPath: json['musicPath'] as String,
      storyboardPath: json['storyboardPath'] as String?,
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
}
