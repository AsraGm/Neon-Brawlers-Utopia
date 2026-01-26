using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Controla el sistema de misiones/objetivos automático
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

    private int misionActualIndex = 0;
    private MisionData misionActual;

    private void Start()
    {
        if (misiones.Length > 0)
        {
            CargarMision(0);
        }
        else
        {
            Debug.LogWarning("[ObjetivoManager] No hay misiones asignadas");
        }
    }

    /// <summary>
    /// Carga una misión por su índice
    /// </summary>
    private void CargarMision(int index)
    {
        if (index < 0 || index >= misiones.Length)
        {
            Debug.Log("[ObjetivoManager] Todas las misiones completadas");
            textoObjetivo.text = "MISIÓN COMPLETADA";
            textoQueHacer.text = "¡Has completado todas las misiones!";
            return;
        }

        misionActualIndex = index;
        misionActual = misiones[index];

        textoObjetivo.text = misionActual.textoObjetivo;
        textoQueHacer.text = misionActual.textoQueHacer;

        Debug.Log($"[ObjetivoManager] Misión cargada: {misionActual.name}");
    }

    /// <summary>
    /// Llamar cuando se recolecta un item (desde InventoryUIManager)
    /// </summary>
    public void ItemRecolectado(string itemID)
    {
        if (misionActual == null) return;

        // Verificar si este item completa la misión actual
        if (misionActual.itemRequeridoID == itemID)
        {
            Debug.Log($"[ObjetivoManager] ¡Misión completada! {misionActual.name}");

            // Pasar a la siguiente misión
            if (misionActual.siguienteMisionID >= 0)
            {
                CargarMision(misionActual.siguienteMisionID);
            }
            else
            {
                // Era la última misión
                CargarMision(-1);
            }
        }
    }

    /// <summary>
    /// Actualizar objetivo manualmente (para casos especiales)
    /// </summary>
    public void ActualizarObjetivo(string objetivo, string queHacer)
    {
        textoObjetivo.text = objetivo;
        textoQueHacer.text = queHacer;
    }
}