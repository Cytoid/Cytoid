int readRequiredInt(Map<String, dynamic> json, String key, String context) {
  final value = json[key];
  if (value is int) return value;
  if (value is num && value % 1 == 0) return value.toInt();
  throw FormatException('$context: "$key" must be an integer.');
}

T? readOptionalObject<T>(
  Map<String, dynamic> json,
  String key,
  String context,
  T Function(Map<String, dynamic>) fromJson,
) {
  if (!json.containsKey(key) || json[key] == null) return null;
  final value = json[key];
  if (value is! Map) {
    throw FormatException('$context: "$key" must be an object when present.');
  }
  return fromJson(Map<String, dynamic>.from(value));
}

Map<String, String> readStringMapEntries(
  Object? value,
  String field,
  String context,
) {
  if (value is! Map) {
    throw FormatException('$context: "$field" must be an object.');
  }
  final result = <String, String>{};
  for (final entry in value.entries) {
    final key = entry.key;
    final entryValue = entry.value;
    if (key is! String || entryValue is! String) {
      throw FormatException(
        '$context: "$field" entries must have string keys and values.',
      );
    }
    result[key] = entryValue;
  }
  return result;
}
