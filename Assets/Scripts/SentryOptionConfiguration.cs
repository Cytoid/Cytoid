using Sentry.Unity;

public class SentryOptionConfiguration : SentryOptionsConfiguration
{
    public override void Configure(SentryUnityOptions options)
    {
        // Enable debug mode in development
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        options.Debug = true;
#endif
    }
}
