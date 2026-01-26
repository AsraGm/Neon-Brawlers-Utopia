using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// InventoryUIManager (Singleton Maestro)
/// Controla TODO el sistema de inventario estilo Dead Space
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

    [Header("=== HIGHLIGHT ===")]
    public GameObject highlightObject;
    [SerializeField] private Color colorHighlight = new Color(0, 1, 1, 0.3f);

    [Header("=== CONFIGURACIÓN GRID ===")]
    [SerializeField] private int columnasGrid = 5;
    [SerializeField] private float tamañoSlot = 80f;

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
    private List<GameObject> slotsActuales => tabActual == TabActual.LLAVES ? slotsLLAVES : slotsBdD;

    private enum TabActual { LLAVES, MISION, BdD }
    #endregion

    #region Inicialización
    private void Start()
    {
        // Obtener todos los slots existentes
        ObtenerSlots();

        // Crear highlight si no existe
        if (highlightObject == null)
            CrearHighlightPlaceholder();

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
        highlightObject.transform.SetParent(canvasInventario.transform);

        Image img = highlightObject.AddComponent<Image>();
        img.color = colorHighlight;

        RectTransform rt = highlightObject.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(tamañoSlot, tamañoSlot);

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

        if (LoreManager.Instance != null && LoreManager.Instance.PanelEstaAbierto())
        {
            return; // No permitir navegación ni cambio de tabs
        }

        // Cambiar de tab
        if (Input.GetKeyDown(teclaCambiarTab))
            CambiarTab();

        // Navegación (solo si NO estamos en MISIÓN)
        if (tabActual != TabActual.MISION)
        {
            if (Input.GetKeyDown(teclaArriba)) Navegar(-columnasGrid);
            if (Input.GetKeyDown(teclaAbajo)) Navegar(columnasGrid);
            if (Input.GetKeyDown(teclaIzquierda)) Navegar(-1);
            if (Input.GetKeyDown(teclaDerecha)) Navegar(1);

            if (Input.GetKeyDown(teclaSeleccionar))
                SeleccionarItem();
        }
    }
    public void ActualizarHighlightPublico()
    {
        ActualizarHighlight();
    }

    public void AbrirInventario()
    {
        inventarioAbierto = true;
        canvasInventario.SetActive(true);

        // Mostrar tab actual
        MostrarTab(tabActual);

        // Posicionar highlight
        ActualizarHighlight();

        Debug.Log("[InventoryUI] Inventario abierto");
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
        // Ciclar: LLAVES → MISION → BdD → LLAVES
        tabActual = (TabActual)(((int)tabActual + 1) % 3);

        MostrarTab(tabActual);
        indiceSeleccionado = 0; // Resetear selección
        ActualizarHighlight();

        Debug.Log($"[InventoryUI] Cambiado a tab: {tabActual}");
    }

    private void MostrarTab(TabActual tab)
    {
        // Ocultar todos
        panelLLAVES.SetActive(false);
        panelMISION.SetActive(false);
        panelBdD.SetActive(false);

        // Mostrar el activo
        switch (tab)
        {
            case TabActual.LLAVES:
                panelLLAVES.SetActive(true);
                break;
            case TabActual.MISION:
                panelMISION.SetActive(true);
                highlightObject.SetActive(false); // No hay highlight en misión
                break;
            case TabActual.BdD:
                panelBdD.SetActive(true);
                break;
        }
    }
    #endregion

    #region Sistema de Highlight y Navegación
    private void Navegar(int direccion)
    {
        int nuevoIndice = indiceSeleccionado + direccion;

        // Limitar dentro de rango
        if (nuevoIndice < 0) nuevoIndice = 0;
        if (nuevoIndice >= slotsActuales.Count) nuevoIndice = slotsActuales.Count - 1;

        indiceSeleccionado = nuevoIndice;
        ActualizarHighlight();
    }

    private void ActualizarHighlight()
    {
        if (tabActual == TabActual.MISION)
        {
            highlightObject.SetActive(false);
            return;
        }

        if (slotsActuales.Count == 0)
        {
            highlightObject.SetActive(false);
            return;
        }

        highlightObject.SetActive(true);

        // Posicionar sobre el slot seleccionado
        GameObject slotSeleccionado = slotsActuales[indiceSeleccionado];
        highlightObject.transform.position = slotSeleccionado.transform.position;
        highlightObject.transform.SetAsLastSibling(); // Dibujarse encima
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
                else
                {
                    Debug.LogWarning("[InventoryUI] LoreManager no existe");
                }
            }
        }
        // Si estamos en LLAVES, solo mostrar log (más adelante: inspección 3D)
        else if (tabActual == TabActual.LLAVES)
        {
            Debug.Log($"[InventoryUI] Item seleccionado en LLAVES, índice: {indiceSeleccionado}");
            // TODO: Abrir ventana de inspección 3D
        }
    }
    #endregion

    #region Agregar/Remover Items
    /// <summary>
    /// Agrega un item al inventario (llamar desde ItemRecolectable)
    /// </summary>
    public void AgregarItem(ItemData item)
    {
        if (item == null) return;

        // Agregar según tipo
        if (item.tipo == TipoItem.Item_Normal)
        {
            if (!itemsLLAVES.Contains(item))
            {
                itemsLLAVES.Add(item);
                ActualizarVisualesLLAVES();
            }
        }

        if (item.tipo == TipoItem.Item_Lore)
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
}