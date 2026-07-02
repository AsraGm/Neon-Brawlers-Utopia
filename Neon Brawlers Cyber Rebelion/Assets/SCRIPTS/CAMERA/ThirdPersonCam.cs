using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;


public class ThirdPersonCam : MonoBehaviour
{
    // Las variables que referencian a cosas de unity como el jugador, camara y rigidbody
    [Header("Referencias")]
    public Transform player;
    public Transform orientation;
    public Transform playerObject;
    public Rigidbody rb;

    [Header("Follow Targets")]
    // El follow original, o sea el jugador
    public Transform normalFollow; 
    // el zoomlokat
    public Transform zoomFollow;
    // El lookat del asset
    public Transform obstacleFollow;

    [Header("Cinemachine")]
    public CinemachineCamera cinemachineCam; // asignada en el Inspector
    private CinemachineBasicMultiChannelPerlin perlinNoise;

    [Header("Zoom de inspección")]
    public float zoomFOV = 20f;

    [Header("Shake al correr (Cinemachine)")]
    public float runShakeAmplitude = 1.2f;
    public float runShakeFrequency = 2f;

    bool isRunning;

    bool isZooming;

    public float normalFOV = 60f;
    public float runFOV = 90f;
    public float fovSpeed = 8f;

    bool inObstacle;

    Transform obstacleFollowCached;

    // variable del valor del FOV
    float targetFOV;

    public float rotationSpeed = 10f;

    // volvemos al cursor invisible al iniciar
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        targetFOV = normalFOV;

        cinemachineCam.Follow = normalFollow;

        // obtenemos el componente Noise de la Virtual Camera
        perlinNoise = cinemachineCam
            .GetCinemachineComponent(CinemachineCore.Stage.Noise)
            as CinemachineBasicMultiChannelPerlin;


        if (perlinNoise == null)
            Debug.LogWarning("No se encontró CinemachineBasicMultiChannelPerlin en la cámara");
        else
        {
            // apagamos el shake al inicio
            perlinNoise.AmplitudeGain = 0f;
            perlinNoise.FrequencyGain = 0f;
        }


    }
    private void Update()
    {
        // Solo actualizamos la orientación si NO estamos en zoom
        if (!isZooming)
        {
            Vector3 viewDir = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
            orientation.forward = viewDir.normalized;
        }

        float horizontalInput = 0f;
        float verticalInput = 0f;

        if (Keyboard.current != null)
        {
            horizontalInput =
                (Keyboard.current.dKey.isPressed ? 1 : 0) -
                (Keyboard.current.aKey.isPressed ? 1 : 0);

            verticalInput =
                (Keyboard.current.wKey.isPressed ? 1 : 0) -
                (Keyboard.current.sKey.isPressed ? 1 : 0);
        }

        // FOV suave
        cinemachineCam.Lens.FieldOfView = Mathf.Lerp(
            cinemachineCam.Lens.FieldOfView,
            targetFOV,
            Time.unscaledDeltaTime * fovSpeed
        );

        HandleInspectionZoom();
    }

    // Método para el zoom de inspección
    void HandleInspectionZoom()
    {
        if (inObstacle) return;
        if (Mouse.current == null) return;

        // Obtener el InputAxisController que maneja el input del mouse
        CinemachineInputAxisController inputController =
            cinemachineCam.GetComponent<CinemachineInputAxisController>();

        bool zoomInput = Mouse.current.leftButton.isPressed;

        if (zoomInput && !isZooming)
        {
            isZooming = true;
            cinemachineCam.Follow = zoomFollow;
            targetFOV = zoomFOV;

            // Desactivar el componente que procesa el input de rotación
            if (inputController != null)
                inputController.enabled = false;
        }
        else if (!zoomInput && isZooming)
        {
            isZooming = false;
            cinemachineCam.Follow = normalFollow;
            targetFOV = normalFOV;

            // Reactivar el input de rotación
            if (inputController != null)
                inputController.enabled = true;
        }
    }

    public void SetCustomFollow(Transform followTarget, float fov)
    {
        obstacleFollowCached = cinemachineCam.Follow;

        targetFOV = fov;
    }

    public void ReturnToObstacleFollow()
    {
        if (!inObstacle) return;

        cinemachineCam.Follow = obstacleFollowCached;
        targetFOV = normalFOV;
    }

    public void ForceReturnToPlayer()
    {
        Debug.Log("FORZANDO CAMARA AL PLAYER");
        inObstacle = false;
        isZooming = false;

        cinemachineCam.Follow = normalFollow;
        targetFOV = normalFOV;
    }

    public void EnterObstacleMode(Transform obstacleFollowTarget)
    {
        inObstacle = true;
        isZooming = false;

        obstacleFollowCached = obstacleFollowTarget;
        cinemachineCam.Follow = obstacleFollowTarget;
        targetFOV = normalFOV;
    }

    public void ExitObstacleMode()
    {
        inObstacle = false;

        cinemachineCam.Follow = normalFollow;
        targetFOV = normalFOV;
    }


    // llamado desde PlayerMovement
    public void SetRunning(bool running)
    {
        // regresa el bool de is zooming para no entrar en conflicto
        if (isZooming) return;

        if (isRunning == running) return; // evita spam


        isRunning = running;

        targetFOV = running ? runFOV : normalFOV;

        if (perlinNoise == null) return;

        if (running)
        {
            perlinNoise.AmplitudeGain = runShakeAmplitude;
            perlinNoise.FrequencyGain = runShakeFrequency;
        }
        else
        {
            perlinNoise.AmplitudeGain = 0f;
            perlinNoise.FrequencyGain = 0f;
        }
    }

}
