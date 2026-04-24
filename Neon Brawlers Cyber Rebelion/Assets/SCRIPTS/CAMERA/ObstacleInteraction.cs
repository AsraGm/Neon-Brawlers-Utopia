using UnityEngine;

public class ObstacleInteraction : MonoBehaviour
{
    [Header("Referencias")]
    public Transform obstacleLookAt;   // el empty
    public Transform snapPoint;         // punto donde se pega el player

    [Header("Ajustes")]
    public float snapSpeed = 5f;

    // variable para saber si el jugador esta o no
    public bool PlayerInObstacle { get; private set; }

    EnemyInteraction enemyInteraction;
    CameraPostFXController postFX;

    // agarrar al script de enemyinteraction cuando inicia
    private void Awake()
    {
        enemyInteraction = GetComponent<EnemyInteraction>();
        postFX = FindFirstObjectByType<CameraPostFXController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger enter: " + other.name);

        if (!other.CompareTag("Player")) return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        ThirdPersonCam cam = FindFirstObjectByType<ThirdPersonCam>();

        if (player == null || cam == null)
        {
            Debug.Log("Player o Camara no encontrados");
            return;
        }

        EnterObstacle(player, cam);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Trigger exit: " + other.name);

        if (!other.CompareTag("Player")) return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        ThirdPersonCam cam = FindFirstObjectByType<ThirdPersonCam>();

        if (player == null || cam == null) return;

        ExitObstacle(player, cam);
    }

    void EnterObstacle(PlayerMovement player, ThirdPersonCam cam)
    {
        PlayerInObstacle = true;    

        player.EnterObstacleMode(snapPoint, snapSpeed);
        cam.EnterObstacleMode(obstacleLookAt);

        // llama al script PostFX
        postFX?.EnterObstacleFX();

        if (enemyInteraction != null)
        {
            enemyInteraction.CheckEnemyInsideOnPlayerEnter();
        }
    }

    void ExitObstacle(PlayerMovement player, ThirdPersonCam cam)
    {
        PlayerInObstacle = false;

        if (enemyInteraction != null)
            enemyInteraction.ForceCancel();

        player.ExitObstacleMode();
        cam.ForceReturnToPlayer();

        // tambien acaba todo postFX
        postFX?.ExitObstacleFX();
        postFX?.StopEnemyFX();
    }



}
