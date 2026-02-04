using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VERSIÓN MEJORADA - LoreManager con mejor sincronización
/// Cambios principales:
/// - Mejor manejo de sincronización con InventoryUIManager
/// - Validaciones adicionales
/// - Fix en actualización de highlight al cerrar
/// </summary>
public class LoreManager : MonoBehaviour
{
    #region Singleton
    public static LoreManager Instance { get; private set; }

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
    [SerializeField] private GameObject panelLoreDetalle;
    [SerializeField] private TextMeshProUGUI textoNombre;
    [SerializeField] private TextMeshProUGUI textoDescripcion;
    [SerializeField] private Button botonCerrar;
    [SerializeField] private TextMeshProUGUI textoBotonAudio; // Texto del botón de audio (opcional)

    [Header("=== AUDIO ===")]
    [SerializeField] private AudioSource audioSource;

    [Header("=== KEYBINDS ===")]
    [SerializeField] private KeyCode teclaCerrar = KeyCode.Escape;
    [SerializeField] private KeyCode teclaToggleAudio = KeyCode.Return; // Enter para reproducir/pausar

    [Header("=== DEBUG ===")]
    [SerializeField] private bool logsDetallados = false;

    private ItemData itemActual;
    private bool panelAbierto = false;
    private bool audioPausado = false;

    private void Start()
    {
        // Cerrar panel al inicio
        CerrarPanel();

        // Configurar botón de cerrar
        if (botonCerrar != null)
            botonCerrar.onClick.AddListener(CerrarPanel);

        // Crear AudioSource si no existe
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        // ✅ NUEVO: Validación de componentes
        ValidarComponentes();
    }

    /// <summary>
    /// ✅ NUEVO: Valida que todos los componentes necesarios estén asignados
    /// </summary>
    private void ValidarComponentes()
    {
        bool todoCorrecto = true;

        if (panelLoreDetalle == null)
        {
            Debug.LogError("[LoreManager] ❌ panelLoreDetalle no asignado");
            todoCorrecto = false;
        }

        if (textoNombre == null)
        {
            Debug.LogWarning("[LoreManager] ⚠️ textoNombre no asignado");
            todoCorrecto = false;
        }

        if (textoDescripcion == null)
        {
            Debug.LogWarning("[LoreManager] ⚠️ textoDescripcion no asignado");
            todoCorrecto = false;
        }

        if (todoCorrecto && logsDetallados)
        {
            Debug.Log("[LoreManager] ✅ Todos los componentes asignados correctamente");
        }
    }

    private void Update()
    {
        if (!panelAbierto) return;

        // Cerrar con tecla
        if (Input.GetKeyDown(teclaCerrar))
        {
            CerrarPanel();
        }

        // Toggle audio con Enter
        if (Input.GetKeyDown(teclaToggleAudio))
        {
            ToggleAudio();
        }

        // Actualizar texto del botón (opcional)
        ActualizarTextoBotonAudio();
    }

    /// <summary>
    /// ✅ MEJORADO: Abre el panel con mejor validación
    /// </summary>
    public void AbrirPanelLore(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("[LoreManager] Item es null");
            return;
        }

        itemActual = item;
        panelAbierto = true;
        panelLoreDetalle.SetActive(true);

        // Mostrar info
        if (textoNombre != null)
        {
            textoNombre.text = item.nombreDisplay;
        }

        if (textoDescripcion != null)
        {
            textoDescripcion.text = item.descripcionLore;
        }

        // ✅ OCULTAR HIGHLIGHT
        OcultarHighlightInventario();

        // Reproducir audio automáticamente si existe
        if (item.audioLore != null && audioSource != null)
        {
            audioSource.clip = item.audioLore;
            audioSource.Play();
            audioPausado = false;

            if (logsDetallados)
            {
                Debug.Log($"[LoreManager] Reproduciendo audio: {item.audioLore.name}");
            }
        }
        else if (logsDetallados)
        {
            Debug.LogWarning($"[LoreManager] Item '{item.nombreDisplay}' no tiene audio asignado");
        }

        if (logsDetallados)
        {
            Debug.Log($"[LoreManager] Panel abierto para: {item.nombreDisplay}");
        }
    }

    /// <summary>
    /// ✅ MEJORADO: Cierre del panel con mejor sincronización
    /// </summary>
    public void CerrarPanel()
    {
        panelAbierto = false;
        panelLoreDetalle.SetActive(false);

        // Detener audio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        audioPausado = false;
        itemActual = null;

        // ✅ REACTIVAR HIGHLIGHT (solo si el inventario está abierto)
        ReactivarHighlightInventario();

        if (logsDetallados)
        {
            Debug.Log("[LoreManager] Panel cerrado");
        }
    }

    /// <summary>
    /// ✅ NUEVO: Método separado para ocultar highlight (más robusto)
    /// </summary>
    private void OcultarHighlightInventario()
    {
        if (InventoryUIManager.Instance != null && InventoryUIManager.Instance.highlightObject != null)
        {
            InventoryUIManager.Instance.highlightObject.SetActive(false);

            if (logsDetallados)
            {
                Debug.Log("[LoreManager] Highlight del inventario ocultado");
            }
        }
    }

    /// <summary>
    /// ✅ NUEVO: Método separado para reactivar highlight (más robusto)
    /// </summary>
    private void ReactivarHighlightInventario()
    {
        if (InventoryUIManager.Instance != null)
        {
            // Solo actualizar si el inventario está abierto
            InventoryUIManager.Instance.ActualizarHighlightPublico();

            if (logsDetallados)
            {
                Debug.Log("[LoreManager] Highlight del inventario reactivado");
            }
        }
    }

    /// <summary>
    /// Toggle de reproducción de audio (STOP y PLAY desde inicio)
    /// </summary>
    private void ToggleAudio()
    {
        if (itemActual == null || itemActual.audioLore == null || audioSource == null)
        {
            Debug.LogWarning("[LoreManager] No hay audio para reproducir");
            return;
        }

        // Si está sonando → DETENER (Stop)
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            audioPausado = true;

            if (logsDetallados)
            {
                Debug.Log("[LoreManager] Audio detenido");
            }
        }
        // Si NO está sonando → REPRODUCIR desde el inicio
        else
        {
            audioSource.clip = itemActual.audioLore;
            audioSource.Play();
            audioPausado = false;

            if (logsDetallados)
            {
                Debug.Log("[LoreManager] Audio reproduciendo desde el inicio");
            }
        }
    }

    /// <summary>
    /// Actualiza el texto del botón de audio (opcional)
    /// </summary>
    private void ActualizarTextoBotonAudio()
    {
        if (textoBotonAudio == null) return;

        if (audioSource != null && audioSource.isPlaying)
        {
            textoBotonAudio.text = "DETENER [ENTER]";
        }
        else
        {
            textoBotonAudio.text = "REPRODUCIR [ENTER]";
        }
    }

    /// <summary>
    /// Verifica si el panel está abierto
    /// </summary>
    public bool PanelEstaAbierto()
    {
        return panelAbierto;
    }

    /// <summary>
    /// ✅ NUEVO: Detiene el audio si está sonando (útil para transiciones de escena)
    /// </summary>
    public void DetenerAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            audioPausado = false;
        }
    }

    /// <summary>
    /// ✅ NUEVO: Obtiene el item actualmente mostrado (útil para debugging)
    /// </summary>
    public ItemData ObtenerItemActual()
    {
        return itemActual;
    }

    /// <summary>
    /// ✅ NUEVO: Verifica si hay audio reproduciéndose
    /// </summary>
    public bool AudioReproduciendose()
    {
        return audioSource != null && audioSource.isPlaying;
    }
}