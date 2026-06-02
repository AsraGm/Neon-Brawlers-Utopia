using UnityEngine;
using UnityEngine.InputSystem;

public class ObstacleInteraction : MonoBehaviour
{
    [Header("Referencias")]
    public Transform obstacleLookAt;
    public Transform snapPoint;

    [Header("Hide Walking Zone")]
    public Collider hideWalkingZone; // el trigger hijo del obstáculo

    [Header("Ajustes")]
    public float snapSpeed = 10f;

    public bool PlayerInObstacle { get; private set; }
    public bool PlayerIsHidden { get; private set; }

    PlayerMovement playerMovement;
    ThirdPersonCam cam;
    EnemyInteraction enemyInteraction;
    CameraPostFXController postFX;

    private void Awake()
    {
        enemyInteraction = GetComponent<EnemyInteraction>();
        postFX = FindFirstObjectByType<CameraPostFXController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerMovement = other.GetComponentInParent<PlayerMovement>();
        cam = FindFirstObjectByType<ThirdPersonCam>();

        if (playerMovement == null || cam == null) return;

        if (!PlayerInObstacle)
        {
            PlayerInObstacle = true;
            HudManager.instance?.ShowHideButton();
            Debug.Log("Entras a colider");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (PlayerIsHidden)
            ExitHide();

        PlayerInObstacle = false;
        HudManager.instance?.HideAllHideUI();
        Debug.Log("Sales de colider");
    }

    private void Update()
    {
        if (!PlayerInObstacle) return;
        if (playerMovement == null || cam == null) return;

        // Tecla H para entrar/salir
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            if (!PlayerIsHidden)
                EnterHide();
            else
                ExitHide();
        }

        // Confinar al jugador dentro del HideWalkingZone mientras está escondido
        if (PlayerIsHidden && hideWalkingZone != null)
        {
            Vector3 pos = playerMovement.transform.position;
            Vector3 clamped = hideWalkingZone.ClosestPoint(pos);

            // ClosestPoint devuelve el punto más cercano DENTRO del collider
            // Si el jugador ya está dentro, clamped == pos y no pasa nada
            // Si intenta salir, lo regresa al borde
        }
    }

    void EnterHide()
    {
        PlayerIsHidden = true;

        // Entrar en modo obstáculo SIN bloquear, solo cambia velocidad
        playerMovement.EnterHideMode(snapPoint, snapSpeed);

        // Cámara y efectos
        cam.EnterObstacleMode(obstacleLookAt);
        postFX?.EnterObstacleFX();
        enemyInteraction?.CheckEnemyInsideOnPlayerEnter();

        // HUD
        HudManager.instance?.HideAllHideUI();
        HudManager.instance?.ShowExitHideButton();
    }

    void ExitHide()
    {
        PlayerIsHidden = false;

        enemyInteraction?.ForceCancel();
        postFX?.ExitObstacleFX();
        postFX?.StopEnemyFX();
        cam.ForceReturnToPlayer();

        // Restaurar velocidad normal
        playerMovement.ExitHideMode();

        // HUD
        HudManager.instance?.HideAllHideUI();
        if (PlayerInObstacle)
            HudManager.instance?.ShowHideButton();
    }
}