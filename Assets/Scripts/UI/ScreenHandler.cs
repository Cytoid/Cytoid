public interface ScreenEventListener
{
    
    void OnScreenInitialized();
    
    void OnScreenBecomeActive();
    
    void OnScreenUpdate();

    void OnScreenBecomeInactive();

    void OnScreenDestroyed();
    
}