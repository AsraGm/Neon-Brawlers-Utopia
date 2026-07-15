using UnityEngine;

[DefaultExecutionOrder(200)]
public class CameraCollision : MonoBehaviour
{
    #region Referencias

    [Header("Referencias")]
    [Tooltip("Componente ThirdPersonCamera del cual se obtiene el punto de anclaje actual")]
    public ThirdPersonCamera thirdPersonCamera;

    #endregion

    #region Configuración

    [Header("Colisión")]
    [Tooltip("Capas consideradas como obstáculos para la colisión de cámara")]
    public LayerMask collisionMask;

    [Tooltip("Radio de la esfera usada para detectar colisiones")]
    public float collisionRadius = 0.3f;

    [Tooltip("Distancia que se mantiene entre la cámara y la superficie con la que colisiona")]
    public float collisionPadding = 0.2f;

    [Tooltip("Tiempo de suavizado al acercar o alejar la cámara por colisión")]
    public float collisionSmoothTime = 0.05f;

    #endregion

    #region Estado

    private Vector3 smoothedPosition;
    private Vector3 velocity;
    private bool initialized;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        if (collisionMask.value == 0)
        {
            Debug.LogWarning("CameraCollision: collisionMask está en 'Nothing', no va a detectar ninguna pared. Asigna la capa correcta en el inspector.", this);
        }
    }

    private void LateUpdate()
    {
        if (thirdPersonCamera == null) return;

        Vector3 targetPosition = GetCollisionAdjustedPosition();
        ApplySmoothedPosition(targetPosition);
    }

    #endregion

    #region Colisión

    private Vector3 GetCollisionAdjustedPosition()
    {
        Vector3 anchor = thirdPersonCamera.CurrentPivotPosition;
        Vector3 desiredPosition = transform.position;

        Vector3 direction = desiredPosition - anchor;
        float distance = direction.magnitude;

        if (distance <= 0.0001f) return desiredPosition;

        bool hasHit = Physics.SphereCast(
            anchor,
            collisionRadius,
            direction.normalized,
            out RaycastHit hit,
            distance,
            collisionMask
        );

        Debug.Log($"CameraCollision: hasHit={hasHit} distance={distance:F2} hitObject={(hasHit ? hit.collider.name : "ninguno")}", this);

        if (!hasHit) return desiredPosition;

        float safeDistance = Mathf.Max(hit.distance - collisionPadding, 0f);
        return anchor + direction.normalized * safeDistance;
    }

    private void ApplySmoothedPosition(Vector3 targetPosition)
    {
        if (!initialized)
        {
            smoothedPosition = transform.position;
            initialized = true;
        }

        smoothedPosition = Vector3.SmoothDamp(smoothedPosition, targetPosition, ref velocity, collisionSmoothTime);
        transform.position = smoothedPosition;
    }

    #endregion

    #region Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (thirdPersonCamera == null) return;

        Vector3 anchor = thirdPersonCamera.CurrentPivotPosition;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(anchor, transform.position);
        Gizmos.DrawWireSphere(transform.position, collisionRadius);
    }
#endif

    #endregion
}