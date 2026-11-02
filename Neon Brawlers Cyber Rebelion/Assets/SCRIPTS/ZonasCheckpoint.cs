using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ZonasCheckpoint : MonoBehaviour
{
    [System.Serializable]
    public class Zona
    {
        [Tooltip("Solo para que tu sepas cual es (ej: Puerta cuarto 2). No afecta nada.")]
        public string nota;

        [Tooltip("El objeto padre de la zona que YA esta en la escena (el que reemplazaremos).")]
        public GameObject instanciaActual;

        [Tooltip("El prefab base. Si instanciaActual es una instancia de prefab, se llena solo en el editor.")]
        public GameObject prefab;

        // Datos originales, se capturan al iniciar la escena
        [System.NonSerialized] public Vector3 posicion;
        [System.NonSerialized] public Quaternion rotacion;
        [System.NonSerialized] public Vector3 escala;
        [System.NonSerialized] public Transform padre;
        [System.NonSerialized] public int indiceHermano;
        [System.NonSerialized] public string nombre;
        [System.NonSerialized] public bool activoOriginal;
        [System.NonSerialized] public bool capturada;
    }

    [Header("Filtro")]
    [SerializeField] private string jugadorTag = "Player";
    [SerializeField] private bool soloUnaVez = true;

    [Header("Zonas que se reinician al morir despues de este checkpoint")]
    [SerializeField] private List<Zona> zonas = new List<Zona>();

    [Header("Objetos que NO se reemplazan, solo vuelven a su estado original (puertas, etc.)")]
    [Tooltip("Arrastra aqui las puertas. Al reiniciar vuelven a su posicion, activo/apagado y Animator del inicio.")]
    [SerializeField] private List<GameObject> objetosAReiniciar = new List<GameObject>();

    [Tooltip("Opcional: cualquier cosa extra que quieras ejecutar al reiniciar (ej: reactivar un collider).")]
    [SerializeField] private UnityEvent alReiniciar;

    [Header("Opciones")]
    [Tooltip("Avisa al AudioManager que un enemigo dejo de perseguir antes de destruirlo, para que no se quede la musica de persecucion.")]
    [SerializeField] private bool avisarAudioAlDestruir = true;
    [SerializeField] private bool mostrarLogs = true;

    private bool usado = false;

    private class EstadoObjeto
    {
        public Vector3 posicion;
        public Quaternion rotacion;
        public Vector3 escala;
        public bool activo;
    }

    private readonly Dictionary<GameObject, EstadoObjeto> estadosOriginales = new Dictionary<GameObject, EstadoObjeto>();

    private static ZonasCheckpoint activo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEstaticos()
    {
        activo = null;
    }

    private void Awake()
    {
        foreach (Zona z in zonas)
            Capturar(z);

        foreach (GameObject o in objetosAReiniciar)
        {
            if (o == null) continue;
            estadosOriginales[o] = new EstadoObjeto
            {
                posicion = o.transform.position,
                rotacion = o.transform.rotation,
                escala = o.transform.localScale,
                activo = o.activeSelf
            };
        }
    }

    private void OnDestroy()
    {
        if (activo == this) activo = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(jugadorTag)) return;
        if (soloUnaVez && usado) return;

        usado = true;
        activo = this;

        if (mostrarLogs)
            Debug.Log($"[ZonasCheckpoint] '{name}' es ahora el checkpoint activo ({zonas.Count} zonas)");
    }

    public static void ResetearActivas()
    {
        if (activo == null) return;
        activo.ResetearZonas();
    }

    private void ResetearZonas()
    {
        foreach (Zona z in zonas)
            Reiniciar(z);

        foreach (GameObject o in objetosAReiniciar)
            RestaurarObjeto(o);

        alReiniciar?.Invoke();

        Physics.SyncTransforms();

        if (mostrarLogs)
            Debug.Log($"[ZonasCheckpoint] '{name}' reinicio {zonas.Count} zonas");
    }

    private void Capturar(Zona z)
    {
        if (z == null || z.capturada || z.instanciaActual == null) return;

        Transform t = z.instanciaActual.transform;
        z.posicion = t.position;
        z.rotacion = t.rotation;
        z.escala = t.localScale;
        z.padre = t.parent;
        z.indiceHermano = t.GetSiblingIndex();
        z.nombre = z.instanciaActual.name;
        z.activoOriginal = z.instanciaActual.activeSelf;
        z.capturada = true;
    }

    private void Reiniciar(Zona z)
    {
        if (z == null) return;

        if (z.prefab == null)
        {
            Debug.LogWarning($"[ZonasCheckpoint] La zona '{z.nota}' no tiene prefab asignado");
            return;
        }

        if (!z.capturada)
        {
            Debug.LogWarning($"[ZonasCheckpoint] La zona '{z.nota}' no tiene instanciaActual asignada");
            return;
        }

        if (z.instanciaActual != null)
        {
            if (avisarAudioAlDestruir)
                AvisarEnemigosPersiguiendo(z.instanciaActual);

            z.instanciaActual.SetActive(false);
            Destroy(z.instanciaActual);
        }

        GameObject nuevo = Instantiate(z.prefab, z.posicion, z.rotacion, z.padre);
        nuevo.name = z.nombre;
        nuevo.transform.localScale = z.escala;
        nuevo.transform.SetSiblingIndex(z.indiceHermano);
        nuevo.SetActive(z.activoOriginal);

        z.instanciaActual = nuevo;
    }

    private void RestaurarObjeto(GameObject o)
    {
        if (o == null || !estadosOriginales.TryGetValue(o, out EstadoObjeto e)) return;

        // Primero se activa para poder reiniciar su Animator
        o.SetActive(true);

        o.transform.SetPositionAndRotation(e.posicion, e.rotacion);
        o.transform.localScale = e.escala;

        Rigidbody rb = o.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
        }

        foreach (Animator a in o.GetComponentsInChildren<Animator>(true))
        {
            a.Rebind();
            a.Update(0f);
        }

        if (!e.activo)
            o.SetActive(false);
    }

    private void AvisarEnemigosPersiguiendo(GameObject zona)
    {
        EnemyPatrol[] enemigos = zona.GetComponentsInChildren<EnemyPatrol>(true);
        foreach (EnemyPatrol e in enemigos)
        {
            if (e != null && e.isChasing)
                AudioManager.instance?.EnemyStoppedChasing();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (zonas == null) return;

        foreach (Zona z in zonas)
        {
            if (z == null || z.prefab != null || z.instanciaActual == null) continue;

            GameObject fuente = PrefabUtility.GetCorrespondingObjectFromOriginalSource(z.instanciaActual);
            if (fuente != null)
                z.prefab = fuente;
        }
    }
#endif
}