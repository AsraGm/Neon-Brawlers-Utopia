using UnityEngine;

public class CrouchObstacleInteraction : MonoBehaviour
{
    [Header("Referencias")]
    public Transform obstacleLookAt;

    PlayerMovement playerMovement;
    ThirdPersonCam cam;
    EnemyInteraction enemyInteraction;
    CameraPostFXController postFX;

    bool playerInRange = false;
    bool isActive = false;

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

        playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;

        // Si estaba activo, apagar efectos al salir
        if (isActive)
            DeactivateFX();
    }

    private void Update()
    {
        if (!playerInRange || playerMovement == null) return;

        bool crouching = playerMovement.isCrouching;

        if (crouching && !isActive)
            ActivateFX();
        else if (!crouching && isActive)
            DeactivateFX();
    }

    void ActivateFX()
    {
        isActive = true;

        cam.EnterObstacleMode(obstacleLookAt);
        postFX?.EnterObstacleFX();
        enemyInteraction?.CheckEnemyInsideOnPlayerEnter();
    }

    void DeactivateFX()
    {
        isActive = false;

        enemyInteraction?.ForceCancel();
        cam.ForceReturnToPlayer();
        postFX?.ExitObstacleFX();
        postFX?.StopEnemyFX();
    }

    // Propiedad para que EnemyInteraction pueda consultarlo igual que en ObstacleInteraction
    public bool PlayerIsHidden => isActive;
}