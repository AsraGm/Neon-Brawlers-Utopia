using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static ArtGallery;

public class ArtItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Components")]
    public Image thumbnailImage;          // Imagen del thumbnail
    public TextMeshProUGUI titleText;     // Texto del título (OCULTO - solo para referencia en Inspector)
    public Button itemButton;             // Botón del item

    [Header("Hover Effects")]
    public float hoverScale = 1.1f;       // Escala al hacer hover
    public float animationSpeed = 5f;     // Velocidad de la animación
    public Color hoverTint = Color.white; // Color al hacer hover

    private ArtPiece artData;
    private ArtGallery gallery;
    private int itemIndex;
    private Vector3 originalScale;
    private Color originalColor;
    private bool isHovering = false;

    void Start()
    {
        originalScale = transform.localScale;
        if (thumbnailImage != null)
        {
            originalColor = thumbnailImage.color;
        }

        // OCULTAR el título completamente si existe
        if (titleText != null)
        {
            titleText.gameObject.SetActive(false);
        }
    }

    public void Setup(ArtPiece artPiece, ArtGallery artGallery, int index)
    {
        artData = artPiece;
        gallery = artGallery;
        itemIndex = index;

        // Configurar la imagen thumbnail
        if (thumbnailImage != null && artPiece.thumbnail != null)
        {
            thumbnailImage.sprite = artPiece.thumbnail;
        }

        // NO configurar el título - lo mantenemos oculto para un diseño limpio
        if (titleText != null)
        {
            titleText.gameObject.SetActive(false);
        }

        // Configurar el botón
        if (itemButton != null)
        {
            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(OnItemClicked);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnItemClicked();
    }

    void OnItemClicked()
    {
        if (gallery != null && artData != null)
        {
            // Solo mostrar título en el preview, no en la galería
            gallery.OpenPreview(artData);
        }
    }

    void Update()
    {
        // Animación de hover
        if (isHovering)
        {
            // Escalar
            transform.localScale = Vector3.Lerp(transform.localScale,
                originalScale * hoverScale, Time.deltaTime * animationSpeed);

            // Cambiar color
            if (thumbnailImage != null)
            {
                thumbnailImage.color = Color.Lerp(thumbnailImage.color,
                    hoverTint, Time.deltaTime * animationSpeed);
            }
        }
        else
        {
            // Volver al tamaño original
            transform.localScale = Vector3.Lerp(transform.localScale,
                originalScale, Time.deltaTime * animationSpeed);

            // Volver al color original
            if (thumbnailImage != null)
            {
                thumbnailImage.color = Color.Lerp(thumbnailImage.color,
                    originalColor, Time.deltaTime * animationSpeed);
            }
        }
    }

    // Método para efectos adicionales (opcional)
    public void PlayClickAnimation()
    {
        // Aquí puedes agregar efectos como partículas, sonidos, etc.
        StartCoroutine(ClickAnimation());
    }

    private System.Collections.IEnumerator ClickAnimation()
    {
        Vector3 targetScale = originalScale * 0.95f;

        // Comprimir
        float timer = 0;
        while (timer < 0.1f)
        {
            transform.localScale = Vector3.Lerp(originalScale, targetScale, timer / 0.1f);
            timer += Time.deltaTime;
            yield return null;
        }

        // Expandir de vuelta
        timer = 0;
        while (timer < 0.1f)
        {
            transform.localScale = Vector3.Lerp(targetScale, originalScale, timer / 0.1f);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }
}