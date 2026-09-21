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

    [Tooltip("Offset local respecto al punto de anclaje, calculado según la rotación de la cámara")]
    public Vector3 offset = new Vector3(0f, 0f, -4f);

    [Tooltip("Tiempo de suavizado del seguimiento de posición, simula el lag de una cámara en mano")]
    public float positionSmoothTime = 0.15f;

    [Tooltip("Distancia a partir de la cual se asume que el jugador fue teletransportado, y la cámara se ajusta instantáneamente en vez de suavizar")]
    public float teleportThreshold = 3f;

    private Vector3 positionVelocity;
    private Vector3 smoothedAnchorPosition;
    private bool anchorInitialized = false;

    private Vector3 smoothedCameraPosition;
    private Vector3 cameraPositionVelocity;
    private bool cameraPositionInitialized = false;

    #endregion

    #region Respiración

    [Header("Respiración")]
    [Tooltip("Amplitud del movimiento vertical que simula la respiración en estado normal")]
    public float breathingAmplitude = 0.03f;

    [Tooltip("Frecuencia del movimiento vertical que simula la respiración en estado normal")]
    public float breathingFrequency = 1.2f;

    [Tooltip("Amplitud de la respiración mientras el jugador está siendo detectado por un enemigo")]
    public float tenseBreathingAmplitude = 0.08f;

    [Tooltip("Frecuencia de la respiración mientras el jugador está siendo detectado por un enemigo")]
    public float tenseBreathingFrequency = 3f;

    private bool isTense;

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

    [Tooltip("Offset de rotación fijo aplicado sobre la rotación calculada, para ajustar el ángulo de visión sin afectar el resto del comportamiento")]
    public Vector3 rotationOffset = Vector3.zero;

    private float yaw;
    private float pitch;

    #endregion

    #region Modo Enemigo

    public enum FollowMode
    {
        Normal,
        Custom,
        Returning
    }

    [Header("Modo Enemigo")]
    [Tooltip("Velocidad de transición de posición y rotación al entrar o salir del modo de mirada de enemigo")]
    public float enemyFollowTransitionSpeed = 8f;

    [Tooltip("Ángulo en grados por debajo del cual se considera terminada la transición de regreso, retomando el control instantáneo del mouse")]
    public float returnRotationThreshold = 1f;

    private FollowMode followMode = FollowMode.Normal;
    private Transform customLookAt;
    private Vector3 customPositionOffset;
    private Vector3 customRotationOffset;

    public Vector3 CurrentPivotPosition => followMode == FollowMode.Custom && customLookAt != null
        ? customLookAt.position
        : smoothedAnchorPosition;

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
        if (Time.timeScale <= 0f) return;

        HandleRotationInput();
    }

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f) return;

        switch (followMode)
        {
            case FollowMode.Custom:
                ApplyCustomFollow();
                break;
            case FollowMode.Returning:
                ApplyReturningFollow();
                break;
            default:
                ApplyRotation();
                FollowPosition();
                break;
        }

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
        transform.rotation = GetNormalTargetRotation();
    }

    private Quaternion GetNormalTargetRotation()
    {
        return Quaternion.Euler(pitch, yaw, 0f) * Quaternion.Euler(rotationOffset);
    }

    #endregion

    #region Seguimiento

    private void FollowPosition()
    {
        Vector3 targetPosition = GetNormalTargetPosition();

        transform.position = targetPosition;
        SyncSharedCameraPosition(targetPosition);
    }

    private Vector3 GetNormalTargetPosition()
    {
        if (playerMovement == null) return transform.position;

        Vector3 anchorPosition = playerMovement.transform.position + Vector3.up * anchorHeight;

        if (!anchorInitialized)
        {
            smoothedAnchorPosition = anchorPosition;
            anchorInitialized = true;
        }
        else if (Vector3.Distance(smoothedAnchorPosition, anchorPosition) > teleportThreshold)
        {
            smoothedAnchorPosition = anchorPosition;
            positionVelocity = Vector3.zero;
        }

        smoothedAnchorPosition = Vector3.SmoothDamp(
            smoothedAnchorPosition,
            anchorPosition,
            ref positionVelocity,
            positionSmoothTime
        );

        Vector3 rotatedOffset = GetNormalTargetRotation() * offset;
        return smoothedAnchorPosition + rotatedOffset + Vector3.up * GetBreathingOffset();
    }

    private float GetBreathingOffset()
    {
        float amplitude = isTense ? tenseBreathingAmplitude : breathingAmplitude;
        float frequency = isTense ? tenseBreathingFrequency : breathingFrequency;

        return Mathf.Sin(Time.time * frequency) * amplitude;
    }

    public void SetTenseBreathing(bool active)
    {
        isTense = active;
    }

    private void SyncOrientation()
    {
        if (orientation == null) return;

        orientation.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void SyncSharedCameraPosition(Vector3 currentPosition)
    {
        smoothedCameraPosition = currentPosition;
        cameraPositionVelocity = Vector3.zero;
        cameraPositionInitialized = true;
    }

    private void ApplySmoothedCameraPosition(Vector3 targetPosition, float smoothTime)
    {
        if (!cameraPositionInitialized)
        {
            smoothedCameraPosition = transform.position;
            cameraPositionInitialized = true;
        }

        smoothedCameraPosition = Vector3.SmoothDamp(smoothedCameraPosition, targetPosition, ref cameraPositionVelocity, smoothTime);
        transform.position = smoothedCameraPosition;
    }

    #endregion

    #region Modo Enemigo

    private float GetCustomSmoothTime()
    {
        return 1f / Mathf.Max(enemyFollowTransitionSpeed, 0.01f);
    }

    private void ApplyCustomFollow()
    {
        float t = Time.deltaTime * enemyFollowTransitionSpeed;

        Quaternion targetRotation = customLookAt.rotation * Quaternion.Euler(customRotationOffset);
        Vector3 targetPosition = customLookAt.position + targetRotation * (offset + customPositionOffset);

        ApplySmoothedCameraPosition(targetPosition, GetCustomSmoothTime());
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
    }

    private void ApplyReturningFollow()
    {
        float t = Time.deltaTime * enemyFollowTransitionSpeed;

        Quaternion targetRotation = GetNormalTargetRotation();
        Vector3 targetPosition = GetNormalTargetPosition();

        ApplySmoothedCameraPosition(targetPosition, GetCustomSmoothTime());
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);

        if (Quaternion.Angle(transform.rotation, targetRotation) <= returnRotationThreshold)
        {
            followMode = FollowMode.Normal;
        }
    }

    public void EnterObstacleMode(Transform lookAt, Vector3 positionOffset, Vector3 rotationOffset)
    {
        customLookAt = lookAt;
        customPositionOffset = positionOffset;
        customRotationOffset = rotationOffset;
        followMode = FollowMode.Custom;
    }

    public void ForceReturnToPlayer()
    {
        customLookAt = null;
        followMode = FollowMode.Returning;
    }

    #endregion
}