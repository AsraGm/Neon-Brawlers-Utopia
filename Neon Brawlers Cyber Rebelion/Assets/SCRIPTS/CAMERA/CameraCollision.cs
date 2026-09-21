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

    [Tooltip("Radio aproximado de la cámara, usado para separar los rayos de detección")]
    public float collisionRadius = 0.3f;

    [Tooltip("Distancia que se mantiene entre la cámara y la superficie con la que colisiona")]
    public float collisionPadding = 0.2f;

    [Tooltip("Distancia adicional que se revisa más allá de la posición ideal de la cámara, para anticipar paredes cercanas")]
    public float extraDetectionRange = 0.5f;

    [Tooltip("Frames consecutivos que debe persistir la colisión antes de aplicar la corrección, filtra falsos positivos de un solo frame en uniones de mesh")]
    public int requiredConsecutiveHits = 2;

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
    private bool wasColliding;
    private int consecutiveHitFrames;

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

        Vector3 desiredPosition = transform.position;
        bool isColliding = TryGetCorrectedPosition(desiredPosition, out Vector3 correctedPosition);

        ApplyPosition(desiredPosition, correctedPosition, isColliding);
    }

    #endregion

    #region Colisión

    private bool TryGetCorrectedPosition(Vector3 desiredPosition, out Vector3 correctedPosition)
    {
        correctedPosition = desiredPosition;

        Vector3 anchor = thirdPersonCamera.CurrentPivotPosition;
        Vector3 direction = desiredPosition - anchor;
        float distance = direction.magnitude;

        if (distance <= 0.0001f) return false;

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

            if (IsPartOfPlayer(hit.collider))
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

        consecutiveHitFrames = anyHit ? consecutiveHitFrames + 1 : 0;

        if (consecutiveHitFrames < requiredConsecutiveHits) return false;

        float safeDistance = Mathf.Max(closestDistance - collisionPadding, 0f);
        correctedPosition = anchor + directionNormalized * safeDistance;
        return true;
    }

    private bool IsPartOfPlayer(Collider hitCollider)
    {
        if (thirdPersonCamera.playerMovement == null) return false;

        return hitCollider.transform.root == thirdPersonCamera.playerMovement.transform.root;
    }

    private void ApplyPosition(Vector3 desiredPosition, Vector3 correctedPosition, bool isColliding)
    {
        if (isColliding)
        {
            smoothedPosition = correctedPosition;
            velocity = Vector3.zero;
            wasColliding = true;
        }
        else if (wasColliding)
        {
            smoothedPosition = Vector3.SmoothDamp(smoothedPosition, desiredPosition, ref velocity, collisionSmoothTime);

            if (Vector3.Distance(smoothedPosition, desiredPosition) <= 0.01f)
            {
                wasColliding = false;
            }
        }
        else
        {
            smoothedPosition = desiredPosition;
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