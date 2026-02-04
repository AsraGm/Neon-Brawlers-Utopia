using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VERSIÓN MEJORADA - InspectSystem con mejor sincronización
/// Cambios principales:
/// - Mejor manejo de sincronización con InventoryUIManager
/// - Validaciones adicionales
/// - Fix en actualización de highlight al cerrar
/// </summary>
public class InspectSystem : MonoBehaviour
{
    #region Singleton
    public static InspectSystem Instance { get; private set; }

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
    [SerializeField] private GameObject panelInspector;
    [SerializeField] private RawImage imagenRender; // Donde se muestra el modelo
    [SerializeField] private TextMeshProUGUI textoNombreItem;
    [SerializeField] private Button botonCerrar;

    [Header("=== RENDER 3D ===")]
    [SerializeField] private Camera camaraRender;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private Transform puntoSpawn; // Donde aparece el modelo
    [SerializeField] private Light luzModelo; // Luz para iluminar el modelo

    [Header("=== ROTACIÓN ===")]
    [SerializeField] private float rotationSpeed = 100f;

    [Header("=== ZOOM ===")]
    [SerializeField] private float zoomMin = 2f;
    [SerializeField] private float zoomMax = 6f;
    [SerializeField] private float zoomSpeed = 2f;
    private float currentZoom = 4f;

    [Header("=== KEYBINDS ===")]
    [SerializeField] private KeyCode teclaCerrar = KeyCode.Escape;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool logsDetallados = false;

    // Variables internas
    private GameObject modeloActual;
    private ItemData itemActual;
    private bool panelAbierto = false;
    private Vector3 previousMousePosition;

    private void Start()
    {
        CerrarPanel();

        // Configurar botón
        if (botonCerrar != null)
            botonCerrar.onClick.AddListener(CerrarPanel);

        // Configurar RenderTexture en la imagen
        if (imagenRender != null && renderTexture != null)
        {
            imagenRender.texture = renderTexture;
        }

        // Desactivar cámara de render al inicio
        if (camaraRender != null)
        {
            camaraRender.enabled = false;
        }

        // Desactivar luz al inicio
        if (luzModelo != null)
        {
            luzModelo.enabled = false;
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

        if (panelInspector == null)
        {
            Debug.LogError("[InspectSystem] ❌ panelInspector no asignado");
            todoCorrecto = false;
        }

        if (imagenRender == null)
        {
            Debug.LogWarning("[InspectSystem] ⚠️ imagenRender no asignado");
            todoCorrecto = false;
        }

        if (camaraRender == null)
        {
            Debug.LogWarning("[InspectSystem] ⚠️ camaraRender no asignado");
            todoCorrecto = false;
        }

        if (renderTexture == null)
        {
            Debug.LogWarning("[InspectSystem] ⚠️ renderTexture no asignado");
            todoCorrecto = false;
        }

        if (puntoSpawn == null)
        {
            Debug.LogWarning("[InspectSystem] ⚠️ puntoSpawn no asignado");
            todoCorrecto = false;
        }

        if (todoCorrecto && logsDetallados)
        {
            Debug.Log("[InspectSystem] ✅ Todos los componentes asignados correctamente");
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

        // Rotación con mouse
        if (modeloActual != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                previousMousePosition = Input.mousePosition;
            }

            if (Input.GetMouseButton(0))
            {
                Vector3 deltaMousePosition = Input.mousePosition - previousMousePosition;
                float rotationX = deltaMousePosition.y * rotationSpeed * Time.deltaTime;
                float rotationY = -deltaMousePosition.x * rotationSpeed * Time.deltaTime;

                Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0f);
                modeloActual.transform.rotation = rotation * modeloActual.transform.rotation;

                previousMousePosition = Input.mousePosition;
            }
        }

        // Zoom con scroll
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f && camaraRender != null)
        {
            currentZoom -= scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, zoomMin, zoomMax);

            // Mover cámara hacia adelante/atrás
            camaraRender.transform.localPosition = new Vector3(0, 0, -currentZoom);
        }
    }

    /// <summary>
    /// Abre el inspector con un item del inventario
    /// ✅ MEJORADO: Mejor validación y manejo de errores
    /// </summary>
    public void AbrirInspector(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("[InspectSystem] Item es null");
            return;
        }

        if (item.modelo3D == null)
        {
            Debug.LogWarning($"[InspectSystem] Item '{item.nombreDisplay}' no tiene modelo 3D asignado");
            return;
        }

        itemActual = item;
        panelAbierto = true;
        panelInspector.SetActive(true);

        // Mostrar nombre
        if (textoNombreItem != null)
        {
            textoNombreItem.text = item.nombreDisplay;
        }

        // ✅ OCULTAR HIGHLIGHT
        OcultarHighlightInventario();

        // Activar cámara y luz
        if (camaraRender != null)
        {
            camaraRender.enabled = true;
        }
        if (luzModelo != null)
        {
            luzModelo.enabled = true;
        }

        // Instanciar modelo
        InstanciarModelo(item);

        if (logsDetallados)
        {
            Debug.Log($"[InspectSystem] Inspector abierto para: {item.nombreDisplay}");
        }
    }

    /// <summary>
    /// ✅ NUEVO: Método separado para instanciar el modelo (más limpio)
    /// </summary>
    private void InstanciarModelo(ItemData item)
    {
        if (puntoSpawn == null)
        {
            Debug.LogError("[InspectSystem] puntoSpawn no asignado, no se puede instanciar modelo");
            return;
        }

        // Destruir modelo anterior si existe
        if (modeloActual != null)
        {
            Destroy(modeloActual);
        }

        modeloActual = Instantiate(item.modelo3D, puntoSpawn.position, Quaternion.identity);
        modeloActual.transform.SetParent(puntoSpawn);
        modeloActual.transform.localPosition = Vector3.zero;
        modeloActual.transform.localRotation = Quaternion.identity;

        // Desactivar sombras (optimización para render texture)
        Renderer[] renderers = modeloActual.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // Resetear zoom
        currentZoom = 4f;
        if (camaraRender != null)
        {
            camaraRender.transform.localPosition = new Vector3(0, 0, -currentZoom);
        }
    }

    /// <summary>
    /// ✅ MEJORADO: Cierre del panel con mejor sincronización
    /// </summary>
    public void CerrarPanel()
    {
        panelAbierto = false;
        panelInspector.SetActive(false);

        // Desactivar cámara y luz
        if (camaraRender != null)
        {
            camaraRender.enabled = false;
        }
        if (luzModelo != null)
        {
            luzModelo.enabled = false;
        }

        // Destruir modelo
        if (modeloActual != null)
        {
            Destroy(modeloActual);
            modeloActual = null;
        }

        itemActual = null;

        // ✅ REACTIVAR HIGHLIGHT (solo si el inventario está abierto)
        ReactivarHighlightInventario();

        if (logsDetallados)
        {
            Debug.Log("[InspectSystem] Inspector cerrado");
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
                Debug.Log("[InspectSystem] Highlight del inventario ocultado");
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
            // El InventoryUIManager se encargará de mostrar/ocultar según corresponda
            InventoryUIManager.Instance.ActualizarHighlightPublico();

            if (logsDetallados)
            {
                Debug.Log("[InspectSystem] Highlight del inventario reactivado");
            }
        }
    }

    /// <summary>
    /// Verifica si el panel está abierto (para bloquear navegación)
    /// </summary>
    public bool PanelEstaAbierto()
    {
        return panelAbierto;
    }

    /// <summary>
    /// ✅ NUEVO: Método para cambiar configuración de zoom en runtime
    /// </summary>
    public void ConfigurarZoom(float min, float max, float velocidad)
    {
        zoomMin = min;
        zoomMax = max;
        zoomSpeed = velocidad;
        currentZoom = Mathf.Clamp(currentZoom, zoomMin, zoomMax);
    }

    /// <summary>
    /// ✅ NUEVO: Resetear zoom a valor por defecto
    /// </summary>
    public void ResetearZoom()
    {
        currentZoom = 4f;
        if (camaraRender != null)
        {
            camaraRender.transform.localPosition = new Vector3(0, 0, -currentZoom);
        }
    }
}