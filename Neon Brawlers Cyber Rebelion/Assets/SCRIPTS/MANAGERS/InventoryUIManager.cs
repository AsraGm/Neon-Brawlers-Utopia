using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ✅ VERSIÓN CORREGIDA V2 - InventoryUIManager con cálculo de posición CORRECTO
/// 
/// FIX PRINCIPAL:
/// - Método de cálculo de posición del highlight simplificado y corregido
/// - Ahora usa TransformPoint para conversión directa de coordenadas
/// - Mucho más confiable y funciona en cualquier configuración de Canvas
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
    [Tooltip("⭐ IMPORTANTE: Contenedor SEPARADO para el highlight (NO debe ser hijo del Content con GridLayoutGroup)")]
    [SerializeField] private Transform highlightContainer;

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

    [Header("=== DEBUG ===")]
    [SerializeField] private bool mostrarDebugPosicion = false;

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

    // Navegación
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

    private bool juegoPausado = false; //variable por si se pausa el juego

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

        // ✅ NUEVO SISTEMA: Crear highlight en contenedor separado
        if (highlightObject == null)
            CrearHighlightMejorado();

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

    /// <summary>
    /// ✅ NUEVO SISTEMA MEJORADO: Crea el highlight en un contenedor SEPARADO
    /// Esto evita que el GridLayoutGroup lo trate como un slot más
    /// </summary>
    private void CrearHighlightMejorado()
    {
        // ⭐ VALIDACIÓN: Si no hay contenedor asignado, crearlo automáticamente
        if (highlightContainer == null)
        {
            Debug.LogWarning("[InventoryUI] ⚠️ No hay HighlightContainer asignado. Creando uno automáticamente...");

            // Crear contenedor como hijo directo del Canvas de inventario
            GameObject containerObj = new GameObject("HighlightContainer");
            containerObj.transform.SetParent(canvasInventario.transform, false);

            // Configurar RectTransform para que ocupe toda el área
            RectTransform containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;
            containerRect.anchoredPosition = Vector2.zero;

            highlightContainer = containerObj.transform;

            Debug.Log("[InventoryUI] ✅ HighlightContainer creado automáticamente");
        }

        // Crear el highlight como hijo del contenedor separado
        highlightObject = new GameObject("Highlight");
        highlightObject.transform.SetParent(highlightContainer, false);

        // Agregar imagen
        Image img = highlightObject.AddComponent<Image>();
        img.color = colorHighlight;
        img.raycastTarget = false; // ✅ No bloquear clicks

        // Configurar RectTransform
        RectTransform rt = highlightObject.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(tamañoSlot * escalaHighlight, tamañoSlot * escalaHighlight);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        highlightObject.SetActive(false);

        Debug.Log("[InventoryUI] ✅ Highlight creado en contenedor separado (NO afecta al GridLayoutGroup)");
    }
    #endregion

    #region Abrir/Cerrar Inventario
    private void Update()
    {
        //Evitar abrir el inventario si hay pausa
        if (juegoPausado) return;

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

        // ✅ No procesar inputs si hay paneles abiertos
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

            if (Input.GetKeyDown(teclaSeleccionar))
                SeleccionarItem();
        }
    }

    public void ActualizarHighlightPublico()
    {
        // ✅ Solo actualizar si el inventario está abierto
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
        yield return null; // Esperar un frame adicional para asegurar que el layout esté actualizado
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

        int nuevoIndice = indiceSeleccionado + direccion;

        if (direccion == 1 || direccion == -1)
        {
            int filaActual = indiceSeleccionado / columnasGrid;
            int nuevaFila = nuevoIndice / columnasGrid;

            if (filaActual != nuevaFila)
            {
                if (direccion == 1 && (indiceSeleccionado % columnasGrid == columnasGrid - 1 || indiceSeleccionado == slotsActuales.Count - 1))
                    return;

                if (direccion == -1 && indiceSeleccionado % columnasGrid == 0)
                    return;
            }
        }

        nuevoIndice = Mathf.Clamp(nuevoIndice, 0, slotsActuales.Count - 1);

        if (nuevoIndice != indiceSeleccionado)
        {
            indiceSeleccionado = nuevoIndice;
            tiempoUltimoInput = Time.time;

            StartCoroutine(ActualizarScrollYHighlight());
        }
    }

    private System.Collections.IEnumerator ActualizarScrollYHighlight()
    {
        yield return null;
        yield return null;
        HacerScrollAlSlot();
        ActualizarHighlight();
    }
    private System.Collections.IEnumerator ActualizarHighlightConDelayNavegacion()
    {
        yield return null;
        yield return null;
        ActualizarHighlight();
    }
    private void ActualizarHighlight()
    {
        if (tabActual == TabActual.MISION || slotsActuales.Count == 0)
        {
            highlightObject.SetActive(false);
            return;
        }

        // Asegurarse de que el índice es válido
        if (indiceSeleccionado < 0 || indiceSeleccionado >= slotsActuales.Count)
        {
            indiceSeleccionado = 0;
        }

        highlightObject.SetActive(true);

        GameObject slotSeleccionado = slotsActuales[indiceSeleccionado];
        RectTransform slotRect = slotSeleccionado.GetComponent<RectTransform>();
        RectTransform highlightRect = highlightObject.GetComponent<RectTransform>();

        // ⭐ MÉTODO SIMPLE: Hacer que el highlight sea hijo del slot temporalmente
        // y centrarlo con posición (0,0)

        highlightObject.transform.SetParent(slotSeleccionado.transform, false);
        highlightRect.anchoredPosition = Vector2.zero;
        highlightRect.anchorMin = new Vector2(0.5f, 0.5f);
        highlightRect.anchorMax = new Vector2(0.5f, 0.5f);
        highlightRect.pivot = new Vector2(0.5f, 0.5f);

        // Ajustar tamaño
        highlightRect.sizeDelta = slotRect.sizeDelta * escalaHighlight;

        // Escala
        highlightObject.transform.localScale = Vector3.one;

        // Asegurarse de que esté encima de todo dentro del slot
        highlightObject.transform.SetAsLastSibling();

        if (mostrarDebugPosicion)
        {
            Debug.Log($"[InventoryUI] Highlight actualizado en slot: {slotSeleccionado.name} (índice {indiceSeleccionado})");
        }
    }

    private void HacerScrollAlSlot()
    {
        if (scrollActual == null || slotsActuales.Count == 0) return;
        if (indiceSeleccionado >= slotsActuales.Count) return;

        scrollActual.StopMovement();

        RectTransform contentRect = scrollActual.content;
        RectTransform viewportRect = scrollActual.viewport;
        if (contentRect == null || viewportRect == null) return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        Canvas.ForceUpdateCanvases();

        GridLayoutGroup gridLayout = contentRect.GetComponent<GridLayoutGroup>();
        if (gridLayout == null) return;

        float alturaFila = gridLayout.cellSize.y + gridLayout.spacing.y;

        int totalFilas = Mathf.CeilToInt((float)slotsActuales.Count / columnasGrid);
        float contentHeightCalculada = totalFilas * alturaFila
                                        + gridLayout.padding.top
                                        + gridLayout.padding.bottom;

        float contentHeight = Mathf.Max(contentRect.rect.height, contentHeightCalculada);
        float viewportHeight = viewportRect.rect.height;

        // Si todo entra en el viewport, no hay nada que mover
        if (contentHeight <= viewportHeight)
        {
            contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, 0f);

            return;
        }

        int filaActual = indiceSeleccionado / columnasGrid;

        // ⭐ Posición objetivo EXACTA en píxeles: la fila sube (Content se mueve
        // hacia arriba en Y positivo, porque el pivot está arriba)
        float targetY = filaActual * alturaFila;

        // No dejar que se pase del final del contenido
        float maxScroll = contentHeight - viewportHeight;
        targetY = Mathf.Clamp(targetY, 0f, maxScroll);

        contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, targetY);

        Debug.Log($"[SCROLL-CHECK] Content='{contentRect.name}' (id={contentRect.GetInstanceID()}) posY_aplicado={targetY} posY_real_despues={contentRect.anchoredPosition.y}");
    }
    #endregion

        #region Selección de Item
    private void SeleccionarItem()
    {
        // LLAVES: Inspeccionar modelo 3D
        if (tabActual == TabActual.LLAVES)
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
        // BdD: Mostrar lore
        else if (tabActual == TabActual.BdD)
        {
            if (indiceSeleccionado < itemsBdD.Count)
            {
                ItemData item = itemsBdD[indiceSeleccionado];

                if (LoreManager.Instance != null)
                {
                    LoreManager.Instance.AbrirPanelLore(item);
                }
                else
                {
                    Debug.LogWarning("[InventoryUI] LoreManager no existe");
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

    public void QuitarItem(ItemData item)
    {
        if (item == null) return;

        if (itemsLLAVES.Contains(item))
        {
            itemsLLAVES.Remove(item);
            ActualizarVisualesLLAVES();
        }

        if (itemsBdD.Contains(item))
        {
            itemsBdD.Remove(item);
            ActualizarVisualesBdD();
        }

        Debug.Log($"[InventoryUI] Item removido: {item.itemID}");
    }

    /// <summary>
    /// Obtiene la lista de IDs de todos los items en el inventario
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
    /// Limpia todo el inventario
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
    /// Agrega un item por su ID desde el ItemDatabase
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

    public void QuitarItemPorID(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return;

        if (itemDatabase == null)
        {
            Debug.LogError("[InventoryUI] No hay ItemDatabase asignado.");
            return;
        }

        ItemData item = itemDatabase.ObtenerItemPorID(itemID);

        if (item != null)
            QuitarItem(item);
        else
            Debug.LogWarning($"[InventoryUI] Item '{itemID}' no encontrado en ItemDatabase");
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
                // Slot vacío
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
                // Slot vacío
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

        // Esperar un frame antes de actualizar highlight para asegurar que el layout esté listo
        StartCoroutine(ActualizarHighlightConDelayCorto());

        ActualizarEstadoBotones();

        Debug.Log($"[InventoryUI] Cambiado a tab: {tabActual}");
    }

    private System.Collections.IEnumerator ActualizarHighlightConDelayCorto()
    {
        yield return null;
        ActualizarHighlight();
        HacerScrollAlSlot();
    }

    /// <summary>
    /// Actualiza el color de los botones según el tab activo
    /// </summary>
    private void ActualizarEstadoBotones()
    {
        // ⭐ Actualizar botón LLAVES
        if (botonLLAVES != null)
        {
            TextMeshProUGUI texto = botonLLAVES.GetComponentInChildren<TextMeshProUGUI>();
            if (texto != null)
            {
                texto.fontStyle = tabActual == TabActual.LLAVES ? FontStyles.Bold : FontStyles.Normal;
            }

            botonLLAVES.transform.localScale = tabActual == TabActual.LLAVES ? Vector3.one * 1.1f : Vector3.one;

            ColorBlock colors = botonLLAVES.colors;
            colors.normalColor = tabActual == TabActual.LLAVES ? colorTabActivo : colorTabInactivo;
            botonLLAVES.colors = colors;
        }

        // ⭐ Actualizar botón MISIÓN
        if (botonMISION != null)
        {
            TextMeshProUGUI texto = botonMISION.GetComponentInChildren<TextMeshProUGUI>();
            if (texto != null)
            {
                texto.fontStyle = tabActual == TabActual.MISION ? FontStyles.Bold : FontStyles.Normal;
            }

            botonMISION.transform.localScale = tabActual == TabActual.MISION ? Vector3.one * 1.1f : Vector3.one; // ⭐ AÑADIDO

            ColorBlock colors = botonMISION.colors;
            colors.normalColor = tabActual == TabActual.MISION ? colorTabActivo : colorTabInactivo;
            botonMISION.colors = colors;
        }

        // ⭐ Actualizar botón BdD
        if (botonBdD != null)
        {
            TextMeshProUGUI texto = botonBdD.GetComponentInChildren<TextMeshProUGUI>();
            if (texto != null)
            {
                texto.fontStyle = tabActual == TabActual.BdD ? FontStyles.Bold : FontStyles.Normal;
            }

            botonBdD.transform.localScale = tabActual == TabActual.BdD ? Vector3.one * 1.1f : Vector3.one; // ⭐ AÑADIDO

            ColorBlock colors = botonBdD.colors;
            colors.normalColor = tabActual == TabActual.BdD ? colorTabActivo : colorTabInactivo;
            botonBdD.colors = colors;
        }
    }
    #endregion

    #region Reseteo
    public void LimpiarInventarioCompleto()
    {
        // Limpiar listas
        itemsLLAVES.Clear();
        itemsBdD.Clear();

        // Limpiar todas las referencias UI de LLAVES
        foreach (var slot in slotsLLAVES)
        {
            if (slot != null)
            {
                LimpiarSlot(slot.transform);
            }
        }

        // Limpiar todas las referencias UI de BdD
        foreach (var slot in slotsBdD)
        {
            if (slot != null)
            {
                LimpiarSlot(slot.transform);
            }
        }

        // Resetear índice
        indiceSeleccionado = 0;

        // Ocultar highlight
        if (highlightObject != null)
        {
            highlightObject.SetActive(false);
        }

        Debug.Log("[InventoryUIManager] 🎒 Inventario completamente limpiado");
    }
    private void LimpiarSlot(Transform slot)
    {
        Image imagen = slot.Find("Imagen")?.GetComponent<Image>();
        if (imagen != null)
        {
            imagen.sprite = null;
            imagen.enabled = false;
        }
    }
    #endregion

    //cerrar el inventario si se pausa el juego
    public void SetPausa(bool pausa)
    {
        juegoPausado = pausa;

        if (juegoPausado && inventarioAbierto)
        {
            CerrarInventario();
        }
    }
}