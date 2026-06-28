/// On-the-wire envelope `type` strings (bridge ↔ game).
abstract final class WireMessageType {
  static const gameReady = 'game.ready';
  static const bridgeStatus = 'bridge.status';
  static const gameStatus = 'game.status';
  static const bridgePing = 'bridge.ping';
  static const gamePong = 'game.pong';
  static const gameLogsBatch = 'game.logs.batch';
  static const bridgeSettingsUpdate = 'bridge.settings.update';
  static const gameSettingsUpdated = 'game.settings.updated';
  static const bridgePlayStart = 'bridge.play.start';
  static const gamePlayResult = 'game.play.result';
  static const bridgePlayEnd = 'bridge.play.end';
  static const gamePlayEnded = 'game.play.ended';

  // v2 host-protocol types (docs/host-protocol-v2.md). Stable across v2 model
  // revisions; held here so all callers share one source of truth alongside the
  // v1 names still in flight during the migration.
  static const engineReady = 'engine.ready';
  static const sessionStart = 'session.start';
  static const sessionStarted = 'session.started';
  static const sessionCancel = 'session.cancel';
  static const sessionResult = 'session.result';
  static const sessionFailed = 'session.failed';
}
