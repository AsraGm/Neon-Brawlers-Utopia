using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[System.Serializable]
public class InputButtonBinding
{
    #region Configuración

    public enum Source
    {
        Keyboard,
        Mouse
    }

    public enum MouseButtonType
    {
        Left,
        Right,
        Middle
    }

    [Tooltip("Origen del botón, teclado o mouse")]
    public Source source = Source.Keyboard;

    [Tooltip("Tecla de teclado usada, solo aplica si el origen es Keyboard")]
    public Key key = Key.LeftShift;

    [Tooltip("Botón del mouse usado, solo aplica si el origen es Mouse")]
    public MouseButtonType mouseButton = MouseButtonType.Right;

    #endregion

    #region Lectura

    public bool IsPressed() // Metodo para verificar el boton presionado
    {
        return source == Source.Keyboard ? IsKeyboardPressed() : IsMousePressed();
    }

    public bool WasPressedThisFrame() // Metodo para verificar si el boton fue presionado en este frame
    {
        return source == Source.Keyboard ? WasKeyboardPressedThisFrame() : WasMousePressedThisFrame();
    }

    private bool IsKeyboardPressed() // Metodo para verificar si la tecla del teclado esta presionada
    {
        return Keyboard.current != null && Keyboard.current[key].isPressed;
    }

    private bool WasKeyboardPressedThisFrame() // Metodo para verificar si la tecla del teclado fue presionada en este frame
    {
        return Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
    }

    private bool IsMousePressed() // Metodo para verificar si el boton del mouse esta presionado
    {
        ButtonControl control = GetMouseControl();
        return control != null && control.isPressed;
    }

    private bool WasMousePressedThisFrame() // Metodo para verificar si el boton del mouse fue presionado en este frame
    {
        ButtonControl control = GetMouseControl();
        return control != null && control.wasPressedThisFrame;
    }

    private ButtonControl GetMouseControl() // Metodo para obtener el control del mouse correspondiente al boton seleccionado
    {
        if (Mouse.current == null) return null;

        return mouseButton switch
        {
            MouseButtonType.Left => Mouse.current.leftButton,
            MouseButtonType.Right => Mouse.current.rightButton,
            MouseButtonType.Middle => Mouse.current.middleButton,
            _ => null
        };
    }

    #endregion
}