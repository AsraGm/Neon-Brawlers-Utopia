using UnityEngine;
using UnityEngine.InputSystem;

public class ObstacleInteraction : MonoBehaviour
{
    #region Referencias

    [Header("Referencias")]
    [Tooltip("Punto que la cámara mira mientras el jugador está en rango del obstáculo")]
    public Transform obstacleLookAt;

    [Tooltip("Punto al que se ancla el jugador mientras está escondido")]
    public Transform snapPoint;

    #endregion

    #region Cámara

    [Header("Cámara")]
    [Tooltip("Offset de posición adicional aplicado sobre obstacleLookAt, propio de este obstáculo")]
    public Vector3 cameraPositionOffset = Vector3.zero;

    [Tooltip("Offset de rotación adicional aplicado sobre obstacleLookAt, propio de este obstáculo")]
    public Vector3 cameraRotationOffset = Vector3.zero;

    #endregion

    #region Detección

    [Header("Detección")]
    [Tooltip("Origen y orientación de la zona de detección")]
    public Transform detectionOrigin;

    [Tooltip("Desplazamiento local del centro de la zona de detección respecto a detectionOrigin")]
    public Vector3 detectionOffset = Vector3.zero;

    [Tooltip("Tamaño completo de la zona de detección")]
    public Vector3 detectionBoxSize = new Vector3(1f, 2f, 0.5f);

    [Tooltip("Capas consideradas como jugador para la detección")]
    public LayerMask playerLayer;

    #endregion

    #region Hide Walking Zone

    [Header("Hide Walking Zone")]
    [Tooltip("Trigger hijo del obstáculo que delimita el área de movimiento mientras el jugador está escondido")]
    public Collider hideWalkingZone;

    #endregion

    #region Ajustes

    [Header("Ajustes")]
    [Tooltip("Velocidad de desplazamiento del jugador mientras está anclado al obstáculo")]
    public float snapSpeed = 10f;

    #endregion

    #region Estado

    public bool PlayerInObstacle { get; private set; }
    public bool PlayerIsHidden { get; private set; }

    PlayerMovement playerMovement;
    ThirdPersonCamera cam;
    EnemyInteraction enemyInteraction;
    CameraPostFXController postFX;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        enemyInteraction = GetComponent<EnemyInteraction>();
        postFX = FindFirstObjectByType<CameraPostFXController>();
        cam = FindFirstObjectByType<ThirdPersonCamera>();
    }

    private void Update()
    {
        DetectPlayerInRange();

        if (!PlayerInObstacle) return;

        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            if (!PlayerIsHidden)
                EnterHide();
            else
                ExitHide();
        }

        if (PlayerIsHidden && hideWalkingZone != null)
        {
            ClampPlayerToWalkingZone();
        }
    }

    #endregion

    #region Detección

    private void DetectPlayerInRange()
    {
        if (detectionOrigin == null) return;

        Collider[] hits = Physics.OverlapBox(
            GetDetectionOrigin(),
            detectionBoxSize * 0.5f,
            detectionOrigin.rotation,
            playerLayer
        );

        bool inRange = hits.Length > 0;

        if (inRange && !PlayerInObstacle)
        {
            EnterRange(hits[0]);
        }
        else if (!inRange && PlayerInObstacle)
        {
            ExitRange();
        }
    }

    private Vector3 GetDetectionOrigin()
    {
        return detectionOrigin.position + detectionOrigin.rotation * detectionOffset;
    }

    private void EnterRange(Collider playerCollider)
    {
        playerMovement = playerCollider.GetComponentInParent<PlayerMovement>();

        if (playerMovement == null) return;

        PlayerInObstacle = true;
        HudManager.instance?.ShowHideButton();
    }

    private void ExitRange()
    {
        if (PlayerIsHidden)
            ExitHide();

        PlayerInObstacle = false;
        HudManager.instance?.HideAllHideUI();
    }

    #endregion

    #region Hide

    void EnterHide()
    {
        PlayerIsHidden = true;
        HabilidadesManager.instance.playerIsHiding = true;

        playerMovement.EnterHideMode(snapPoint, snapSpeed);

        cam?.EnterObstacleMode(obstacleLookAt, cameraPositionOffset, cameraRotationOffset);
        postFX?.EnterObstacleFX();
        enemyInteraction?.CheckEnemyInsideOnPlayerEnter();

        HudManager.instance?.HideAllHideUI();
        HudManager.instance?.ShowExitHideButton();
    }

    void ExitHide()
    {
        PlayerIsHidden = false;
        HabilidadesManager.instance.playerIsHiding = false;

        enemyInteraction?.ForceCancel();
        postFX?.ExitObstacleFX();
        postFX?.StopEnemyFX();
        cam?.ForceReturnToPlayer();

        playerMovement.ExitHideMode();

        HudManager.instance?.HideAllHideUI();
        if (PlayerInObstacle)
            HudManager.instance?.ShowHideButton();
    }

    void ClampPlayerToWalkingZone()
    {
        Vector3 pos = playerMovement.transform.position;
        Vector3 clamped = hideWalkingZone.ClosestPoint(pos);

        playerMovement.transform.position = clamped;
    }

    #endregion

    #region Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (detectionOrigin == null) return;

        Vector3 center = GetDetectionOrigin();

        Matrix4x4 originalMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, detectionOrigin.rotation, Vector3.one);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, detectionBoxSize);
        Gizmos.matrix = originalMatrix;
    }
#endif

    #endregion
}