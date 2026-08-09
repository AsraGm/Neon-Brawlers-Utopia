using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manager central que decide, cada cierto intervalo (no cada frame),
/// qué luces registradas deben estar encendidas segun distancia a camara
/// y un presupuesto maximo de luces reales simultaneas.
/// Coloca este script en un GameObject vacio de la escena (uno solo).
/// </summary>
public class LightCullingManager : MonoBehaviour
{
    private static LightCullingManager _instance;
    public static bool HasInstance => _instance != null;
    public static LightCullingManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<LightCullingManager>();
            return _instance;
        }
    }

    [Tooltip("Cada cuantos segundos se revisan las distancias.")]
    [SerializeField] private float checkInterval = 0.25f;

    [Tooltip("Maximo de luces reales encendidas al mismo tiempo, aunque haya mas 'en rango'.")]
    [SerializeField] private int maxActiveLights = 12;

    private readonly List<CullableLight> _registered = new List<CullableLight>();
    private Transform _cam;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        _cam = Camera.main.transform;
    }

    private void OnEnable() => StartCoroutine(CullLoop());

    public void Register(CullableLight light)
    {
        if (!_registered.Contains(light)) _registered.Add(light);
    }

    public void Unregister(CullableLight light) => _registered.Remove(light);

    private IEnumerator CullLoop()
    {
        var wait = new WaitForSeconds(checkInterval);
        while (true)
        {
            Evaluate();
            yield return wait;
        }
    }

    private void Evaluate()
    {
        _registered.Sort((a, b) => SqrDist(a).CompareTo(SqrDist(b)));

        for (int i = 0; i < _registered.Count; i++)
        {
            var light = _registered[i];
            bool withinBudget = i < maxActiveLights;
            bool withinRange = SqrDist(light) <= light.MaxDistanceSqr;
            light.SetVisible(withinBudget && withinRange);
        }
    }

    private float SqrDist(CullableLight light) =>
        (light.CachedTransform.position - _cam.position).sqrMagnitude;
}
