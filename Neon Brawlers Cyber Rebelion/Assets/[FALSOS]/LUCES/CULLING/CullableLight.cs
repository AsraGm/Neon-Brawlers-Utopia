using System.Collections;
using UnityEngine;

/// <summary>
/// Va en el mismo GameObject que tu Light (ej: el foco parpadeante).
/// Se registra automaticamente con el LightCullingManager y hace fade
/// de intensidad al aparecer/desaparecer para que no se note el "pop".
/// Arrastra aqui tu EmissiveFlicker (u otro script que anime la luz)
/// en el arreglo "Scripts To Pause" para que se pausen cuando no se vea.
/// </summary>
[RequireComponent(typeof(Light))]
public class CullableLight : MonoBehaviour
{
    [Header("Rango")]
    [Tooltip("Distancia maxima a la camara a la que esta luz puede estar activa.")]
    [SerializeField] private float maxDistance = 15f;

    [Header("Transicion")]
    [Tooltip("Segundos que tarda en aparecer/desaparecer.")]
    [SerializeField] private float fadeDuration = 0.4f;

    [Header("Scripts a pausar cuando no se ve (ej: EmissiveFlicker)")]
    [SerializeField] private Behaviour[] scriptsToPause;

    private Light _light;
    private float _baseIntensity;
    private Coroutine _fadeRoutine;
    private bool _visible = true;

    public Transform CachedTransform { get; private set; }
    public float MaxDistanceSqr { get; private set; }

    private void Awake()
    {
        _light = GetComponent<Light>();
        _baseIntensity = _light.intensity;
        CachedTransform = transform;
        MaxDistanceSqr = maxDistance * maxDistance;
    }

    private void OnEnable() => LightCullingManager.Instance.Register(this);

    private void OnDisable()
    {
        if (LightCullingManager.HasInstance)
            LightCullingManager.Instance.Unregister(this);
    }

    public void SetVisible(bool visible)
    {
        if (visible == _visible) return;
        _visible = visible;

        foreach (var s in scriptsToPause)
            if (s != null) s.enabled = visible;

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeTo(visible ? _baseIntensity : 0f, visible));
    }

    private IEnumerator FadeTo(float target, bool willBeVisible)
    {
        if (willBeVisible) _light.enabled = true;

        float start = _light.intensity;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            _light.intensity = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        _light.intensity = target;

        if (!willBeVisible) _light.enabled = false;
    }
}
