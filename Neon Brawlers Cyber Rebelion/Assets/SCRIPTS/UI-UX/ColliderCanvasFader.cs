using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ColliderCanvasFader : MonoBehaviour
{
    [Header("Filtro de entrada")]
    [Tooltip("Tag que debe tener el objeto que entra al trigger (ej: Player). Dejar vacio para no filtrar por tag.")]
    [SerializeField] private string requiredTag = "Player";

    [Header("Referencias del Canvas")]
    [Tooltip("RectTransform de la 'carpeta' del panel que se va a mostrar con fade (ej: el objeto padre que agrupa todo ese panel en la jerarquia).")]
    [SerializeField] private RectTransform panel;

    [Tooltip("Boton que aparece luego del tiempo de espera para poder cerrar el panel.")]
    [SerializeField] private Button closeButton;

    // CanvasGroup obtenido (o agregado) automaticamente desde el RectTransform del panel.
    private CanvasGroup canvasGroup;

    [Header("Tiempos")]
    [Tooltip("Duracion del fade in (segundos).")]
    [SerializeField] private float fadeInDuration = 0.5f;

    [Tooltip("Tiempo de espera despues del fade in antes de mostrar el boton de cerrar.")]
    [SerializeField] private float timeBeforeButton = 3f;

    [Tooltip("Duracion del fade out al cerrar el panel.")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Comportamiento")]
    [Tooltip("Si esta activo, el trigger solo se puede activar una vez.")]
    [SerializeField] private bool activateOnlyOnce = false;

    [Tooltip("Si esta activo, al hacer fade out tambien se desactiva el GameObject del canvas (SetActive false).")]
    [SerializeField] private bool disableCanvasObjectOnClose = true;

    [Tooltip("Si esta activo, al abrirse el panel se pausa el juego (Time.timeScale = 0) y se reanuda (Time.timeScale = 1) apenas se activa el boton de cerrar.")]
    [SerializeField] private bool pauseTimeWhileOpen = true;

    [Header("Input")]
    [Tooltip("Tecla que, ademas del click, activa el boton de cerrar cuando esta visible.")]
    [SerializeField] private KeyCode closeKey = KeyCode.Q;

    private bool alreadyTriggered = false;
    private Coroutine currentRoutine;

    private void Awake()
    {
        if (panel == null)
        {
            Debug.LogWarning($"[ColliderCanvasFader] No hay Panel (RectTransform) asignado en {gameObject.name}");
        }
        else
        {
            // Busca el CanvasGroup en la carpeta del panel; si no tiene, se lo agrega.
            canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();

            // Estado inicial: panel invisible y no bloqueando clicks/raycasts.
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activateOnlyOnce && alreadyTriggered)
            return;

        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        alreadyTriggered = true;
        OpenPanel();
    }

    private void Update()
    {
        // Permite cerrar el panel con la tecla configurada, igual que si se clickeara el boton.
        if (closeButton != null && closeButton.gameObject.activeInHierarchy && Input.GetKeyDown(closeKey))
        {
            closeButton.onClick.Invoke();
        }
    }

    /// <summary>
    /// Muestra el panel con fade in y, tras el tiempo configurado, habilita el boton de cierre.
    /// </summary>
    public void OpenPanel()
    {
        if (panel == null || canvasGroup == null)
        {
            Debug.LogWarning($"[ColliderCanvasFader] No hay Panel asignado en {gameObject.name}");
            return;
        }

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        if (pauseTimeWhileOpen)
            Time.timeScale = 0f;

        panel.gameObject.SetActive(true);
        currentRoutine = StartCoroutine(OpenRoutine());
    }

    /// <summary>
    /// Cierra el panel (con fade out) y opcionalmente desactiva su GameObject.
    /// Se puede llamar desde el boton o manualmente desde otro script/evento.
    /// </summary>
    public void ClosePanel()
    {
        if (canvasGroup == null) return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        if (pauseTimeWhileOpen)
            Time.timeScale = 1f;

        currentRoutine = StartCoroutine(CloseRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        // Fade in
        yield return Fade(0f, 1f, fadeInDuration);
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Espera antes de mostrar el boton (tiempo real, no afectado por la pausa)
        yield return new WaitForSecondsRealtime(timeBeforeButton);

        if (closeButton != null)
            closeButton.gameObject.SetActive(true);
    }

    private IEnumerator CloseRoutine()
    {
        if (closeButton != null)
            closeButton.gameObject.SetActive(false);

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        yield return Fade(canvasGroup.alpha, 0f, fadeOutDuration);

        if (disableCanvasObjectOnClose)
            panel.gameObject.SetActive(false);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}