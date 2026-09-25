using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraPostFXController : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Volume globalVolume;

    [Header("Chromatic Aberration")]
    [SerializeField] private float chromaSpeed = 2f;

    [Header("White Balance")]
    [SerializeField] private float hiddenTemperature = -52f;
    [SerializeField] private float enemyTemperature = 70f;
    [SerializeField] private float temperatureSpeed = 3f;

    [Header("Lens Distortion")]
    [SerializeField] private float baseLensIntensity = 0.21f;
    [SerializeField] private float hiddenLensIntensity = 0.75f;
    [SerializeField] private float lensSpeed = 3f;


    // declaramos a parte lens y chromatic para usarlos
    LensDistortion lens;
    ChromaticAberration chroma;
    WhiteBalance balance;

    
    float lensTarget = 0f;
    float temperatureTarget = 0f;
    bool chromaActive = false;

    private bool isHidden = false;


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
        //lens.intensity.Override(baseLensIntensity);
        chroma.intensity.Override(0f);
        balance.temperature.Override(0f);
    }

    void Update()
    {
        UpdateChromaticAberration();
        UpdateWhiteBalance();
        //UpdateLensDistortion();
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

    void UpdateLensDistortion()
    {
        if (lens == null) return;

        lens.intensity.value = Mathf.Lerp
        (
            lens.intensity.value,
            lensTarget,
            Time.deltaTime * lensSpeed
        );
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
        temperatureTarget = hiddenTemperature;
        lensTarget = hiddenLensIntensity;
    }

    public void ExitObstacleFX()
    {
        lensTarget = baseLensIntensity; 
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
        temperatureTarget = isHidden ? hiddenTemperature : 0f;
    }

}
