public interface ScreenHandler
{
    
    void OnScreenCreated();
    
    void OnScreenBecomeActive();
    
    void OnScreenUpdate();

    void OnScreenBecomeInactive();

    void OnScreenDestroyed();
    
}