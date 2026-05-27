using UnityEngine;
using UnityEngine.InputSystem;

public class ObstacleInteraction : MonoBehaviour
{
    [Header("Referencias")]
    public Transform obstacleLookAt;
    public Transform snapPoint;

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


        Debug.Log("Entras a colider");

        if (!PlayerInObstacle)
        {
            // Solo HideButton, ningún efecto todavía
            HudManager.instance?.ShowHideButton();

            PlayerInObstacle = true;
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Si se va mientras está escondido, limpieza completa
        if (PlayerIsHidden)
            ExitHide();

        PlayerInObstacle = false;
        Debug.Log("Sales de colider");
        HudManager.instance?.HideAllHideUI();
    }

    private void Update()
    {
        if (!PlayerInObstacle) return;
        if (playerMovement == null || cam == null) return;

        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            if (!PlayerIsHidden)
                EnterHide();
            else
                ExitHide();
        }
    }

    void EnterHide()
    {
        PlayerIsHidden = true;

        // Primero movimiento y posición
        playerMovement.EnterObstacleMode(snapPoint, snapSpeed);
        playerMovement.TeleportTo(snapPoint.position, snapPoint.rotation);

        // Luego cámara y efectos
        cam.EnterObstacleMode(obstacleLookAt);
        postFX?.EnterObstacleFX();

        // Revisar enemigo (ahora que PlayerIsHidden = true, EnemyInteraction reaccionará)
        enemyInteraction?.CheckEnemyInsideOnPlayerEnter();

        // HUD: cambia DESPUÉS de que todo está activo
        HudManager.instance?.HideAllHideUI();
        HudManager.instance?.ShowExitHideButton();
    }

    void ExitHide()
    {
        // Primero apagamos efectos
        enemyInteraction?.ForceCancel();
        postFX?.ExitObstacleFX();
        postFX?.StopEnemyFX();
        cam.ForceReturnToPlayer();
        playerMovement.ExitObstacleMode();

        // Marcamos el estado DESPUÉS de limpiar
        PlayerIsHidden = false;

        // HUD: vuelve a HideButton si sigue en rango, o se oculta todo
        HudManager.instance?.HideAllHideUI();
        if (PlayerInObstacle)
            HudManager.instance?.ShowHideButton();
        else
            HudManager.instance?.HideAllHideUI();
    }
}
