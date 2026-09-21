using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public class CameraZoom : MonoBehaviour
{
    #region Configuración

    [Header("Zoom")]
    [Tooltip("Distancia extra mínima que el zoom puede restar respecto al reposo definido por ThirdPersonCamera")]
    public float minDistance = 0f;

    [Tooltip("Distancia extra máxima que el zoom puede sumar respecto al reposo definido por ThirdPersonCamera")]
    public float maxDistance = 3f;

    [Tooltip("Tiempo de suavizado al cambiar el nivel de zoom")]
    public float zoomSmoothTime = 0.1f;

    #endregion

    #region Zoom - Scroll

    [Header("Zoom - Scroll")]
    [Tooltip("Habilita el zoom mediante la rueda del mouse")]
    public bool useScrollWheel = true;

    [Tooltip("Sensibilidad de la rueda del mouse para el zoom, solo aplica si useScrollWheel está activo")]
    public float scrollZoomSpeed = 2f;

    #endregion

    #region Zoom - Aim

    public enum AimActivationMode
    {
        Hold,
        Toggle
    }

    [Header("Zoom - Aim")]
    [Tooltip("Habilita un zoom rápido de acercamiento mientras se usa un botón dedicado")]
    public bool useAimZoom = false;

    [Tooltip("Distancia extra aplicada mientras el zoom de aim está activo")]
    public float aimDistance = -1f;

    [Tooltip("Define si el aim se activa manteniendo presionado el botón o alternando su estado con cada pulsación")]
    public AimActivationMode aimActivationMode = AimActivationMode.Hold;

    [Tooltip("Botón usado para activar el zoom de aim")]
    public InputButtonBinding aimButton = new InputButtonBinding();

    public bool IsZooming { get; private set; }

    private bool aimToggleState;

    #endregion

    #region Estado

    private const float ScrollDeltaScale = 0.01f;

    private float targetDistance;
    private float currentDistance;
    private float zoomVelocity;

    public float CurrentExtraDistance => currentDistance;

    #endregion

    #region Unity Lifecycle

    private void LateUpdate()
    {
        HandleAimInput();
        HandleZoomInput();
        UpdateZoomSmoothing();
        ApplyPosition();
    }

    #endregion

    #region Zoom - Distancia

    private void HandleZoomInput()
    {
        if (useScrollWheel)
        {
            ApplyScrollZoom();
        }

        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
    }

    private void ApplyScrollZoom()
    {
        if (Mouse.current == null) return;

        float scrollDelta = Mouse.current.scroll.ReadValue().y;
        targetDistance -= scrollDelta * scrollZoomSpeed * ScrollDeltaScale;
    }

    private float GetDesiredDistance()
    {
        return useAimZoom && IsZooming ? aimDistance : targetDistance;
    }

    private void UpdateZoomSmoothing()
    {
        currentDistance = Mathf.SmoothDamp(currentDistance, GetDesiredDistance(), ref zoomVelocity, zoomSmoothTime);
    }

    private void ApplyPosition()
    {
        transform.position += transform.rotation * new Vector3(0f, 0f, -currentDistance);
    }

    #endregion

    #region Zoom - Aim

    private void HandleAimInput()
    {
        if (!useAimZoom)
        {
            IsZooming = false;
            return;
        }

        if (aimActivationMode == AimActivationMode.Hold)
        {
            IsZooming = aimButton.IsPressed();
        }
        else
        {
            if (aimButton.WasPressedThisFrame())
            {
                aimToggleState = !aimToggleState;
            }

            IsZooming = aimToggleState;
        }
    }

    #endregion
}