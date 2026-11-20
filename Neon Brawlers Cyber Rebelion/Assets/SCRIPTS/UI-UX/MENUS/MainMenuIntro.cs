using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuIntro : MonoBehaviour
{
    [Header("Imagen del main menu")]
    [SerializeField] private RectTransform menuImage;

    [Header("Estado inicial (zoom)")]
    [SerializeField] private Vector3 startScale = new Vector3(6f, 6f, 6f);
    // Valores como se ven en el Inspector (Left, Right, Top, Bottom)
    [SerializeField] private float left = 1151f;
    [SerializeField] private float right = -1151f;
    [SerializeField] private float top = 868f;
    [SerializeField] private float bottom = -868f;

    [Header("Tiempos")]
    [SerializeField] private float holdTime = 3f;
    [SerializeField] private float zoomOutDuration = 2f;
    [SerializeField] private float buttonsFadeDuration = 1.5f;

    [Header("Audio (un solo AudioSource, Play On Awake desactivado)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip zoomOutClip;
    [SerializeField] private AudioClip menuMusicClip;

    [Header("Botones")]
    [SerializeField] private Image[] buttonImages;   // las imágenes de los botones
    [SerializeField] private Graphic[] buttonTexts;  // los textos (Text o TextMeshPro)
    [SerializeField] private Button[] buttons;       // opcional: para bloquear clics durante la intro

    private void Awake()
    {
        menuImage.localScale = startScale;
        // Inspector: Left=offsetMin.x, Right=-offsetMax.x, Top=-offsetMax.y, Bottom=offsetMin.y
        menuImage.offsetMin = new Vector2(left, bottom);
        menuImage.offsetMax = new Vector2(-right, -top);

        SetButtonsAlpha(0f);
        SetButtonsInteractable(false);
    }

    private void Start()
    {
        StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        // 1. Mantener el zoom
        yield return new WaitForSecondsRealtime(holdTime);

        // 2. Zoom out + sonido
        if (audioSource != null && zoomOutClip != null)
            audioSource.PlayOneShot(zoomOutClip);

        Vector2 minStart = menuImage.offsetMin;
        Vector2 maxStart = menuImage.offsetMax;
        Vector3 scaleStart = menuImage.localScale;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / zoomOutDuration;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

            menuImage.localScale = Vector3.Lerp(scaleStart, Vector3.one, k);
            menuImage.offsetMin = Vector2.Lerp(minStart, Vector2.zero, k);
            menuImage.offsetMax = Vector2.Lerp(maxStart, Vector2.zero, k);
            yield return null;
        }

        menuImage.localScale = Vector3.one;
        menuImage.offsetMin = Vector2.zero;
        menuImage.offsetMax = Vector2.zero;

        // 3. Música del menú principal
        if (audioSource != null && menuMusicClip != null)
        {
            audioSource.clip = menuMusicClip;
            audioSource.loop = true;
            audioSource.Play();
        }

        // 4. Fade in de imágenes y textos al mismo tiempo
        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / buttonsFadeDuration;
            SetButtonsAlpha(Mathf.Clamp01(t));
            yield return null;
        }

        SetButtonsAlpha(1f);
        SetButtonsInteractable(true);
    }

    private void SetButtonsAlpha(float a)
    {
        foreach (var img in buttonImages)
        {
            if (img == null) continue;
            Color c = img.color;
            c.a = a;
            img.color = c;
        }

        foreach (var txt in buttonTexts)
        {
            if (txt == null) continue;
            Color c = txt.color;
            c.a = a;
            txt.color = c;
        }
    }

    private void SetButtonsInteractable(bool value)
    {
        foreach (var b in buttons)
        {
            if (b != null) b.interactable = value;
        }
    }
}