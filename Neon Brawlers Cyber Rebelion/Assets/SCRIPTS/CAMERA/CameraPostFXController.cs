using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraPostFXController : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Volume globalVolume;

    [Header("Lens Distortion")]
    [SerializeField] private float lensTargetHidden = 0.76f;
    [SerializeField] private float lensSpeed = 3f;

    [Header("Chromatic Aberration")]
    [SerializeField] private float chromaSpeed = 2f;

    [Header("White Balance")]
    [SerializeField] private float hiddenTemperature = -32f;
    [SerializeField] private float enemyTemperature = 50f;
    [SerializeField] private float temperatureSpeed = 3f;

    // declaramos a parte lens y chromatic para usarlos
    LensDistortion lens;
    ChromaticAberration chroma;
    WhiteBalance balance;

    
    float lensTarget = 0f;
    float temperatureTarget = 0f;
    bool chromaActive = false;

    // lo PRIMERITO es que se asegure de encontrar el global volume con sus valores de lens y aberration
    void Awake()
    {
        if (globalVolume == null)
            globalVolume = FindFirstObjectByType<Volume>();

        if (globalVolume == null)
        {
            Debug.LogError("No se encontró ningún Volume en la escena");
            enabled = false;
            return;
        }

        VolumeProfile profile = globalVolume.profile;

        if (!profile.TryGet(out lens))
            Debug.LogError("Lens Distortion no está en el Volume Profile");

        if (!profile.TryGet(out chroma))
            Debug.LogError("Chromatic Aberration no está en el Volume Profile");

        if (!profile.TryGet(out balance))
            Debug.LogError("White Balance no está en el Volume Profile");

        // Seguridad inicial
        lens.intensity.Override(0f);
        chroma.intensity.Override(0f);
        balance.temperature.Override(0f);
    }

    void Update()
    {
        UpdateLensDistortion();
        UpdateChromaticAberration();
        UpdateWhiteBalance();
    }

    void UpdateLensDistortion()
    {
        if (lens == null) return;

        lens.intensity.value = Mathf.Lerp(
            lens.intensity.value,
            lensTarget,
            Time.deltaTime * lensSpeed
        );
    }

    void UpdateChromaticAberration()
    {
        if (chroma == null) return;

        if (chromaActive)
        {
            chroma.intensity.value =
                Mathf.PingPong(Time.time * chromaSpeed, 1f);
        }
        else
        {
            chroma.intensity.value = Mathf.Lerp(
                chroma.intensity.value,
                0f,
                Time.deltaTime * chromaSpeed
            );
        }
    }

    void UpdateWhiteBalance()
    {
        if (balance == null) return;

        balance.temperature.value = Mathf.Lerp(
            balance.temperature.value,
            temperatureTarget,
            Time.deltaTime * temperatureSpeed
        );
    }


    // =========================
    // MÉTODOS PÚBLICOS (EVENTOS)
    // =========================

    public void EnterObstacleFX()
    {
        lensTarget = lensTargetHidden;
        temperatureTarget = hiddenTemperature;
    }


    public void ExitObstacleFX()
    {
        lensTarget = 0f;
        temperatureTarget = 0f;
    }

    public void StartEnemyFX()
    {
        chromaActive = true;
        temperatureTarget = enemyTemperature;
    }


    public void StopEnemyFX()
    {
        chromaActive = false;

        // Si sigue en obstáculo, regresa a temp de escondido
        if (lensTarget > 0f)
            temperatureTarget = hiddenTemperature;
        else
            temperatureTarget = 0f;
    }

}
