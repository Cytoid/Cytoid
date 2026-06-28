import 'dart:async';

import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/foundation.dart';

class UnityLogStore extends ChangeNotifier {
  UnityLogStore({this.maxEntries = 500});

  final int maxEntries;
  final List<CytoidGameCoreLogEntry> _entries = [];
  final Set<String> _entryKeys = {};
  StreamSubscription<CytoidGameCoreLogBatch>? _batchSubscription;

  List<CytoidGameCoreLogEntry> get entries => List.unmodifiable(_entries);

  void bind(CytoidGameCoreClient client) {
    unbind();
    _batchSubscription = client.logBatchEvents.listen(_appendBatch);
  }

  void unbind() {
    unbindOnly();
  }

  void unbindOnly() {
    _batchSubscription?.cancel();
    _batchSubscription = null;
  }

  void clear() {
    if (_entries.isEmpty) {
      return;
    }
    _entries.clear();
    _entryKeys.clear();
    notifyListeners();
  }

  void _appendBatch(CytoidGameCoreLogBatch batch) {
    _appendEntries(batch.logs);
    notifyListeners();
  }

  void _appendEntries(Iterable<CytoidGameCoreLogEntry> entries) {
    for (final entry in entries) {
      final key = _entryKey(entry);
      if (_entryKeys.add(key)) {
        _entries.add(entry);
      }
    }

    _entries.sort((a, b) => _entryTime(a).compareTo(_entryTime(b)));
    _trim();
  }

  void _trim() {
    while (_entries.length > maxEntries) {
      final removed = _entries.removeAt(0);
      _entryKeys.remove(_entryKey(removed));
    }
  }

  DateTime _entryTime(CytoidGameCoreLogEntry entry) {
    return DateTime.fromMillisecondsSinceEpoch(entry.timestamp);
  }

  String _entryKey(CytoidGameCoreLogEntry entry) {
    return [
      entry.timestamp.toString(),
      entry.level.name,
      entry.sessionId ?? '',
      entry.message,
      entry.stackTrace ?? '',
    ].join('\u{1f}');
  }

  @override
  void dispose() {
    unbindOnly();
    super.dispose();
  }
}
