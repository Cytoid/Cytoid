using UnityEngine;
using UnityEngine.InputSystem;

public static class GameInputCompat
{
    public static bool WasEscapePressedThisFrame()
    {
        var keyboard = Keyboard.current;
        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
    }

    public static bool TryGetPointerScreenPosition(out Vector2 screenPosition)
    {
        var pointer = Pointer.current;
        if (pointer == null)
        {
            screenPosition = default;
            return false;
        }

        screenPosition = pointer.position.ReadValue();
        if (screenPosition.x > 1000000000f)
        {
            screenPosition = default;
            return false;
        }

        return true;
    }

    public static void SetGyroscopeEnabled(bool enabled)
    {
        var gyro = UnityEngine.InputSystem.Gyroscope.current;
        if (gyro == null) return;

        if (enabled)
            InputSystem.EnableDevice(gyro);
        else
            InputSystem.DisableDevice(gyro);
    }
}
