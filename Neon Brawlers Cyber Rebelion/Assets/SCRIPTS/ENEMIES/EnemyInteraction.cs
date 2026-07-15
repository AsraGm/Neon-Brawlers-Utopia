using UnityEngine;

public class EnemyInteraction : MonoBehaviour
{
    #region Referencias

    ThirdPersonCamera cam;
    ObstacleInteraction obstacleInteraction;
    CrouchObstacleInteraction crouchObstacle;
    CameraPostFXController postFX;

    #endregion

    #region Estado

    bool enemyInside;
    Transform currentEnemyLookAt;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        cam = FindFirstObjectByType<ThirdPersonCamera>();
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

    #endregion

    #region API Pública

    public void CheckEnemyInsideOnPlayerEnter()
    {
        if (enemyInside && currentEnemyLookAt != null)
            EnterEnemyMode(currentEnemyLookAt);
    }

    public void ExitEnemyMode()
    {
        if (cam == null) return;

        if (!IsPlayerHidden()) return;

        cam.SetTenseBreathing(false);
        postFX?.StopEnemyFX();
    }

    public void ForceCancel()
    {
        if (!enemyInside) return;

        enemyInside = false;
        ExitEnemyMode();
    }

    #endregion

    #region Ayudantes

    bool IsEnemy(Collider other)
    {
        return other.gameObject.layer == LayerMask.NameToLayer("Enemy");
    }

    bool IsPlayerHidden()
    {
        if (obstacleInteraction != null) return obstacleInteraction.PlayerIsHidden;
        if (crouchObstacle != null) return crouchObstacle.PlayerIsHidden;
        return false;
    }

    void EnterEnemyMode(Transform dynamicLookAt)
    {
        if (cam == null) return;

        cam.SetTenseBreathing(true);
        postFX?.StartEnemyFX();
    }

    #endregion
}