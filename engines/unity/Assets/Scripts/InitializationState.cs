public class InitializationState
{
    public bool IsInitialized;
    public FirstLaunchPhase FirstLaunchPhase = FirstLaunchPhase.None;

    public bool IsDuringFirstLaunch() =>
        FirstLaunchPhase != FirstLaunchPhase.None && FirstLaunchPhase != FirstLaunchPhase.Completed;

    public bool IsAfterFirstLaunch() => FirstLaunchPhase == FirstLaunchPhase.Completed;
}

public enum FirstLaunchPhase
{
    None, GlobalCalibration, BasicTutorial, Completed
}