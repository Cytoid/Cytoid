public static class WireMessageTypes
{
    public const string EngineReady = "engine.ready";
    public const string EngineError = "engine.error";
    public const string HealthCheck = "health.check";
    public const string HealthOk = "health.ok";
    public const string SettingsApply = "settings.apply";
    public const string SettingsApplied = "settings.applied";
    public const string SessionStart = "session.start";
    public const string SessionStarted = "session.started";
    public const string SessionCancel = "session.cancel";
    public const string SessionTelemetry = "session.telemetry";
    public const string SessionResult = "session.result";
    public const string SessionFailed = "session.failed";
    public const string LogsBatch = "logs.batch";
}
