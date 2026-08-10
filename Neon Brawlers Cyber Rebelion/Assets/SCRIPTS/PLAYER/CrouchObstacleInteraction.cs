using UnityEngine;

public class CrouchObstacleInteraction : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerMovement playerMovement;
    public CameraPostFXController postFX;
    public EnemyInteraction enemyInteraction;

    bool playerInRange = false;
    bool isActive = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        Debug.Log("Player entró al trigger");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        Debug.Log("Player salió del trigger");

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
        Debug.Log("ActivateFX");

        if (HabilidadesManager.instance != null)
            HabilidadesManager.instance.playerIsHiding = true;

        postFX?.EnterObstacleFX();
        enemyInteraction?.CheckEnemyInsideOnPlayerEnter();
    }

    void DeactivateFX()
    {
        isActive = false;
        Debug.Log("DeactivateFX");

        if (HabilidadesManager.instance != null)
            HabilidadesManager.instance.playerIsHiding = false;

        enemyInteraction?.ForceCancel();
        postFX?.ExitObstacleFX();
        postFX?.StopEnemyFX();
    }

    public bool PlayerIsHidden => isActive;
}