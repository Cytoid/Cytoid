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
}
