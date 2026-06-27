/// `session.start.level.assets` block (v2 § AssetPayload).
class AssetPayload {
  const AssetPayload({
    required this.vfsUri,
    required this.chartPath,
    required this.musicPath,
    this.storyboardPath,
    this.checksum,
  });

  /// File URI ending with a directory separator.
  final String vfsUri;

  /// VFS-relative chart path.
  final String chartPath;

  /// VFS-relative music path.
  final String musicPath;

  /// VFS-relative storyboard path.
  final String? storyboardPath;

  /// Optional content identity for diagnostics/cache validation.
  final Map<String, dynamic>? checksum;

  factory AssetPayload.fromJson(Map<String, dynamic> json) {
    return AssetPayload(
      vfsUri: json['vfsUri'] as String,
      chartPath: json['chartPath'] as String,
      musicPath: json['musicPath'] as String,
      storyboardPath: json['storyboardPath'] as String?,
      checksum: json['checksum'] is Map
          ? Map<String, dynamic>.from(json['checksum'] as Map)
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'vfsUri': vfsUri,
      'chartPath': chartPath,
      'musicPath': musicPath,
      if (storyboardPath != null) 'storyboardPath': storyboardPath,
      if (checksum != null) 'checksum': checksum,
    };
  }
}
