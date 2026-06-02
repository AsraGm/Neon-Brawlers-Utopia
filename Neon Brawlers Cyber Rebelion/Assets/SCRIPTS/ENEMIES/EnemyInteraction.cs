using UnityEngine;

public class EnemyInteraction : MonoBehaviour
{
    [Header("Cámara")]
    [SerializeField] private float enemyFOV = 30f;

    ThirdPersonCam cam;
    ObstacleInteraction obstacleInteraction;
    CrouchObstacleInteraction crouchObstacle;
    CameraPostFXController postFX;

    bool enemyInside;
    Transform currentEnemyLookAt;

    void Awake()
    {
        cam = FindFirstObjectByType<ThirdPersonCam>();
        postFX = FindFirstObjectByType<CameraPostFXController>();

        // Busca cualquiera de los dos tipos de obstacle en este mismo GameObject
        obstacleInteraction = GetComponent<ObstacleInteraction>();
        crouchObstacle = GetComponent<CrouchObstacleInteraction>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsEnemy(other)) return;

        enemyInside = true;
        currentEnemyLookAt = other.transform.Find("EnemyLookAt");

        if (IsPlayerHidden())
            EnterEnemyMode(currentEnemyLookAt);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsEnemy(other)) return;

        enemyInside = true;
        currentEnemyLookAt = other.transform.Find("EnemyLookAt");

        if (IsPlayerHidden())
            EnterEnemyMode(currentEnemyLookAt);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsEnemy(other)) return;

        enemyInside = false;
        currentEnemyLookAt = null;
        ExitEnemyMode();
    }

    public void CheckEnemyInsideOnPlayerEnter()
    {
        if (enemyInside && currentEnemyLookAt != null)
            EnterEnemyMode(currentEnemyLookAt);
    }

    bool IsEnemy(Collider other)
    {
        return other.gameObject.layer == LayerMask.NameToLayer("Enemy");
    }

    // Helper que consulta cualquiera de los dos tipos de obstacle
    bool IsPlayerHidden()
    {
        if (obstacleInteraction != null) return obstacleInteraction.PlayerIsHidden;
        if (crouchObstacle != null) return crouchObstacle.PlayerIsHidden;
        return false;
    }

    void EnterEnemyMode(Transform dynamicLookAt)
    {
        if (cam == null) return;
        cam.SetCustomFollow(dynamicLookAt, enemyFOV);
        postFX?.StartEnemyFX();
    }

    public void ExitEnemyMode()
    {
        if (cam == null) return;

        if (!IsPlayerHidden()) return;

        cam.ReturnToObstacleFollow();
        postFX?.StopEnemyFX();
    }

    public void ForceCancel()
    {
        if (!enemyInside) return;
        enemyInside = false;
        ExitEnemyMode();
    }
}