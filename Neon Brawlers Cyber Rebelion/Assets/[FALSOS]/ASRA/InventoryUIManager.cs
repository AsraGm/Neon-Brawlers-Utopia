using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VERSIÓN MEJORADA - InventoryUIManager con todas las correcciones
/// Cambios principales:
/// - Métodos ObtenerItemsIDs(), LimpiarInventario(), AgregarItemPorID() implementados
/// - Sistema de ItemDatabase para cargar items por ID
/// - Fix en navegación del tab MISIÓN
/// - Sincronización mejorada con LoreManager/InspectSystem
/// - Validaciones adicionales
/// </summary>
public class InventoryUIManager : MonoBehaviour
{
    #region Singleton
    public static InventoryUIManager Instance { get; private set; }

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

    #region Referencias UI
    [Header("=== PANELES ===")]
    [SerializeField] private GameObject panelLLAVES;
    [SerializeField] private GameObject panelMISION;
    [SerializeField] private GameObject panelBdD;
    [SerializeField] private GameObject canvasInventario; // INVENTORY completo

    [Header("=== CONTENEDORES DE SLOTS ===")]
    [Tooltip("Content del ScrollView de LLAVES")]
    [SerializeField] private Transform contentLLAVES;

    [Tooltip("Content del ScrollView de BdD")]
    [SerializeField] private Transform contentBdD;

    [Header("=== SCROLL VIEWS ===")]
    [Tooltip("ScrollRect del panel LLAVES")]
    [SerializeField] private ScrollRect scrollRectLLAVES;

    [Tooltip("ScrollRect del panel BdD")]
    [SerializeField] private ScrollRect scrollRectBdD;

    [Header("=== HIGHLIGHT ===")]
    public GameObject highlightObject;
    [SerializeField] private Color colorHighlight = new Color(0, 1, 1, 0.3f);
    [SerializeField] private float escalaHighlight = 1.1f;

    [Header("=== BOTONES TABS ===")]
    [SerializeField] private Button botonLLAVES;
    [SerializeField] private Button botonMISION;
    [SerializeField] private Button botonBdD;

    [Header("=== COLORES TABS ===")]
    [SerializeField] private Color colorTabActivo = new Color(0, 1, 1, 1); // Cyan
    [SerializeField] private Color colorTabInactivo = new Color(0.5f, 0.5f, 0.5f, 1); // Gris

    [Header("=== CONFIGURACIÓN GRID ===")]
    [SerializeField] private int columnasGrid = 5;
    [SerializeField] private float tamañoSlot = 80f;

    [Header("=== ITEM DATABASE ===")]
    [Tooltip("ScriptableObject que contiene TODOS los items del juego")]
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("=== KEYBINDS ===")]
    public KeyCode teclaAbrir = KeyCode.I;
    public KeyCode teclaCambiarTab = KeyCode.Tab;
    public KeyCode teclaSeleccionar = KeyCode.Return;
    public KeyCode teclaArriba = KeyCode.UpArrow;
    public KeyCode teclaAbajo = KeyCode.DownArrow;
    public KeyCode teclaIzquierda = KeyCode.LeftArrow;
    public KeyCode teclaDerecha = KeyCode.RightArrow;
    #endregion

    #region Variables Internas
    private bool inventarioAbierto = false;
    private TabActual tabActual = TabActual.LLAVES;

    // Listas de items recolectados
    private List<ItemData> itemsLLAVES = new List<ItemData>();
    private List<ItemData> itemsBdD = new List<ItemData>();

    // Slots (referencias a los GameObjects SLOTS en el Content)
    private List<GameObject> slotsLLAVES = new List<GameObject>();
    private List<GameObject> slotsBdD = new List<GameObject>();

    // Navegación - ✅ FIX: Ahora maneja correctamente el tab MISIÓN
    private int indiceSeleccionado = 0;
    private List<GameObject> slotsActuales
    {
        get
        {
            switch (tabActual)
            {
                case TabActual.LLAVES:
                    return slotsLLAVES;
                case TabActual.BdD:
                    return slotsBdD;
                case TabActual.MISION:
                default:
                    return new List<GameObject>(); // Lista vacía para MISIÓN (sin navegación)
            }
        }
    }

    private ScrollRect scrollActual
    {
        get
        {
            switch (tabActual)
            {
                case TabActual.LLAVES:
                    return scrollRectLLAVES;
                case TabActual.BdD:
                    return scrollRectBdD;
                default:
                    return null;
            }
        }
    }
    private Transform contentActual
    {
        get
        {
            switch (tabActual)
            {
                case TabActual.LLAVES:
                    return contentLLAVES;
                case TabActual.BdD:
                    return contentBdD;
                default:
                    return null;
            }
        }
    }

    private float tiempoUltimoInput = 0f;
    private float cooldownInput = 0.15f; // Tiempo mínimo entre inputs

    private enum TabActual { LLAVES, MISION, BdD }
    #endregion

    #region Inicialización
    private void Start()
    {
        if (itemDatabase == null)
        {
            Debug.LogWarning("[InventoryUI] ⚠️ ItemDatabase no asignado. El sistema de guardado/carga no funcionará correctamente. Por favor asigna un ItemDatabase en el Inspector.");
        }

        // Obtener todos los slots existentes
        ObtenerSlots();

        // Crear highlight si no existe
        if (highlightObject == null)
            CrearHighlightPlaceholder();

        ConfigurarBotonesTabs();

        // Cerrar inventario al inicio
        CerrarInventario();

        Debug.Log("[InventoryUI] Sistema inicializado");
    }

    private void ObtenerSlots()
    {
        // Obtener slots de LLAVES
        foreach (Transform child in contentLLAVES)
        {
            slotsLLAVES.Add(child.gameObject);
        }

        // Obtener slots de BdD
        foreach (Transform child in contentBdD)
        {
            slotsBdD.Add(child.gameObject);
        }

        Debug.Log($"[InventoryUI] {slotsLLAVES.Count} slots LLAVES, {slotsBdD.Count} slots BdD");
    }

    private void CrearHighlightPlaceholder()
    {
        highlightObject = new GameObject("Highlight");

        highlightObject.transform.SetParent(contentLLAVES, false);

        Image img = highlightObject.AddComponent<Image>();
        img.color = colorHighlight;
        img.raycastTarget = false; // ✅ No bloquear clicks

        RectTransform rt = highlightObject.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(escalaHighlight, escalaHighlight);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);

        highlightObject.SetActive(false);
    }
    #endregion

    #region Abrir/Cerrar Inventario
    private void Update()
    {
        // Abrir/Cerrar inventario
        if (Input.GetKeyDown(teclaAbrir))
        {
            if (inventarioAbierto)
                CerrarInventario();
            else
                AbrirInventario();
        }

        // Solo procesar inputs si está abierto
        if (!inventarioAbierto) return;

        // ✅ MEJORADO: No procesar inputs si hay paneles abiertos
        if ((LoreManager.Instance != null && LoreManager.Instance.PanelEstaAbierto()) ||
         (InspectSystem.Instance != null && InspectSystem.Instance.PanelEstaAbierto()))
        {
            return;
        }

        // Cambiar de tab
        if (Input.GetKeyDown(teclaCambiarTab))
            CambiarTab();

        if (Time.time - tiempoUltimoInput < cooldownInput)
            return;

        // Navegación (solo si NO estamos en MISIÓN)
        if (tabActual != TabActual.MISION)
        {
            bool inputDetectado = false;

            if (Input.GetKeyDown(teclaArriba))
            {
                Navegar(-columnasGrid);
                inputDetectado = true;
            }
            else if (Input.GetKeyDown(teclaAbajo))
            {
                Navegar(columnasGrid);
                inputDetectado = true;
            }
            else if (Input.GetKeyDown(teclaIzquierda))
            {
                Navegar(-1);
                inputDetectado = true;
            }
            else if (Input.GetKeyDown(teclaDerecha))
            {
                Navegar(1);
                inputDetectado = true;
            }

            if (inputDetectado)
                tiempoUltimoInput = Time.time;

            if (Input.GetKeyDown(teclaSeleccionar))
                SeleccionarItem();
        }
    }

    public void ActualizarHighlightPublico()
    {
        // ✅ NUEVO: Solo actualizar si el inventario está abierto
        if (inventarioAbierto)
        {
            ActualizarHighlight();
        }
    }

    public void AbrirInventario()
    {
        inventarioAbierto = true;
        canvasInventario.SetActive(true);

        // Mostrar tab actual
        MostrarTab(tabActual);

        indiceSeleccionado = 0; // Resetear al abrir

        StartCoroutine(ActualizarHighlightConDelay());

        ActualizarEstadoBotones();

        Debug.Log("[InventoryUI] Inventario abierto");
    }

    private System.Collections.IEnumerator ActualizarHighlightConDelay()
    {
        yield return null; // Esperar 1 frame
        ActualizarHighlight();
        HacerScrollAlSlot();
    }

    public void CerrarInventario()
    {
        inventarioAbierto = false;
        canvasInventario.SetActive(false);
        highlightObject.SetActive(false);

        Debug.Log("[InventoryUI] Inventario cerrado");
    }
    #endregion

    #region Sistema de Tabs
    private void CambiarTab()
    {
        // Ciclar entre tabs: LLAVES -> MISIÓN -> BdD -> LLAVES
        switch (tabActual)
        {
            case TabActual.LLAVES:
                CambiarATab(TabActual.MISION);
                break;
            case TabActual.MISION:
                CambiarATab(TabActual.BdD);
                break;
            case TabActual.BdD:
                CambiarATab(TabActual.LLAVES);
                break;
        }
    }

    private void MostrarTab(TabActual tab)
    {
        // Ocultar todos
        panelLLAVES.SetActive(false);
        panelMISION.SetActive(false);
        panelBdD.SetActive(false);

        // Mostrar solo el activo
        switch (tab)
        {
            case TabActual.LLAVES:
                panelLLAVES.SetActive(true);
                break;
            case TabActual.MISION:
                panelMISION.SetActive(true);
                highlightObject.SetActive(false); // No hay navegación en MISIÓN
                break;
            case TabActual.BdD:
                panelBdD.SetActive(true);
                break;
        }
    }
    #endregion

    #region Navegación
    private void Navegar(int direccion)
    {
        if (slotsActuales.Count == 0) return;

        indiceSeleccionado += direccion;

        // Wrap-around
        if (indiceSeleccionado < 0)
            indiceSeleccionado = slotsActuales.Count - 1;
        else if (indiceSeleccionado >= slotsActuales.Count)
            indiceSeleccionado = 0;

        ActualizarHighlight();
        HacerScrollAlSlot();
    }

    private void ActualizarHighlight()
    {
        if (tabActual == TabActual.MISION || slotsActuales.Count == 0)
        {
            highlightObject.SetActive(false);
            return;
        }

        if (highlightObject.transform.parent != contentActual)
        {
            highlightObject.transform.SetParent(contentActual, false);
        }

        highlightObject.SetActive(true);

        GameObject slotSeleccionado = slotsActuales[indiceSeleccionado];
        RectTransform slotRect = slotSeleccionado.GetComponent<RectTransform>();
        RectTransform highlightRect = highlightObject.GetComponent<RectTransform>();

        highlightRect.anchoredPosition = slotRect.anchoredPosition;
        highlightObject.transform.SetAsLastSibling(); // Dibujarse encima
        highlightObject.transform.localScale = Vector3.one * escalaHighlight;
    }

    private void HacerScrollAlSlot()
    {
        if (scrollActual == null || slotsActuales.Count == 0) return;
        if (indiceSeleccionado >= slotsActuales.Count) return;

        // Forzar actualización de layout
        Canvas.ForceUpdateCanvases();

        GameObject slotSeleccionado = slotsActuales[indiceSeleccionado];
        RectTransform slotRect = slotSeleccionado.GetComponent<RectTransform>();
        RectTransform contentRect = scrollActual.content;
        RectTransform viewportRect = scrollActual.viewport;

        if (slotRect == null || contentRect == null || viewportRect == null) return;

        int filaActual = indiceSeleccionado / columnasGrid;

        float alturaFila = slotRect.rect.height +
                           contentRect.GetComponent<GridLayoutGroup>().spacing.y;

        float targetYPos = filaActual * alturaFila;

        // Obtener dimensiones
        float viewportHeight = viewportRect.rect.height;
        float contentHeight = contentRect.rect.height;
        float maxScroll = contentHeight - viewportHeight;

        if (maxScroll > 0)
        {
            float normalizedPos = Mathf.Clamp01(targetYPos / maxScroll);

            // Unity usa 1 = arriba, 0 = abajo
            scrollActual.verticalNormalizedPosition = 1f - normalizedPos;
        }
    }

    private void SeleccionarItem()
    {
        if (slotsActuales.Count == 0 || indiceSeleccionado >= slotsActuales.Count)
            return;

        // Si estamos en BdD, abrir panel de lore
        if (tabActual == TabActual.BdD)
        {
            if (indiceSeleccionado < itemsBdD.Count)
            {
                ItemData item = itemsBdD[indiceSeleccionado];

                if (LoreManager.Instance != null)
                {
                    LoreManager.Instance.AbrirPanelLore(item);
                }
            }
        }

        else if (tabActual == TabActual.LLAVES)
        {
            if (indiceSeleccionado < itemsLLAVES.Count)
            {
                ItemData item = itemsLLAVES[indiceSeleccionado];

                if (InspectSystem.Instance != null)
                {
                    InspectSystem.Instance.AbrirInspector(item);
                }
                else
                {
                    Debug.LogWarning("[InventoryUI] InspectSystem no existe");
                }
            }
        }
    }
    #endregion

    #region Agregar/Remover Items
    /// <summary>
    /// Agrega un item al inventario (llamar desde ItemRecolectable)
    /// </summary>
    public void AgregarItem(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("[InventoryUI] Intentando agregar item null");
            return;
        }

        // Agregar según tipo
        if (item.tipo == TipoItem.ItemNormal)
        {
            if (!itemsLLAVES.Contains(item))
            {
                itemsLLAVES.Add(item);
                ActualizarVisualesLLAVES();
            }
        }

        if (item.tipo == TipoItem.ItemLore)
        {
            if (!itemsBdD.Contains(item))
            {
                itemsBdD.Add(item);
                ActualizarVisualesBdD();
            }
        }

        Debug.Log($"[InventoryUI] Item agregado: {item.itemID}");

        // Notificar al ObjetivoManager
        if (ObjetivoManager.Instance != null)
            ObjetivoManager.Instance.ItemRecolectado(item.itemID);
    }

    /// <summary>
    /// ✅ NUEVO: Obtiene la lista de IDs de todos los items en el inventario
    /// Usado por GameManager para guardar checkpoints
    /// </summary>
    public List<string> ObtenerItemsIDs()
    {
        List<string> ids = new List<string>();

        // Agregar items de LLAVES
        foreach (var item in itemsLLAVES)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemID))
            {
                ids.Add(item.itemID);
            }
        }

        // Agregar items de BdD (evitar duplicados)
        foreach (var item in itemsBdD)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemID) && !ids.Contains(item.itemID))
            {
                ids.Add(item.itemID);
            }
        }

        return ids;
    }

    /// <summary>
    /// ✅ NUEVO: Limpia todo el inventario
    /// Usado por GameManager antes de cargar checkpoint
    /// </summary>
    public void LimpiarInventario()
    {
        itemsLLAVES.Clear();
        itemsBdD.Clear();
        ActualizarVisualesLLAVES();
        ActualizarVisualesBdD();

        Debug.Log("[InventoryUI] Inventario limpiado");
    }

    /// <summary>
    /// ✅ NUEVO: Agrega un item por su ID desde el ItemDatabase
    /// Usado por GameManager al cargar checkpoint
    /// </summary>
    public void AgregarItemPorID(string itemID)
    {
        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogWarning("[InventoryUI] Intentando agregar item con ID vacío");
            return;
        }

        if (itemDatabase == null)
        {
            Debug.LogError("[InventoryUI] ❌ No hay ItemDatabase asignado. No se puede cargar el item: " + itemID);
            return;
        }

        ItemData item = itemDatabase.ObtenerItemPorID(itemID);

        if (item != null)
        {
            AgregarItem(item);
        }
        else
        {
            Debug.LogWarning($"[InventoryUI] Item con ID '{itemID}' no encontrado en ItemDatabase");
        }
    }

    private void ActualizarVisualesLLAVES()
    {
        // Asignar sprites de items recolectados
        for (int i = 0; i < slotsLLAVES.Count; i++)
        {
            Image img = slotsLLAVES[i].GetComponent<Image>();
            if (img == null) continue;

            // Si hay un item para este slot, mostrarlo
            if (i < itemsLLAVES.Count && itemsLLAVES[i].iconoItem != null)
            {
                img.sprite = itemsLLAVES[i].iconoItem;
                img.color = Color.white; // Visible
            }
            else
            {
                // Slot vacío - mantener el fondo visible pero sin sprite
                img.sprite = null;
                img.color = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Gris semi-transparente
            }
        }
    }

    private void ActualizarVisualesBdD()
    {
        // Asignar sprites de items recolectados
        for (int i = 0; i < slotsBdD.Count; i++)
        {
            Image img = slotsBdD[i].GetComponent<Image>();
            if (img == null) continue;

            // Si hay un item para este slot, mostrarlo
            if (i < itemsBdD.Count && itemsBdD[i].iconoItem != null)
            {
                img.sprite = itemsBdD[i].iconoItem;
                img.color = Color.white; // Visible
            }
            else
            {
                // Slot vacío - mantener el fondo visible pero sin sprite
                img.sprite = null;
                img.color = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Gris semi-transparente
            }
        }
    }

    public bool TieneItem(string itemID)
    {
        return itemsLLAVES.Any(item => item.itemID == itemID) ||
               itemsBdD.Any(item => item.itemID == itemID);
    }
    #endregion

    #region Sistema de Botones Tabs
    /// <summary>
    /// Configura los listeners de los botones de tabs
    /// </summary>
    private void ConfigurarBotonesTabs()
    {
        if (botonLLAVES != null)
        {
            botonLLAVES.onClick.AddListener(() => CambiarATab(TabActual.LLAVES));
        }

        if (botonMISION != null)
        {
            botonMISION.onClick.AddListener(() => CambiarATab(TabActual.MISION));
        }

        if (botonBdD != null)
        {
            botonBdD.onClick.AddListener(() => CambiarATab(TabActual.BdD));
        }
    }

    /// <summary>
    /// Cambia a un tab específico (llamado por botones o teclado)
    /// </summary>
    private void CambiarATab(TabActual nuevoTab)
    {
        tabActual = nuevoTab;
        MostrarTab(tabActual);
        indiceSeleccionado = 0; // Resetear selección
        ActualizarHighlight();
        HacerScrollAlSlot();
        ActualizarEstadoBotones();

        Debug.Log($"[InventoryUI] Cambiado a tab: {tabActual}");
    }

    /// <summary>
    /// Actualiza el color de los botones según el tab activo
    /// </summary>
    private void ActualizarEstadoBotones()
    {
        TextMeshProUGUI texto = botonLLAVES.GetComponentInChildren<TextMeshProUGUI>();
        if (texto != null)
        {
            texto.fontStyle = tabActual == TabActual.LLAVES ? FontStyles.Bold : FontStyles.Normal;
        }

        botonLLAVES.transform.localScale = tabActual == TabActual.LLAVES ? Vector3.one * 1.1f : Vector3.one;
        // Restablecer todos a inactivo
        if (botonLLAVES != null)
        {
            ColorBlock colors = botonLLAVES.colors;
            colors.normalColor = tabActual == TabActual.LLAVES ? colorTabActivo : colorTabInactivo;
            botonLLAVES.colors = colors;
        }

        if (botonMISION != null)
        {
            ColorBlock colors = botonMISION.colors;
            colors.normalColor = tabActual == TabActual.MISION ? colorTabActivo : colorTabInactivo;
            botonMISION.colors = colors;
        }

        if (botonBdD != null)
        {
            ColorBlock colors = botonBdD.colors;
            colors.normalColor = tabActual == TabActual.BdD ? colorTabActivo : colorTabInactivo;
            botonBdD.colors = colors;
        }
    }
    #endregion
}