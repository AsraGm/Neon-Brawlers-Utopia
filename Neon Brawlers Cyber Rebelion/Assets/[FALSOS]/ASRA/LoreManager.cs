using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        textoNombre.text = item.nombreDisplay;
        textoDescripcion.text = item.descripcionLore;

        // ✅ OCULTAR HIGHLIGHT
        if (InventoryUIManager.Instance != null && InventoryUIManager.Instance.highlightObject != null)
        {
            InventoryUIManager.Instance.highlightObject.SetActive(false);
        }

        // Reproducir audio automáticamente si existe
        if (item.audioLore != null && audioSource != null)
        {
            audioSource.clip = item.audioLore;
            audioSource.Play();
            audioPausado = false;
            Debug.Log($"[LoreManager] Reproduciendo audio: {item.audioLore.name}");
        }
        else
        {
            Debug.LogWarning($"[LoreManager] Item '{item.nombreDisplay}' no tiene audio asignado");
        }

        Debug.Log($"[LoreManager] Panel abierto para: {item.nombreDisplay}");
    }

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

        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.ActualizarHighlightPublico();
        }

        Debug.Log("[LoreManager] Panel cerrado");
    }

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
            audioPausado = true; // Usamos esta variable como "audio detenido"
            Debug.Log("[LoreManager] Audio detenido");
        }
        // Si NO está sonando → REPRODUCIR desde el inicio
        else
        {
            audioSource.clip = itemActual.audioLore;
            audioSource.Play();
            audioPausado = false;
            Debug.Log("[LoreManager] Audio reproduciendo desde el inicio");
        }
    }
    private void ActualizarTextoBotonAudio()
    {
        if (textoBotonAudio == null) return;

        if (audioSource.isPlaying)
        {
            textoBotonAudio.text = "DETENER [ENTER]";
        }
        else
        {
            textoBotonAudio.text = "REPRODUCIR [ENTER]";
        }
    }
    public bool PanelEstaAbierto()
    {
        return panelAbierto;
    }
}