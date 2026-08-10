using UnityEngine;

public class EnemyInteraction : MonoBehaviour
{
    CameraPostFXController postFX;
    ObstacleInteraction obstacleInteraction;
    CrouchObstacleInteraction crouchObstacle;

    bool enemyInside;
    Transform currentEnemyLookAt;

    void Awake()
    {
        postFX = FindFirstObjectByType<CameraPostFXController>();
        obstacleInteraction = GetComponent<ObstacleInteraction>();
        crouchObstacle = GetComponent<CrouchObstacleInteraction>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsEnemy(other)) return;

        enemyInside = true;
        currentEnemyLookAt = other.transform.Find("EnemyLookAt");

        if (IsPlayerHidden())
            EnterEnemyMode();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsEnemy(other)) return;

        enemyInside = true;
        currentEnemyLookAt = other.transform.Find("EnemyLookAt");

        if (IsPlayerHidden())
            EnterEnemyMode();
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
        if (enemyInside)
            EnterEnemyMode();
    }

    public void ExitEnemyMode()
    {
        if (!IsPlayerHidden()) return;
        postFX?.StopEnemyFX();
    }

    public void ForceCancel()
    {
        if (!enemyInside) return;
        enemyInside = false;
        postFX?.StopEnemyFX();
    }

    bool IsEnemy(Collider other) =>
        other.gameObject.layer == LayerMask.NameToLayer("Enemy");

    bool IsPlayerHidden()
    {
        if (obstacleInteraction != null) return obstacleInteraction.PlayerIsHidden;
        if (crouchObstacle != null) return crouchObstacle.PlayerIsHidden;
        return false;
    }

    void EnterEnemyMode()
    {
        postFX?.StartEnemyFX();
    }
}