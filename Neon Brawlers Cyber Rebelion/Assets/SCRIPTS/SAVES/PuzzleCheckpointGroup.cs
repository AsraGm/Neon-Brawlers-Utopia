using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public interface ICheckpointSaveable
{
    string CapturarEstado();
    void RestaurarEstado(string estado);
}

public class PuzzleCheckpointGroup : MonoBehaviour
{
    [Header("Objetos que se guardan/restauran")]
    [SerializeField] private GameObject[] objetos;

    [Header("Opcional")]
    [Tooltip("Déjalo vacío para generar el ID automáticamente")]
    [SerializeField] private string idManual;
    [SerializeField] private bool mostrarLogs = false;

    private class EstadoObjeto
    {
        public bool activo;
        public Vector3 posicion;
        public Quaternion rotacion;
        public Vector3 escala;
        public string[] custom;
    }

    // Estático: sobrevive a la recarga de escena
    private static readonly Dictionary<string, Dictionary<string, EstadoObjeto>> guardado =
        new Dictionary<string, Dictionary<string, EstadoObjeto>>();
    private static readonly List<PuzzleCheckpointGroup> grupos = new List<PuzzleCheckpointGroup>();

    private Dictionary<string, EstadoObjeto> baseline;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEstaticos()
    {
        guardado.Clear();
        grupos.Clear();
    }

    private string IdGrupo => string.IsNullOrEmpty(idManual)
        ? SceneManager.GetActiveScene().name + Ruta(transform)
        : idManual;

    private void Awake()
    {
        baseline = Capturar();

        if (guardado.TryGetValue(IdGrupo, out var estado))
        {
            Aplicar(estado);
            if (mostrarLogs) Debug.Log($"[PuzzleCheckpointGroup] '{name}' restaurado desde checkpoint");
        }
    }

    private void OnEnable()
    {
        if (!grupos.Contains(this)) grupos.Add(this);
    }

    private void OnDisable()
    {
        grupos.Remove(this);
    }

    public static void GuardarTodos()
    {
        foreach (var g in grupos)
            if (g != null) g.Guardar();
    }

    public static void RestaurarTodos()
    {
        foreach (var g in grupos)
            if (g != null) g.Restaurar();
    }

    public static void LimpiarTodo()
    {
        guardado.Clear();
    }

    private void Guardar()
    {
        guardado[IdGrupo] = Capturar();
        if (mostrarLogs) Debug.Log($"[PuzzleCheckpointGroup] '{name}' guardado ({objetos.Length} objetos)");
    }

    private void Restaurar()
    {
        if (guardado.TryGetValue(IdGrupo, out var estado))
            Aplicar(estado);
        else
            Aplicar(baseline); 
    }

    private Dictionary<string, EstadoObjeto> Capturar()
    {
        var dic = new Dictionary<string, EstadoObjeto>();
        if (objetos == null) return dic;

        foreach (var obj in objetos)
        {
            if (obj == null) continue;

            var savers = obj.GetComponents<ICheckpointSaveable>();
            var custom = new string[savers.Length];
            for (int i = 0; i < savers.Length; i++)
                custom[i] = savers[i].CapturarEstado();

            dic[Ruta(obj.transform)] = new EstadoObjeto
            {
                activo = obj.activeSelf,
                posicion = obj.transform.position,
                rotacion = obj.transform.rotation,
                escala = obj.transform.localScale,
                custom = custom
            };
        }
        return dic;
    }

    private void Aplicar(Dictionary<string, EstadoObjeto> estados)
    {
        if (objetos == null || estados == null) return;

        foreach (var obj in objetos)
        {
            if (obj == null) continue;
            if (!estados.TryGetValue(Ruta(obj.transform), out var e)) continue;

            obj.transform.SetPositionAndRotation(e.posicion, e.rotacion);
            obj.transform.localScale = e.escala;

            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
            }

            var savers = obj.GetComponents<ICheckpointSaveable>();
            for (int i = 0; i < savers.Length && i < e.custom.Length; i++)
                savers[i].RestaurarEstado(e.custom[i]);

            if (obj.activeSelf != e.activo)
                obj.SetActive(e.activo);
        }

        Physics.SyncTransforms();
    }

    private static string Ruta(Transform t)
    {
        var sb = new StringBuilder();
        while (t != null)
        {
            sb.Insert(0, "/" + t.GetSiblingIndex() + ":" + t.name);
            t = t.parent;
        }
        return sb.ToString();
    }
}
