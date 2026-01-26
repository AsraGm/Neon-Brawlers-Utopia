using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sistema de inspección 3D para items en LLAVES (estilo Valorant/Dead Space)
/// </summary>
public class ModeloInspector3D : MonoBehaviour
{
    #region Singleton
    public static ModeloInspector3D Instance { get; private set; }

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
    [SerializeField] private TextMeshProUGUI textoNombreItem;
    [SerializeField] private Button botonCerrar;

    [Header("=== CÁMARA 3D ===")]
    [SerializeField] private Camera camaraInspector;
    [SerializeField] private Transform puntoSpawnModelo;
    [SerializeField] private float distanciaCamara = 3f;

    [Header("=== ROTACIÓN CON MOUSE ===")]
    [SerializeField] private float sensibilidadRotacion = 200f;
    [SerializeField] private float suavizado = 10f;

    [Header("=== ZOOM ===")]
    [SerializeField] private float zoomMin = 1.5f;
    [SerializeField] private float zoomMax = 5f;
    [SerializeField] private float velocidadZoom = 2f;

    [Header("=== KEYBINDS ===")]
    [SerializeField] private KeyCode teclaCerrar = KeyCode.Escape;

    private GameObject modeloActual;
    private ItemData itemActual;
    private bool panelAbierto = false;

    // Rotación
    private Vector2 rotacionActual = Vector2.zero;
    private Vector2 rotacionObjetivo = Vector2.zero;

    // Zoom
    private float zoomActual;

    private void Start()
    {
        // Cerrar panel al inicio
        CerrarPanel();

        // Configurar botón
        if (botonCerrar != null)
            botonCerrar.onClick.AddListener(CerrarPanel);

        // Configurar cámara
        if (camaraInspector != null)
        {
            camaraInspector.enabled = false;
        }

        zoomActual = distanciaCamara;
    }

    private void Update()
    {
        if (!panelAbierto) return;

        // Cerrar con tecla
        if (Input.GetKeyDown(teclaCerrar))
        {
            CerrarPanel();
        }

        // Rotación con mouse (click izquierdo sostenido)
        if (Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X") * sensibilidadRotacion * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * sensibilidadRotacion * Time.deltaTime;

            rotacionObjetivo.x -= mouseY;
            rotacionObjetivo.y += mouseX;

            // Limitar rotación vertical
            rotacionObjetivo.x = Mathf.Clamp(rotacionObjetivo.x, -80f, 80f);
        }

        // Aplicar rotación suavizada
        rotacionActual = Vector2.Lerp(rotacionActual, rotacionObjetivo, Time.deltaTime * suavizado);

        if (modeloActual != null)
        {
            modeloActual.transform.rotation = Quaternion.Euler(rotacionActual.x, rotacionActual.y, 0);
        }

        // Zoom con scroll
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            zoomActual -= scroll * velocidadZoom;
            zoomActual = Mathf.Clamp(zoomActual, zoomMin, zoomMax);

            if (camaraInspector != null && puntoSpawnModelo != null)
            {
                camaraInspector.transform.position = puntoSpawnModelo.position + Vector3.back * zoomActual;
            }
        }
    }

    /// <summary>
    /// Abre el inspector 3D con un modelo
    /// </summary>
    public void AbrirInspector(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("[ModeloInspector3D] Item es null");
            return;
        }

        //if (item.modelo3D == null)
        //{
        //    Debug.LogWarning($"[ModeloInspector3D] Item '{item.nombreDisplay}' no tiene modelo 3D asignado");
        //    return;
        //}

        itemActual = item;
        panelAbierto = true;
        panelInspector.SetActive(true);

        // Mostrar nombre
        if (textoNombreItem != null)
        {
            textoNombreItem.text = item.nombreDisplay;
        }

        // OCULTAR HIGHLIGHT
        if (InventoryUIManager.Instance != null && InventoryUIManager.Instance.highlightObject != null)
        {
            InventoryUIManager.Instance.highlightObject.SetActive(false);
        }

        // Activar cámara 3D
        if (camaraInspector != null)
        {
            camaraInspector.enabled = true;
        }

        // Instanciar modelo
        if (puntoSpawnModelo != null)
        {
            // Destruir modelo anterior si existe
            if (modeloActual != null)
            {
                Destroy(modeloActual);
            }

            //modeloActual = Instantiate(item.modelo3D, puntoSpawnModelo.position, Quaternion.identity);
            //modeloActual.transform.SetParent(puntoSpawnModelo);

            // Resetear rotación y zoom
            rotacionActual = Vector2.zero;
            rotacionObjetivo = Vector2.zero;
            zoomActual = distanciaCamara;

            // Posicionar cámara
            camaraInspector.transform.position = puntoSpawnModelo.position + Vector3.back * zoomActual;
            camaraInspector.transform.LookAt(puntoSpawnModelo);
        }

        Debug.Log($"[ModeloInspector3D] Inspector abierto para: {item.nombreDisplay}");
    }

    public void CerrarPanel()
    {
        panelAbierto = false;
        panelInspector.SetActive(false);

        // Desactivar cámara
        if (camaraInspector != null)
        {
            camaraInspector.enabled = false;
        }

        // Destruir modelo
        if (modeloActual != null)
        {
            Destroy(modeloActual);
            modeloActual = null;
        }

        itemActual = null;

        // ✅ REACTIVAR HIGHLIGHT
        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.ActualizarHighlightPublico();
        }

        Debug.Log("[ModeloInspector3D] Inspector cerrado");
    }

    /// <summary>
    /// Verifica si el panel está abierto (para bloquear navegación)
    /// </summary>
    public bool PanelEstaAbierto()
    {
        return panelAbierto;
    }
}