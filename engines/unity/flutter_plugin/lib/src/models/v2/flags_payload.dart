/// `flags` field on `session.result` (v2 § FlagsPayload).
class FlagsPayload {
  const FlagsPayload({required this.usedAutoMod});

  /// True if any auto-class mod was active during the session.
  final bool usedAutoMod;

  factory FlagsPayload.fromJson(Map<String, dynamic> json) {
    final usedAutoMod = json['usedAutoMod'];
    if (usedAutoMod is! bool) {
      throw FormatException(
        'FlagsPayload.fromJson: "usedAutoMod" must be a boolean.',
      );
    }
    return FlagsPayload(usedAutoMod: usedAutoMod);
  }

  Map<String, dynamic> toJson() {
    return {'usedAutoMod': usedAutoMod};
  }

  FlagsPayload copyWith({bool? usedAutoMod}) {
    return FlagsPayload(usedAutoMod: usedAutoMod ?? this.usedAutoMod);
  }
}
