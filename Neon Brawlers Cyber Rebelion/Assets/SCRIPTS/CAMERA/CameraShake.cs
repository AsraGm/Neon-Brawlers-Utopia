using UnityEngine;

[DefaultExecutionOrder(110)]
public class CameraShake : MonoBehaviour, ICameraShake
{
    #region Singleton

    public static ICameraShake Instance { get; private set; }

    #endregion

    #region Presets

    [System.Serializable]
    public class ShakePreset
    {
        [Tooltip("Fuerza del shake. Recomendado: Bajo 0.1, Medio 0.25, Alto 0.5")]
        public float intensity = 0.25f;

        [Tooltip("Duracion del shake en segundos. Recomendado: Bajo 0.15, Medio 0.3, Alto 0.5")]
        public float duration = 0.3f;
    }

    [Header("Presets de Intensidad")]
    [Tooltip("Preset recomendado -> Intensidad: 0.1, Duracion: 0.15")]
    [SerializeField] private ShakePreset lowPreset = new ShakePreset { intensity = 0.1f, duration = 0.15f };

    [Tooltip("Preset recomendado -> Intensidad: 0.25, Duracion: 0.3")]
    [SerializeField] private ShakePreset mediumPreset = new ShakePreset { intensity = 0.25f, duration = 0.3f };

    [Tooltip("Preset recomendado -> Intensidad: 0.5, Duracion: 0.5")]
    [SerializeField] private ShakePreset highPreset = new ShakePreset { intensity = 0.5f, duration = 0.5f };

    #endregion

    #region Toggles

    [Header("Comportamiento")]
    [Tooltip("Activa el desplazamiento de posicion durante el shake. Recomendado: true")]
    [SerializeField] private bool usePositionShake = true;

    [Tooltip("Activa la rotacion en el eje Z durante el shake. Recomendado: false")]
    [SerializeField] private bool useRotationShake = false;

    #endregion

    #region Configuracion de Ruido

    [Header("Ruido")]
    [Tooltip("Frecuencia del ruido Perlin. Valores altos generan vibracion mas rapida. Recomendado: 25")]
    [SerializeField] private float noiseFrequency = 25f;

    [Tooltip("Multiplicador de rotacion en grados aplicado sobre la amplitud. Recomendado: 3")]
    [SerializeField] private float rotationMultiplier = 3f;

    [Tooltip("Curva de decaimiento de la intensidad a lo largo de la duracion del shake. Recomendado: EaseInOut de 1 a 0")]
    [SerializeField] private AnimationCurve decayCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    #endregion

    #region Estado Interno

    private float currentTrauma;
    private float currentDuration;
    private float elapsedTime;
    private Vector3 positionOffset;
    private Quaternion rotationOffset = Quaternion.identity;
    private float noiseSeedX;
    private float noiseSeedY;
    private float noiseSeedZ;

    #endregion

    #region Propiedades Publicas

    public bool IsShaking => currentTrauma > 0f;
    public float CurrentDuration => currentDuration;
    public float RemainingDuration => IsShaking ? Mathf.Max(0f, currentDuration - elapsedTime) : 0f;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("Ya existe una instancia de CameraShake en escena. Se sobreescribira la referencia previa.", this);
        }

        Instance = this;

        noiseSeedX = Random.Range(0f, 1000f);
        noiseSeedY = Random.Range(0f, 1000f);
        noiseSeedZ = Random.Range(0f, 1000f);
    }

    private void OnDestroy()
    {
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }

    private void LateUpdate()
    {
        UpdateShake();
        ApplyShake();
    }

    #endregion

    #region Public API

    public void Shake(ShakeIntensity intensity)
    {
        ShakePreset preset = GetPreset(intensity);
        Shake(preset.intensity, preset.duration);
    }

    public void Shake(float intensity, float duration)
    {
        currentTrauma = Mathf.Max(currentTrauma, intensity);
        currentDuration = duration;
        elapsedTime = 0f;
    }

    public void StopShake()
    {
        currentTrauma = 0f;
        currentDuration = 0f;
        elapsedTime = 0f;
        positionOffset = Vector3.zero;
        rotationOffset = Quaternion.identity;
    }

    #endregion

    #region Logica de Shake

    private ShakePreset GetPreset(ShakeIntensity intensity)
    {
        switch (intensity)
        {
            case ShakeIntensity.Low:
                return lowPreset;
            case ShakeIntensity.High:
                return highPreset;
            default:
                return mediumPreset;
        }
    }

    private void UpdateShake()
    {
        if (currentTrauma <= 0f)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        float normalizedTime = currentDuration > 0f ? Mathf.Clamp01(elapsedTime / currentDuration) : 1f;

        if (normalizedTime >= 1f)
        {
            StopShake();
            return;
        }

        float amplitude = currentTrauma * decayCurve.Evaluate(normalizedTime);

        if (usePositionShake)
        {
            float x = (Mathf.PerlinNoise(noiseSeedX, Time.time * noiseFrequency) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(noiseSeedY, Time.time * noiseFrequency) - 0.5f) * 2f;
            positionOffset = new Vector3(x, y, 0f) * amplitude;
        }

        if (useRotationShake)
        {
            float z = (Mathf.PerlinNoise(noiseSeedZ, Time.time * noiseFrequency) - 0.5f) * 2f;
            rotationOffset = Quaternion.Euler(0f, 0f, z * amplitude * rotationMultiplier);
        }
    }

    private void ApplyShake()
    {
        if (usePositionShake)
        {
            transform.position += positionOffset;
        }

        if (useRotationShake)
        {
            transform.rotation *= rotationOffset;
        }
    }

    #endregion
}

/// <summary>
///   CameraShake.Instance?.Shake(ShakeIntensity.High);
///   
///   CameraShake.Instance?.Shake(variableIntensity, variableDuration);
/// </summary>
