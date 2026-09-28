using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class InputButtonBinding
{
    public enum ButtonType
    {
        Keyboard,
        MouseLeft,
        MouseRight,
        MouseMiddle
    }

    [Tooltip("Tipo de botón usado para esta acción")]
    public ButtonType buttonType = ButtonType.MouseRight;

    [Tooltip("Tecla usada si el Button Type es Keyboard")]
    public Key keyboardKey = Key.LeftShift;

    public bool IsPressed()
    {
        switch (buttonType)
        {
            case ButtonType.MouseLeft:
                return Mouse.current != null && Mouse.current.leftButton.isPressed;
            case ButtonType.MouseRight:
                return Mouse.current != null && Mouse.current.rightButton.isPressed;
            case ButtonType.MouseMiddle:
                return Mouse.current != null && Mouse.current.middleButton.isPressed;
            case ButtonType.Keyboard:
                return Keyboard.current != null && Keyboard.current[keyboardKey].isPressed;
            default:
                return false;
        }
    }

    public bool WasPressedThisFrame()
    {
        switch (buttonType)
        {
            case ButtonType.MouseLeft:
                return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            case ButtonType.MouseRight:
                return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            case ButtonType.MouseMiddle:
                return Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame;
            case ButtonType.Keyboard:
                return Keyboard.current != null && Keyboard.current[keyboardKey].wasPressedThisFrame;
            default:
                return false;
        }
    }
}