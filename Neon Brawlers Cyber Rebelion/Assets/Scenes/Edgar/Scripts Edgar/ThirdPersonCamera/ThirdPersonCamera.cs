using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    #region Referencias

    [Header("Referencias")]
    [Tooltip("Referencia al movimiento del jugador, también se usa como ancla de posición para no heredar su rotación")]
    public PlayerMovement playerMovement;

    [Tooltip("Transform que define la dirección de movimiento del jugador, se sincroniza con el yaw de la cámara")]
    public Transform orientation;

    #endregion

    #region Posición

    [Header("Posición")]
    [Tooltip("Altura del punto de anclaje respecto a la posición del jugador, no se ve afectada por su rotación")]
    public float anchorHeight = 1.6f;

    [Tooltip("Offset local respecto al punto de anclaje, calculado según la rotación de la cámara. La Z define la distancia de reposo del pivote")]
    public Vector3 offset = new Vector3(0f, 0f, -4f);

    [Tooltip("Tiempo de suavizado del seguimiento de posición, simula el lag de una cámara en mano")]
    public float positionSmoothTime = 0.15f;

    private Vector3 positionVelocity;
    private Vector3 smoothedAnchorPosition;
    private bool anchorInitialized = false;

    #endregion

    #region Respiración

    [Header("Respiración")]
    [Tooltip("Amplitud del movimiento vertical que simula la respiración")]
    public float breathingAmplitude = 0.03f;

    [Tooltip("Frecuencia del movimiento vertical que simula la respiración")]
    public float breathingFrequency = 1.2f;

    #endregion

    #region Rotación

    [Header("Rotación")]
    [Tooltip("Sensibilidad del mouse para rotar la cámara")]
    public float mouseSensitivity = 200f;

    [Tooltip("Si está activo, el yaw gira sin límites, ignorando minYaw y maxYaw")]
    public bool infiniteYaw = false;

    [Tooltip("Ángulo mínimo de rotación vertical en grados")]
    public float minPitch = -40f;

    [Tooltip("Ángulo máximo de rotación vertical en grados")]
    public float maxPitch = 70f;

    [Tooltip("Ángulo mínimo de rotación horizontal en grados, ignorado si infiniteYaw está activo")]
    public float minYaw = -180f;

    [Tooltip("Ángulo máximo de rotación horizontal en grados, ignorado si infiniteYaw está activo")]
    public float maxYaw = 180f;

    private float yaw;
    private float pitch;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        Vector3 startAngles = transform.eulerAngles;
        yaw = startAngles.y;
        pitch = startAngles.x;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        HandleRotationInput();
    }

    private void LateUpdate()
    {
        ApplyRotation();
        FollowPosition();
        SyncOrientation();
    }

    #endregion

    #region Rotación de Cámara

    private const float MouseDeltaScale = 0.02f;

    private void HandleRotationInput()
    {
        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        yaw += mouseDelta.x * mouseSensitivity * MouseDeltaScale;
        yaw = infiniteYaw ? Mathf.Repeat(yaw, 360f) : Mathf.Clamp(yaw, minYaw, maxYaw);

        pitch -= mouseDelta.y * mouseSensitivity * MouseDeltaScale;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void ApplyRotation()
    {
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    #endregion

    #region Seguimiento

    private void FollowPosition()
    {
        if (playerMovement == null) return;

        Vector3 anchorPosition = playerMovement.transform.position + Vector3.up * anchorHeight;

        if (!anchorInitialized)
        {
            smoothedAnchorPosition = anchorPosition;
            anchorInitialized = true;
        }

        smoothedAnchorPosition = Vector3.SmoothDamp(
            smoothedAnchorPosition,
            anchorPosition,
            ref positionVelocity,
            positionSmoothTime
        );

        Vector3 rotatedOffset = transform.rotation * offset;
        transform.position = smoothedAnchorPosition + rotatedOffset + Vector3.up * GetBreathingOffset();
    }

    private float GetBreathingOffset()
    {
        return Mathf.Sin(Time.time * breathingFrequency) * breathingAmplitude;
    }

    private void SyncOrientation()
    {
        if (orientation == null) return;

        orientation.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    #endregion
}