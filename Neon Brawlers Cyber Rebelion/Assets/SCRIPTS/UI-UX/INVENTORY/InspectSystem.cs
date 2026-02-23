using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private RawImage imagenRender;
    [SerializeField] private TextMeshProUGUI textoNombreItem;
    [SerializeField] private Button botonCerrar;

    [Header("=== RENDER 3D ===")]
    [SerializeField] private Camera camaraRender;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private Transform puntoSpawn;
    [SerializeField] private Light luzModelo;

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

    private GameObject modeloActual;
    private ItemData itemActual;
    private bool panelAbierto = false;

    private void Start()
    {
        CerrarPanel();

        if (botonCerrar != null)
            botonCerrar.onClick.AddListener(CerrarPanel);

        if (imagenRender != null && renderTexture != null)
            imagenRender.texture = renderTexture;

        if (camaraRender != null)
            camaraRender.enabled = false;

        if (luzModelo != null)
            luzModelo.enabled = false;

        ValidarComponentes();
    }

    private void ValidarComponentes()
    {
        if (panelInspector == null) Debug.LogError("[InspectSystem] panelInspector no asignado");
        if (imagenRender == null) Debug.LogWarning("[InspectSystem] imagenRender no asignado");
        if (camaraRender == null) Debug.LogWarning("[InspectSystem] camaraRender no asignado");
        if (renderTexture == null) Debug.LogWarning("[InspectSystem] renderTexture no asignado");
        if (puntoSpawn == null) Debug.LogWarning("[InspectSystem] puntoSpawn no asignado");
    }

    private void Update()
    {
        if (!panelAbierto) return;

        if (Input.GetKeyDown(teclaCerrar))
            CerrarPanel();

        // ✅ ROTACIÓN: mantén Alt presionado y mueve el mouse
        if (modeloActual != null)
        {
            bool altPresionado = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

            if (altPresionado)
            {
                // Bloquear cursor para que no se salga de la pantalla al rotar
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                float rotX = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
                float rotY = -Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;

                Quaternion rotation = Quaternion.Euler(rotX, rotY, 0f);
                modeloActual.transform.rotation = rotation * modeloActual.transform.rotation;
            }
            else
            {
                // Restaurar cursor normal cuando no está rotando
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        // Zoom con scroll (sigue funcionando igual)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f && camaraRender != null)
        {
            currentZoom -= scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, zoomMin, zoomMax);
            camaraRender.transform.localPosition = new Vector3(0, 0, -currentZoom);
        }
    }

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

        if (textoNombreItem != null)
            textoNombreItem.text = item.nombreDisplay;

        OcultarHighlightInventario();

        if (camaraRender != null) camaraRender.enabled = true;
        if (luzModelo != null) luzModelo.enabled = true;

        InstanciarModelo(item);

        if (logsDetallados)
            Debug.Log($"[InspectSystem] Inspector abierto para: {item.nombreDisplay}");
    }

    private void InstanciarModelo(ItemData item)
    {
        if (puntoSpawn == null)
        {
            Debug.LogError("[InspectSystem] puntoSpawn no asignado");
            return;
        }

        if (modeloActual != null)
            Destroy(modeloActual);

        modeloActual = Instantiate(item.modelo3D, puntoSpawn.position, Quaternion.identity);
        modeloActual.transform.SetParent(puntoSpawn);
        modeloActual.transform.localPosition = Vector3.zero;
        modeloActual.transform.localRotation = Quaternion.identity;

        Renderer[] renderers = modeloActual.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        currentZoom = 4f;
        if (camaraRender != null)
            camaraRender.transform.localPosition = new Vector3(0, 0, -currentZoom);
    }

    public void CerrarPanel()
    {
        panelAbierto = false;

        // Restaurar cursor siempre al cerrar
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        panelInspector.SetActive(false);

        if (camaraRender != null) camaraRender.enabled = false;
        if (luzModelo != null) luzModelo.enabled = false;

        if (modeloActual != null)
        {
            Destroy(modeloActual);
            modeloActual = null;
        }

        itemActual = null;
        ReactivarHighlightInventario();

        if (logsDetallados)
            Debug.Log("[InspectSystem] Inspector cerrado");
    }

    private void OcultarHighlightInventario()
    {
        if (InventoryUIManager.Instance != null && InventoryUIManager.Instance.highlightObject != null)
            InventoryUIManager.Instance.highlightObject.SetActive(false);
    }

    private void ReactivarHighlightInventario()
    {
        if (InventoryUIManager.Instance != null)
            InventoryUIManager.Instance.ActualizarHighlightPublico();
    }

    public bool PanelEstaAbierto() => panelAbierto;

    public void ConfigurarZoom(float min, float max, float velocidad)
    {
        zoomMin = min;
        zoomMax = max;
        zoomSpeed = velocidad;
        currentZoom = Mathf.Clamp(currentZoom, zoomMin, zoomMax);
    }

    public void ResetearZoom()
    {
        currentZoom = 4f;
        if (camaraRender != null)
            camaraRender.transform.localPosition = new Vector3(0, 0, -currentZoom);
    }
}