/// File URI references for level assets passed to the game core.
class GameLaunchAssets {
  const GameLaunchAssets({
    this.vfsUri,
    this.chartUri,
    this.musicUri,
    this.storyboardUri,
  });

  /// Directory containing level files (charts, storyboard images, etc.).
  final String? vfsUri;
  final String? chartUri;
  final String? musicUri;
  final String? storyboardUri;

  factory GameLaunchAssets.fromJson(Map<String, dynamic> json) {
    return GameLaunchAssets(
      vfsUri: json['vfsUri'] as String?,
      chartUri: json['chartUri'] as String?,
      musicUri: json['musicUri'] as String?,
      storyboardUri: json['storyboardUri'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      if (vfsUri != null) 'vfsUri': vfsUri,
      if (chartUri != null) 'chartUri': chartUri,
      if (musicUri != null) 'musicUri': musicUri,
      if (storyboardUri != null) 'storyboardUri': storyboardUri,
    };
  }
}
