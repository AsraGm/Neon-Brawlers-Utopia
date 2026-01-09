using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArtGallery : MonoBehaviour
{
    [System.Serializable]
    public class ArtPiece
    {
        public string artName;
        public Sprite thumbnail;
        public Sprite fullImage;
        public string description;
    }

    [Header("UI References")]
    public GameObject artItemPrefab;
    public Transform contentParent;
    public ScrollRect scrollRect;

    [Header("Gallery Content")]
    public List<ArtPiece> artCollection = new List<ArtPiece>();

    [Header("Preview")]
    public GameObject previewPanel;
    public Image previewImage;
    public TextMeshProUGUI previewTitle;
    public TextMeshProUGUI previewDescription;
    public Button closePreviewButton;

    [Header("Scroll Settings")]
    [Range(0.1f, 100f)] public float scrollSensitivity = 20f;
    public float wheelScrollSpeed = 100f;

    [Header("Grid Settings")]
    public Vector2 cellSize = new Vector2(200, 500);
    public Vector2 spacing = new Vector2(90, 20);

    [Header("Preview Image Settings")]
    [Tooltip("Tipo de ajuste para la imagen del preview")]
    public Image.Type previewImageType = Image.Type.Simple;
    [Tooltip("Si está activado, preservará el aspect ratio de la imagen")]
    public bool preserveAspect = true;

    private List<GameObject> instantiatedItems = new List<GameObject>();
    private GridLayoutGroup gridLayout;

    void Start()
    {
        StartCoroutine(InitializeGallery());
    }

    IEnumerator InitializeGallery()
    {
        yield return null; // Esperar un frame
        SetupGallery();
        SetupPreviewPanel();
    }

    void SetupGallery()
    {
        if (contentParent == null)
        {
            Debug.LogError("ContentParent no está asignado!");
            return;
        }

        // Configurar ScrollRect
        SetupScrollRect();

        // Configurar GridLayout
        SetupGridLayout();

        // Crear los items
        CreateArtItems();
    }

    void SetupScrollRect()
    {
        if (scrollRect != null)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = scrollSensitivity;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;

            // Asegurar que el Viewport y Content estén correctamente configurados
            if (scrollRect.viewport == null)
            {
                Debug.LogWarning("ScrollRect no tiene Viewport asignado!");
            }

            if (scrollRect.content == null)
            {
                scrollRect.content = contentParent.GetComponent<RectTransform>();
            }
        }
    }

    void SetupGridLayout()
    {
        gridLayout = contentParent.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = contentParent.gameObject.AddComponent<GridLayoutGroup>();
        }

        // Configurar el GridLayout
        gridLayout.cellSize = cellSize;
        gridLayout.spacing = spacing;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.padding = new RectOffset(10, 10, 10, 10);
    }

    void CreateArtItems()
    {
        Debug.Log("=== INICIANDO CreateArtItems ===");

        // Limpiar items existentes
        ClearItems();

        if (artCollection.Count == 0)
        {
            Debug.LogError("artCollection está vacío! Agrega sprites en el Inspector.");
            return;
        }

        if (artItemPrefab == null)
        {
            Debug.LogError("artItemPrefab no está asignado!");
            return;
        }

        if (contentParent == null)
        {
            Debug.LogError("contentParent no está asignado!");
            return;
        }

        // Crear cada item
        for (int i = 0; i < artCollection.Count; i++)
        {
            // Crear el item
            GameObject newItem = Instantiate(artItemPrefab, contentParent);
            newItem.name = $"ArtItem_{i}_{artCollection[i].artName}";

            // Configurar el ArtItem script
            ArtItem artItemScript = newItem.GetComponent<ArtItem>();
            if (artItemScript != null)
            {
                artItemScript.Setup(artCollection[i], this, i);
            }

            instantiatedItems.Add(newItem);
        }

        // Forzar actualización del layout
        StartCoroutine(RefreshLayoutDelayed());
    }

    IEnumerator RefreshLayoutDelayed()
    {
        // Esperar que todos los items se instancien completamente
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // Configurar el Content RectTransform correctamente
        RectTransform contentRect = contentParent.GetComponent<RectTransform>();
        if (contentRect != null)
        {
            // Configuración similar a tu InventorySystem que funciona
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1f);
        }

        // Verificar que el GridLayout esté configurado correctamente
        if (gridLayout != null)
        {
            gridLayout.cellSize = cellSize;
            gridLayout.spacing = spacing;
        }

        // Forzar rebuild del layout
        Canvas.ForceUpdateCanvases();
        if (contentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        // Asegurar que el ScrollRect detecte el contenido
        if (scrollRect != null && contentRect != null)
        {
            scrollRect.content = contentRect;
        }

        // Debug final
        if (instantiatedItems.Count > 0)
        {
            LogLayoutDebugInfo();
        }
    }

    void ClearItems()
    {
        foreach (GameObject item in instantiatedItems)
        {
            if (item != null)
                DestroyImmediate(item);
        }
        instantiatedItems.Clear();
    }

    void SetupPreviewPanel()
    {
        if (previewPanel != null)
        {
            previewPanel.SetActive(false);
        }

        if (closePreviewButton != null)
        {
            closePreviewButton.onClick.RemoveAllListeners();
            closePreviewButton.onClick.AddListener(ClosePreview);
        }

        // Configurar la imagen del preview para que se ajuste correctamente
        SetupPreviewImageSettings();
    }

    void SetupPreviewImageSettings()
    {
        if (previewImage != null)
        {
            // Configurar el tipo de imagen y preserve aspect
            previewImage.type = previewImageType;
            previewImage.preserveAspect = preserveAspect;

            // Opcional: establecer color para debug
            previewImage.color = Color.white;
        }
    }

    public void OpenPreview(ArtPiece artPiece)
    {
        if (previewPanel != null)
        {
            previewPanel.SetActive(true);

            if (previewImage != null)
            {
                // Asignar el sprite
                previewImage.sprite = artPiece.fullImage;

                // Asegurar que la configuración esté correcta cada vez que se abre
                previewImage.type = previewImageType;
                previewImage.preserveAspect = preserveAspect;

                // Opcional: ajustar automáticamente el tipo basado en el sprite
                if (artPiece.fullImage != null)
                {
                    AutoConfigureImageSettings(artPiece.fullImage);
                }
            }

            if (previewTitle != null)
                previewTitle.text = artPiece.artName;

            if (previewDescription != null)
                previewDescription.text = artPiece.description;
        }
    }

    // Método para configurar automáticamente las opciones de imagen
    void AutoConfigureImageSettings(Sprite sprite)
    {
        if (previewImage == null || sprite == null) return;

        // Obtener las dimensiones del sprite y del contenedor
        Vector2 spriteSize = new Vector2(sprite.texture.width, sprite.texture.height);
        RectTransform imageRect = previewImage.GetComponent<RectTransform>();
        Vector2 containerSize = imageRect.rect.size;

        // Si el contenedor tiene tamaño 0, usar el sizeDelta
        if (containerSize.x == 0 || containerSize.y == 0)
        {
            containerSize = imageRect.sizeDelta;
        }

        // Calcular ratios
        float spriteRatio = spriteSize.x / spriteSize.y;
        float containerRatio = containerSize.x / containerSize.y;

        // Configurar el tipo de imagen basado en las dimensiones
        if (preserveAspect)
        {
            previewImage.type = Image.Type.Simple;
            previewImage.preserveAspect = true;
        }
        else
        {
            // Si no queremos preservar aspect, usar Stretch
            previewImage.type = Image.Type.Simple;
            previewImage.preserveAspect = false;
        }
    }

    public void ClosePreview()
    {
        if (previewPanel != null)
        {
            previewPanel.SetActive(false);
        }
    }

    void Update()
    {
        // Scroll con mouse wheel - CORREGIDO
        if (scrollRect != null && scrollRect.IsActive())
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                Vector2 scrollPosition = scrollRect.normalizedPosition;
                scrollPosition.y += scroll * (wheelScrollSpeed / 100f);
                scrollPosition.y = Mathf.Clamp01(scrollPosition.y);
                scrollRect.normalizedPosition = scrollPosition;
            }
        }

        // Cerrar preview con Escape
        if (Input.GetKeyDown(KeyCode.Escape) && previewPanel != null && previewPanel.activeInHierarchy)
        {
            ClosePreview();
        }
    }

    // Métodos públicos para modificar la galería
    public void AddArtPiece(ArtPiece newArt)
    {
        artCollection.Add(newArt);
        CreateArtItems();
    }

    public void RemoveArtPiece(int index)
    {
        if (index >= 0 && index < artCollection.Count)
        {
            artCollection.RemoveAt(index);
            CreateArtItems();
        }
    }

    // Método para refrescar el layout
    [ContextMenu("Refresh Layout")]
    public void RefreshLayout()
    {
        if (Application.isPlaying)
        {
            CreateArtItems();
        }
    }

    // Métodos para configurar la imagen del preview en tiempo de ejecución
    public void SetPreviewImageType(Image.Type newType)
    {
        previewImageType = newType;
        if (previewImage != null)
        {
            previewImage.type = newType;
        }
    }

    public void SetPreserveAspect(bool preserve)
    {
        preserveAspect = preserve;
        if (previewImage != null)
        {
            previewImage.preserveAspect = preserve;
        }
    }

    // Métodos para modificar el grid en tiempo de ejecución
    public void SetGridCellSize(Vector2 newSize)
    {
        cellSize = newSize;
        if (gridLayout != null)
        {
            gridLayout.cellSize = newSize;
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent.GetComponent<RectTransform>());
        }
    }

    public void SetGridSpacing(Vector2 newSpacing)
    {
        spacing = newSpacing;
        if (gridLayout != null)
        {
            gridLayout.spacing = newSpacing;
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent.GetComponent<RectTransform>());
        }
    }

    // MÉTODOS DE DEBUG
    [ContextMenu("Debug Layout Info")]
    public void LogLayoutDebugInfo()
    {
        Debug.Log("=== INFORMACIÓN DE DEBUG DEL LAYOUT ===");

        // Info del Content
        if (contentParent != null)
        {
            RectTransform contentRect = contentParent.GetComponent<RectTransform>();
            Debug.Log($"Content Size: {contentRect.rect.size}");
            Debug.Log($"Content Anchored Position: {contentRect.anchoredPosition}");
        }

        if (scrollRect != null)
        {
            Debug.Log($"ScrollRect Sensitivity: {scrollRect.scrollSensitivity}");
            Debug.Log($"ScrollRect Content: {(scrollRect.content != null ? "Asignado" : "NO ASIGNADO")}");
            Debug.Log($"ScrollRect Viewport: {(scrollRect.viewport != null ? "Asignado" : "NO ASIGNADO")}");
        }

        if (instantiatedItems.Count > 0)
        {
            GameObject firstItem = instantiatedItems[0];
            RectTransform itemRect = firstItem.GetComponent<RectTransform>();
        }
    }

    [ContextMenu("Fix Grid Layout NOW")]
    public void FixGridLayoutNow()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(ForceFixLayout());
        }
    }

    IEnumerator ForceFixLayout()
    {
        // Reconfigurar GridLayout
        if (gridLayout != null)
        {
            gridLayout.cellSize = cellSize;
            gridLayout.spacing = spacing;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
        }

        RectTransform contentRect = contentParent.GetComponent<RectTransform>();
        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1f);
        }

        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();
        if (contentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
        if (scrollRect != null && contentRect != null)
        {
            scrollRect.content = contentRect;
        }
        LogLayoutDebugInfo();
    }
}