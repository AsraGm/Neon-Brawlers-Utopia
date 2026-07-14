using UnityEngine;

public class PlayerExposure : MonoBehaviour
{
    [SerializeField] private float checkInterval = 0.15f;
    [SerializeField] private float rangoMultiplicador = 2f;

    private AnimationCurve curvaRiesgo = AnimationCurve.Linear(0, 0, 1, 1);
    private PlayerMovement playerMovement;
    private NoiseDetector[] allDetectors;
    private float timer;

    public float ExposureLevel { get; private set; } 

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        allDetectors = FindObjectsByType<NoiseDetector>(FindObjectsSortMode.None);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < checkInterval) return;
        timer = 0f;

        ExposureLevel = CalculateExposure();
    }

    float CalculateExposure()
    {
        if (playerMovement.isCrouching || !playerMovement.IsMoving)
            return 0f;

        float mayorRiesgo = 0f;

        foreach (var detector in allDetectors)
        {
            if (detector == null) continue;

            float radioBase = playerMovement.IsRunning
                ? detector.LoudNoiseRadius
                : detector.NormalNoiseRadius;

            float radioIndicador = radioBase * rangoMultiplicador;

            float distancia = Vector3.Distance(transform.position, detector.transform.position);

            if (distancia > radioIndicador) continue;

            float riesgoLineal = 1f - (distancia / radioIndicador);
            float riesgo = curvaRiesgo.Evaluate(riesgoLineal);

            if (riesgo > mayorRiesgo) mayorRiesgo = riesgo;
        }

        return mayorRiesgo;
    }
}
