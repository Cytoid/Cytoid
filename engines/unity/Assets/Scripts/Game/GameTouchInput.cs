using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// Gameplay touch hub using the Input System Enhanced Touch API.
/// Replaces Lean Touch for note input and global calibration.
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class GameTouchInput : MonoBehaviour
{
    public const int MouseFingerIndex = -1;

    public static GameTouchInput Instance { get; private set; }

    public static event Action<GameFinger> FingerDown;
    public static event Action<GameFinger> FingerUpdate;
    public static event Action<GameFinger> FingerUp;

    static bool enhancedTouchEnabled;

    void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate GameTouchInput ignored.", this);
            enabled = false;
            return;
        }

        Instance = this;

        if (!enhancedTouchEnabled)
        {
            EnhancedTouchSupport.Enable();
            enhancedTouchEnabled = true;
        }

#if UNITY_EDITOR
        TouchSimulation.Enable();
#endif
    }

    void OnDisable()
    {
        if (Instance != this) return;
        Instance = null;

#if UNITY_EDITOR
        TouchSimulation.Disable();
#endif

        if (enhancedTouchEnabled)
        {
            EnhancedTouchSupport.Disable();
            enhancedTouchEnabled = false;
        }
    }

    void Update()
    {
        foreach (var touch in Touch.activeTouches)
        {
            var finger = GameFinger.FromTouch(touch);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    FingerDown?.Invoke(finger);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    FingerUpdate?.Invoke(finger);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    FingerUp?.Invoke(finger);
                    break;
            }
        }
    }
}

public struct GameFinger
{
    public int Index;
    public Vector2 ScreenPosition;

    public static GameFinger FromTouch(Touch touch) => new GameFinger
    {
        Index = touch.finger.index,
        ScreenPosition = touch.screenPosition
    };

    public Vector3 GetWorldPosition(float distance, Camera camera)
    {
        if (camera == null) camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("GameFinger.GetWorldPosition: no camera.");
            return default;
        }

        return camera.ScreenToWorldPoint(new Vector3(ScreenPosition.x, ScreenPosition.y, distance));
    }
}
