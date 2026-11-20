using UnityEngine;

public class CrouchObstacleInteraction : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerMovement playerMovement;
    public CameraPostFXController postFX;

    private PlayerBreath playerBreath;

    bool playerInRange = false;
    bool isActive = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        Debug.Log("Player entró al trigger");

        if (playerBreath == null)
            playerBreath = other.GetComponentInParent<PlayerBreath>();
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
        playerBreath?.StartBreath();
    }

    void DeactivateFX()
    {
        isActive = false;
        Debug.Log("DeactivateFX");

        if (HabilidadesManager.instance != null)
            HabilidadesManager.instance.playerIsHiding = false;

        postFX?.ExitObstacleFX();
        postFX?.StopEnemyFX();
        playerBreath?.StopBreath();
    }

    public bool PlayerIsHidden => isActive;
}