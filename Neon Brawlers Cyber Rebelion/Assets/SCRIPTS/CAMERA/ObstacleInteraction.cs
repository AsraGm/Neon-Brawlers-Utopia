using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ObstacleInteraction : MonoBehaviour
{
    [Header("Referencias")]
    public Transform obstacleLookAt;
    public Transform snapPoint;

    [Header("Ajustes")]
    public float snapSpeed = 10f;

    // Estado
    public bool PlayerInObstacle { get; private set; }  // jugador en rango del trigger
    public bool PlayerIsHidden { get; private set; }  // jugador bloqueado en escondite

    // Referencias cacheadas
    PlayerMovement playerMovement;
    ThirdPersonCam cam;
    EnemyInteraction enemyInteraction;
    CameraPostFXController postFX;

    private void Awake()
    {
        enemyInteraction = GetComponent<EnemyInteraction>();
        postFX = FindFirstObjectByType<CameraPostFXController>();

        // Aseguramos que los dos botones arrancan ocultos

    }


    // TRIGGER: entrar / salir del rango

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerMovement = other.GetComponentInParent<PlayerMovement>();
        cam = FindFirstObjectByType<ThirdPersonCam>();

        if (playerMovement == null || cam == null) return;

        // Solo mostramos el HideButton, nada más
        PlayerInObstacle = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Si salía caminando mientras estaba escondido, forzamos salida limpia
        if (PlayerIsHidden)
            ExitHide();

        PlayerInObstacle = false;
    }


    // UPDATE: escuchar la tecla C

    private void Update()
    {
        if (!PlayerInObstacle) return;
        if (playerMovement == null || cam == null) return;

        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            if (!PlayerIsHidden)
                EnterHide();
            else
                ExitHide();
        }
    }


    // ESCONDERSE

    void EnterHide()
    {
        PlayerIsHidden = true;

        // Mover al jugador al snapPoint y bloquearlo
        playerMovement.EnterObstacleMode(snapPoint, snapSpeed);
        playerMovement.TeleportTo(snapPoint.position, snapPoint.rotation);

        // Cámara y PostFX
        cam.EnterObstacleMode(obstacleLookAt);
        postFX?.EnterObstacleFX();

        // Revisar si ya hay enemigo cerca
        enemyInteraction?.CheckEnemyInsideOnPlayerEnter();
    }


    // SALIR DEL ESCONDITE

    void ExitHide()
    {
        PlayerIsHidden = false;

        enemyInteraction?.ForceCancel();
        playerMovement.ExitObstacleMode();
        cam.ForceReturnToPlayer();
        postFX?.ExitObstacleFX();
        postFX?.StopEnemyFX();

    }
}