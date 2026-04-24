using UnityEngine;

public class EnemyInteraction : MonoBehaviour
{

    [Header("Cámara")]
    [SerializeField] private float enemyFOV = 30f;

    ThirdPersonCam cam;
    ObstacleInteraction obstacleInteraction;
    // referencia al script de postFX
    CameraPostFXController postFX;

    bool enemyInside;
    Transform currentEnemyLookAt; // lo vamos a usar por si el enemigo ya se encontraba dentro del obstaculo antes del jugador

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

        // Si el jugador YA está dentro, activamos
        if (obstacleInteraction.PlayerInObstacle)
        {
            EnterEnemyMode(currentEnemyLookAt);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsEnemy(other)) return;

        enemyInside = true;
        currentEnemyLookAt = other.transform.Find("EnemyLookAt");

        if (obstacleInteraction.PlayerInObstacle)
        {
            EnterEnemyMode(currentEnemyLookAt);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsEnemy(other)) return;

        enemyInside = false;
        currentEnemyLookAt = null;

        ExitEnemyMode();
    }

    // metodo por si el enemigo ya estaba dentro de la pared cuando el jugador entra
    public void CheckEnemyInsideOnPlayerEnter()
    {
        if (enemyInside && currentEnemyLookAt != null)
        {
            EnterEnemyMode(currentEnemyLookAt);
        }
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
        if (!obstacleInteraction.PlayerInObstacle) return;

        cam.ReturnToObstacleFollow();

        postFX?.StopEnemyFX();
    }

    // llamado desde ObstacleInteraction si el jugador se va
    public void ForceCancel()
    {
        if (!enemyInside) return;

        enemyInside = false;
        ExitEnemyMode();
    }
}

