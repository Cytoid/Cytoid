public interface ScreenListener : ScreenInitializedListener, ScreenBecameActiveListener, ScreenUpdateListener, ScreenBecameInactiveListener, ScreenDestroyedListener
{
}

public interface ScreenInitializedListener
{
    void OnScreenInitialized();
}

public interface ScreenBecameActiveListener
{
    void OnScreenBecameActive();
}

public interface ScreenEnterCompletedListener
{
    void OnScreenEnterCompleted();
}

public interface ScreenPostActiveListener
{
    void OnScreenPostActive();
}

public interface ScreenUpdateListener
{
    void OnScreenUpdate();
}

public interface ScreenBecameInactiveListener
{
    void OnScreenBecameInactive();
}

public interface ScreenLeaveCompletedListener
{
    void OnScreenLeaveCompleted();
}

public interface ScreenDestroyedListener
{
    void OnScreenDestroyed();
}

public interface ScreenChangeListener
{
    void OnScreenChangeStarted(Screen from, Screen to);
    
    void OnScreenChangeFinished(Screen from, Screen to);
    
}