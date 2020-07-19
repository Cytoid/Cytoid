public class InitializationState
{
    public bool IsInitialized;
    public bool IsFirstLaunch;
    public FirstLaunchPhase FirstLaunchPhase;
}

public enum FirstLaunchPhase
{
    GlobalCalibration, BasicTutorial, AdvancedTutorial
}