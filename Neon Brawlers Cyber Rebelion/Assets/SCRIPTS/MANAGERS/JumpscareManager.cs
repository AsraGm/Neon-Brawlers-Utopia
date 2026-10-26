using System.Collections;
using UnityEngine;

public class JumpscareManager : MonoBehaviour
{
    public static JumpscareManager instance { get; private set; }

    [Header("Cámara del jugador")]
    public Camera playerCamera;
    public GameObject playerMesh;

    [Header("Fondo animado")]
    public Renderer fondoRenderer;
    public float offsetYMin = -0.15f;
    public float offsetYMax = 0.15f;
    public float offsetSpeed = 1f;

    [Header("Audio jumpscare")]
    public string nombreCancionPrincipal;   // nombre exacto en AudioManager
    public string nombreGritoJumpscare;     // nombre exacto en AudioManager

    [Header("Jumpscares")]
    public JumpscareData[] jumpscares;

    [System.Serializable]
    public class JumpscareData
    {
        public string nombre;
        public Camera camara;
        public Animator animator;
        public string triggerAnimacion = "doJumpscare";
        public float duracion = 3f;
        public float tiempoInicioFade = 1.5f;
    }

    private bool animandoFondo = false;
    private Coroutine fondoCoroutine;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        foreach (var j in jumpscares)
            if (j.camara != null)
                j.camara.gameObject.SetActive(false);
    }

    public void ActivarJumpscare(string nombreEnemigo, System.Action onFinish, System.Action onInicioFade = null)
    {
        JumpscareData data = System.Array.Find(jumpscares, j => j.nombre == nombreEnemigo);
        if (data == null)
        {
            Debug.LogWarning($"Jumpscare '{nombreEnemigo}' no encontrado");
            onInicioFade?.Invoke();
            onFinish?.Invoke();
            return;
        }

        StartCoroutine(SecuenciaJumpscare(data, onFinish, onInicioFade));
    }

    private IEnumerator SecuenciaJumpscare(JumpscareData data, System.Action onFinish, System.Action onInicioFade)
    {
        // Apagar cámara y mesh del jugador
        if (playerCamera != null) playerCamera.gameObject.SetActive(false);
        if (playerMesh != null) playerMesh.SetActive(false);

        // Activar cámara del jumpscare
        data.camara.gameObject.SetActive(true);

        // Disparar animación
        if (data.animator != null)
            data.animator.SetTrigger(data.triggerAnimacion);

        // Pausar música principal y reproducir grito
        if (!string.IsNullOrEmpty(nombreCancionPrincipal))
            AudioManager.instance?.Stop(nombreCancionPrincipal);
        if (!string.IsNullOrEmpty(nombreGritoJumpscare))
            AudioManager.instance?.Play(nombreGritoJumpscare);

        // Iniciar animación del fondo
        if (fondoCoroutine != null) StopCoroutine(fondoCoroutine);
        fondoCoroutine = StartCoroutine(AnimarFondo());

        float inicioFade = Mathf.Min(data.tiempoInicioFade, data.duracion);
        yield return new WaitForSecondsRealtime(inicioFade);
        onInicioFade?.Invoke();

        yield return new WaitForSecondsRealtime(data.duracion - inicioFade);

        // Detener animación del fondo
        if (fondoCoroutine != null)
        {
            StopCoroutine(fondoCoroutine);
            fondoCoroutine = null;
        }
        animandoFondo = false;

        // Restaurar offset del fondo a 0
        if (fondoRenderer != null)
        {
            Material mat = fondoRenderer.material;
            Vector2 offset = mat.mainTextureOffset;
            offset.y = 0f;
            mat.mainTextureOffset = offset;
        }

        // Restaurar cámaras
        data.camara.gameObject.SetActive(false);
        if (playerCamera != null) playerCamera.gameObject.SetActive(true);
        if (playerMesh != null) playerMesh.SetActive(true);

        // Reanudar música principal
        if (!string.IsNullOrEmpty(nombreCancionPrincipal))
            AudioManager.instance?.Play(nombreCancionPrincipal);

        onFinish?.Invoke();
    }

    private IEnumerator AnimarFondo()
    {
        animandoFondo = true;
        float direccion = 1f;

        while (animandoFondo)
        {
            if (fondoRenderer == null) { yield return null; continue; }

            Material mat = fondoRenderer.material;
            Vector2 offset = mat.mainTextureOffset;

            offset.y += direccion * offsetSpeed * Time.unscaledDeltaTime;

            if (offset.y >= offsetYMax)
            {
                offset.y = offsetYMax;
                direccion = -1f;
            }
            else if (offset.y <= offsetYMin)
            {
                offset.y = offsetYMin;
                direccion = 1f;
            }

            mat.mainTextureOffset = offset;
            yield return null;
        }
    }
}