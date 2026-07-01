using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public static class GameInput
{
    public static bool IsKeyHeld(KeyCode keyCode)
    {
#if ENABLE_INPUT_SYSTEM
        KeyControl key = GetKeyboardKey(keyCode);
        if (key != null)
            return key.isPressed;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(keyCode);
#else
        return false;
#endif
    }

    public static bool WasKeyPressedThisFrame(KeyCode keyCode)
    {
#if ENABLE_INPUT_SYSTEM
        KeyControl key = GetKeyboardKey(keyCode);
        if (key != null)
            return key.wasPressedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(keyCode);
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static KeyControl GetKeyboardKey(KeyCode keyCode)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return null;

        switch (keyCode)
        {
            case KeyCode.A:
                return keyboard.aKey;
            case KeyCode.D:
                return keyboard.dKey;
            case KeyCode.S:
                return keyboard.sKey;
            case KeyCode.W:
                return keyboard.wKey;
            case KeyCode.LeftArrow:
                return keyboard.leftArrowKey;
            case KeyCode.RightArrow:
                return keyboard.rightArrowKey;
            case KeyCode.UpArrow:
                return keyboard.upArrowKey;
            case KeyCode.DownArrow:
                return keyboard.downArrowKey;
            case KeyCode.Space:
                return keyboard.spaceKey;
            default:
                return null;
        }
    }
#endif
}
