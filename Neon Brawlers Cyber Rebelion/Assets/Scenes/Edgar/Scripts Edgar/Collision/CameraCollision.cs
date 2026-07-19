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

    [Tooltip("Radio aproximado de la cámara, usado para separar los rayos de detección // Valores altos = mayor precisión, pero menor rendimiento")]
    public float collisionRadius = 0.3f;

    [Tooltip("Distancia que se mantiene entre la cámara y la superficie con la que colisiona // Valores altos = mayor precisión, pero menor rendimiento")]
    public float collisionPadding = 0.2f;

    [Tooltip("Distancia adicional que se revisa más allá de la posición ideal de la cámara, para anticipar paredes cercanas")]
    public float extraDetectionRange = 0.5f;

    [Tooltip("Tiempo de suavizado al alejar la cámara una vez que deja de colisionar")]
    public float collisionSmoothTime = 0.05f;

    #endregion

    #region Estado

    private static readonly Vector2[] SampleOffsets =
    {
        Vector2.zero,
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right
    };

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

        Vector3 directionNormalized = direction.normalized;
        float castDistance = distance + extraDetectionRange;
        bool anyHit = false;
        float closestDistance = distance;

        foreach (Vector2 sampleOffset in SampleOffsets)
        {
            Vector3 lateralOffset = transform.right * sampleOffset.x * collisionRadius + transform.up * sampleOffset.y * collisionRadius;
            Vector3 sampleAnchor = anchor + lateralOffset;
            Vector3 sampleEnd = sampleAnchor + directionNormalized * castDistance;

            if (!Physics.Linecast(sampleAnchor, sampleEnd, out RaycastHit hit, collisionMask, QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            float hitDistance = Vector3.Distance(sampleAnchor, hit.point);

            if (hitDistance > distance) continue;

            if (!anyHit || hitDistance < closestDistance)
            {
                closestDistance = hitDistance;
                anyHit = true;
            }
        }

        Debug.Log($"CameraCollision: anyHit={anyHit} closestDistance={closestDistance:F2} distance={distance:F2}", this);

        if (!anyHit) return desiredPosition;

        float safeDistance = Mathf.Max(closestDistance - collisionPadding, 0f);
        return anchor + directionNormalized * safeDistance;
    }

    private void ApplySmoothedPosition(Vector3 targetPosition)
    {
        if (!initialized)
        {
            smoothedPosition = transform.position;
            initialized = true;
        }

        Vector3 anchor = thirdPersonCamera.CurrentPivotPosition;
        float targetDistance = Vector3.Distance(anchor, targetPosition);
        float currentDistance = Vector3.Distance(anchor, smoothedPosition);

        if (targetDistance < currentDistance)
        {
            smoothedPosition = targetPosition;
            velocity = Vector3.zero;
        }
        else
        {
            smoothedPosition = Vector3.SmoothDamp(smoothedPosition, targetPosition, ref velocity, collisionSmoothTime);
        }

        transform.position = smoothedPosition;
    }

    #endregion

    #region Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (thirdPersonCamera == null) return;

        Vector3 anchor = thirdPersonCamera.CurrentPivotPosition;
        Vector3 direction = (transform.position - anchor).normalized;
        float distance = Vector3.Distance(anchor, transform.position);

        foreach (Vector2 sampleOffset in SampleOffsets)
        {
            Vector3 lateralOffset = transform.right * sampleOffset.x * collisionRadius + transform.up * sampleOffset.y * collisionRadius;
            Vector3 sampleAnchor = anchor + lateralOffset;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(sampleAnchor, sampleAnchor + direction * distance);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(sampleAnchor + direction * distance, sampleAnchor + direction * (distance + extraDetectionRange));
        }
    }
#endif

    #endregion
}