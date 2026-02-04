using UnityEngine;
using TMPro;

/// <summary>
/// VERSIÓN MEJORADA - ObjetivoManager con guardado/carga de progreso de misiones
/// Cambios principales:
/// - Guarda el índice de la misión actual en checkpoints
/// - Se restaura la misión correcta al cargar checkpoint
/// - Integración con GameManager para persistencia
/// </summary>
public class ObjetivoManager : MonoBehaviour
{
    #region Singleton
    public static ObjetivoManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    #endregion

    [Header("=== REFERENCIAS UI ===")]
    [SerializeField] private TextMeshProUGUI textoObjetivo;
    [SerializeField] private TextMeshProUGUI textoQueHacer;

    [Header("=== LISTA DE MISIONES ===")]
    [Tooltip("Array de todas las misiones en orden (0, 1, 2...)")]
    [SerializeField] private MisionData[] misiones;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool mostrarLogsDetallados = true;

    private int misionActualIndex = 0;
    private MisionData misionActual;

    private void Start()
    {
        if (misiones == null || misiones.Length == 0)
        {
            Debug.LogWarning("[ObjetivoManager] No hay misiones asignadas en el array");
            return;
        }

        // ✅ NUEVO: NO cargar la primera misión automáticamente
        // Esperar a que GameManager cargue el estado guardado
        // (se cargará en CargarEstadoMision o se usará la misión 0 por defecto)
    }

    /// <summary>
    /// Carga una misión por su índice
    /// </summary>
    public void CargarMision(int index)
    {
        // Verificar si completamos todas las misiones
        if (index < 0 || index >= misiones.Length)
        {
            MostrarMisionesCompletadas();
            return;
        }

        misionActualIndex = index;
        misionActual = misiones[index];

        if (misionActual == null)
        {
            Debug.LogError($"[ObjetivoManager] Misión en índice {index} es NULL");
            return;
        }

        // Actualizar UI
        if (textoObjetivo != null)
        {
            textoObjetivo.text = misionActual.textoObjetivo;
        }

        if (textoQueHacer != null)
        {
            textoQueHacer.text = misionActual.textoQueHacer;
        }

        if (mostrarLogsDetallados)
        {
            Debug.Log($"[ObjetivoManager] ✅ Misión #{misionActual.misionID} cargada: '{misionActual.textoObjetivo}'");
            Debug.Log($"[ObjetivoManager] 📝 Qué hacer: '{misionActual.textoQueHacer}'");
            Debug.Log($"[ObjetivoManager] 🎯 Item requerido: '{misionActual.itemRequeridoID}'");
        }
    }

    /// <summary>
    /// Llamar cuando se recolecta un item (desde InventoryUIManager)
    /// </summary>
    public void ItemRecolectado(string itemID)
    {
        if (misionActual == null)
        {
            if (mostrarLogsDetallados)
                Debug.Log("[ObjetivoManager] No hay misión activa");
            return;
        }

        if (mostrarLogsDetallados)
        {
            Debug.Log($"[ObjetivoManager] Item recolectado: '{itemID}'");
            Debug.Log($"[ObjetivoManager] Item requerido: '{misionActual.itemRequeridoID}'");
        }

        // Verificar si este item completa la misión actual
        if (string.IsNullOrEmpty(misionActual.itemRequeridoID))
        {
            if (mostrarLogsDetallados)
                Debug.LogWarning("[ObjetivoManager] La misión actual no tiene itemRequeridoID configurado");
            return;
        }

        if (misionActual.itemRequeridoID == itemID)
        {
            CompletarMision();
        }
    }

    /// <summary>
    /// Completa la misión actual y pasa a la siguiente
    /// </summary>
    private void CompletarMision()
    {
        Debug.Log($"[ObjetivoManager] 🎉 ¡MISIÓN COMPLETADA! #{misionActual.misionID}: '{misionActual.textoObjetivo}'");

        // Pasar a la siguiente misión
        if (misionActual.siguienteMisionID >= 0)
        {
            CargarMision(misionActual.siguienteMisionID);
        }
        else
        {
            // Era la última misión
            MostrarMisionesCompletadas();
        }
    }

    /// <summary>
    /// Muestra mensaje de todas las misiones completadas
    /// </summary>
    private void MostrarMisionesCompletadas()
    {
        if (textoObjetivo != null)
        {
            textoObjetivo.text = "¡MISIÓN COMPLETADA!";
        }

        if (textoQueHacer != null)
        {
            textoQueHacer.text = "Has completado todas las misiones disponibles.";
        }

        Debug.Log("[ObjetivoManager] 🏆 ¡TODAS LAS MISIONES COMPLETADAS!");
        misionActual = null;
    }

    /// <summary>
    /// Actualizar objetivo manualmente (para casos especiales)
    /// </summary>
    public void ActualizarObjetivoManual(string objetivo, string queHacer)
    {
        if (textoObjetivo != null)
        {
            textoObjetivo.text = objetivo;
        }

        if (textoQueHacer != null)
        {
            textoQueHacer.text = queHacer;
        }

        Debug.Log($"[ObjetivoManager] Objetivo actualizado manualmente: '{objetivo}'");
    }

    /// <summary>
    /// Forzar cambio a una misión específica (para debugging)
    /// </summary>
    public void CambiarAMision(int misionID)
    {
        CargarMision(misionID);
    }

    // ==========================================
    // ✅ NUEVOS MÉTODOS PARA GUARDADO/CARGA
    // ==========================================

    /// <summary>
    /// ✅ NUEVO: Obtiene el índice de la misión actual (para guardar en checkpoint)
    /// Llamado por GameManager.GuardarCheckpoint()
    /// </summary>
    public int ObtenerIndiceMisionActual()
    {
        return misionActualIndex;
    }

    /// <summary>
    /// ✅ NUEVO: Carga el estado de la misión desde un checkpoint
    /// Llamado por GameManager.CargarCheckpoint()
    /// </summary>
    /// <param name="indiceMision">Índice de la misión a cargar</param>
    public void CargarEstadoMision(int indiceMision)
    {
        if (indiceMision < 0 || indiceMision >= misiones.Length)
        {
            Debug.LogWarning($"[ObjetivoManager] Índice de misión inválido: {indiceMision}. Cargando misión 0");
            CargarMision(0);
            return;
        }

        CargarMision(indiceMision);

        if (mostrarLogsDetallados)
        {
            Debug.Log($"[ObjetivoManager] 🔄 Estado de misión restaurado: Misión #{indiceMision}");
        }
    }

    /// <summary>
    /// ✅ NUEVO: Inicializa la primera misión (llamar después de que GameManager termine de cargar)
    /// </summary>
    public void InicializarPrimeraMision()
    {
        if (misiones == null || misiones.Length == 0)
        {
            Debug.LogError("[ObjetivoManager] No hay misiones disponibles para inicializar");
            return;
        }

        CargarMision(0);
    }
}