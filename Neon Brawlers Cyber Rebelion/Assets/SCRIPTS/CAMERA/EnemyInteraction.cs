using UnityEngine;

public class EnemyInteraction : MonoBehaviour
{
    [Header("Cámara")]
    [SerializeField] private float enemyFOV = 30f;

    ThirdPersonCam cam;
    ObstacleInteraction obstacleInteraction;
    CameraPostFXController postFX;

    bool enemyInside;
    Transform currentEnemyLookAt;

    void Awake()
    {
        cam = FindFirstObjectByType<ThirdPersonCam>();
        obstacleInteraction = GetComponent<ObstacleInteraction>();
        postFX = FindFirstObjectByType<CameraPostFXController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsEnemy(other)) return;

        enemyInside = true;
        currentEnemyLookAt = other.transform.Find("EnemyLookAt");

        // Solo reacciona si el jugador YA está escondido (tecla H presionada)
        if (obstacleInteraction.PlayerIsHidden)
            EnterEnemyMode(currentEnemyLookAt);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsEnemy(other)) return;

        enemyInside = true;
        currentEnemyLookAt = other.transform.Find("EnemyLookAt");

        if (obstacleInteraction.PlayerIsHidden)
            EnterEnemyMode(currentEnemyLookAt);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsEnemy(other)) return;

        enemyInside = false;
        currentEnemyLookAt = null;
        ExitEnemyMode();
    }

    // Llamado desde ObstacleInteraction al presionar H, por si el enemigo ya estaba dentro
    public void CheckEnemyInsideOnPlayerEnter()
    {
        if (enemyInside && currentEnemyLookAt != null)
            EnterEnemyMode(currentEnemyLookAt);
    }

    bool IsEnemy(Collider other)
    {
        return other.gameObject.layer == LayerMask.NameToLayer("Enemy");
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

        // Solo revierte si el jugador sigue escondido
        if (!obstacleInteraction.PlayerIsHidden) return;

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