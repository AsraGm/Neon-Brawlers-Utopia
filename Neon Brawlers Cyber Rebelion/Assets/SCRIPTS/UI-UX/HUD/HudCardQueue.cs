using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HudCardQueue : MonoBehaviour
{
    public static HudCardQueue Instance { get; private set; }

    [Tooltip("Duración por defecto de una tarjeta (entrada + espera + salida)")]
    [SerializeField] private float duracionDefault = 4f;
    [SerializeField] private float pausaEntreTarjetas = 0.3f;

    private struct Tarjeta
    {
        public Action mostrar;
        public float duracion;
    }

    private readonly Queue<Tarjeta> cola = new Queue<Tarjeta>();
    private Coroutine rutina;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDisable()
    {
        rutina = null;
    }

    /// <param name="mostrar">Lo que pone el texto y dispara la animación de la tarjeta</param>
    /// <param name="duracion">Duración de esa tarjeta; si es menor o igual a 0 usa la default</param>
    public void Enqueue(Action mostrar, float duracion = -1f)
    {
        cola.Enqueue(new Tarjeta
        {
            mostrar = mostrar,
            duracion = duracion > 0f ? duracion : duracionDefault
        });

        if (rutina == null)
            rutina = StartCoroutine(Procesar());
    }

    public void Clear()
    {
        cola.Clear();
    }

    private IEnumerator Procesar()
    {
        while (cola.Count > 0)
        {
            Tarjeta t = cola.Dequeue();
            t.mostrar?.Invoke();

            yield return new WaitForSecondsRealtime(t.duracion + pausaEntreTarjetas);
        }

        rutina = null;
    }
}