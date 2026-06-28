/// On-the-wire envelope `type` strings (bridge ↔ game).
abstract final class WireMessageType {
  static const engineReady = 'engine.ready';
  static const engineError = 'engine.error';
  static const healthCheck = 'health.check';
  static const healthOk = 'health.ok';
  static const settingsApply = 'settings.apply';
  static const settingsApplied = 'settings.applied';
  static const sessionStart = 'session.start';
  static const sessionStarted = 'session.started';
  static const sessionCancel = 'session.cancel';
  static const sessionTelemetry = 'session.telemetry';
  static const sessionResult = 'session.result';
  static const sessionFailed = 'session.failed';
  static const logsBatch = 'logs.batch';
}
