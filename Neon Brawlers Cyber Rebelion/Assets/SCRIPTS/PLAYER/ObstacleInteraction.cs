using UnityEngine;
using UnityEngine.InputSystem;

public class ObstacleInteraction : MonoBehaviour
{
    #region Referencias

    [Header("Referencias")]
    [Tooltip("Punto al que se coloca y ancla el jugador mientras está escondido")]
    public Transform snapPoint;

    [Tooltip("Holder que define la posición y rotación exactas de la cámara durante el modo sigilo")]
    public CamaraStealthHolder cameraStealthHolder;

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

    #region Estado

    public bool PlayerInObstacle { get; private set; }
    public bool PlayerIsHidden { get; private set; }

    private PlayerMovement playerMovement;
    private ThirdPersonCamera cam;
    private EnemyInteraction enemyInteraction;
    private CameraPostFXController postFX;

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
        if (Keyboard.current == null) return;

        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            if (!PlayerIsHidden)
                EnterHide();
            else
                ExitHide();
        }
    }

    #endregion

    #region Detección

    private void DetectPlayerInRange()
    {
        if (detectionOrigin == null) return;

        Collider[] hits = Physics.OverlapBox(GetDetectionOrigin(), detectionBoxSize * 0.5f, detectionOrigin.rotation, playerLayer);

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
        playerMovement = null;

        HudManager.instance?.HideAllHideUI();
    }

    #endregion

    #region Hide

    private void EnterHide()
    {
        if (!CanEnterHide()) return;

        PlayerIsHidden = true;

        if (HabilidadesManager.instance != null)
            HabilidadesManager.instance.playerIsHiding = true;

        playerMovement.EnterHideMode(snapPoint);
        cam.EnterObstacleMode(cameraStealthHolder);

        postFX?.EnterObstacleFX();
        enemyInteraction?.CheckEnemyInsideOnPlayerEnter();

        HudManager.instance?.HideAllHideUI();
        HudManager.instance?.ShowExitHideButton();
    }

    private void ExitHide()
    {
        if (!PlayerIsHidden) return;

        PlayerIsHidden = false;

        if (HabilidadesManager.instance != null)
            HabilidadesManager.instance.playerIsHiding = false;

        enemyInteraction?.ForceCancel();

        postFX?.ExitObstacleFX();
        postFX?.StopEnemyFX();

        cam?.ForceReturnToPlayer();
        playerMovement?.ExitHideMode();

        HudManager.instance?.HideAllHideUI();

        if (PlayerInObstacle)
            HudManager.instance?.ShowHideButton();
    }

    private bool CanEnterHide()
    {
        if (playerMovement == null)
        {
            Debug.LogWarning($"{name}: No se puede entrar al modo sigilo porque PlayerMovement es NULL.");
            return false;
        }

        if (snapPoint == null)
        {
            Debug.LogWarning($"{name}: No se puede entrar al modo sigilo porque SnapPoint es NULL.");
            return false;
        }

        if (cameraStealthHolder == null)
        {
            Debug.LogWarning($"{name}: No se puede entrar al modo sigilo porque CamaraStealthHolder es NULL.");
            return false;
        }

        if (cam == null)
        {
            Debug.LogWarning($"{name}: No se puede entrar al modo sigilo porque ThirdPersonCamera es NULL.");
            return false;
        }

        return true;
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