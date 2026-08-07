using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ScreamerNPC : MonoBehaviour
{
    [Header("Cámaras")]
    public Camera screamerCamera;
    public Camera playerCamera;
    public GameObject playerMesh;      // <el GameObject con el mesh del jugador

    [Header("Animator")]
    public Animator npcAnimator;       // el animator del NPC

    [Header("Ajustes")]
    public string screamerTrigger = "doScreamer";  // nombre del trigger en el Animator

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip screamerSound;

    [Header("=== EFECTO SHADER (Fullscreen Pass) ===")]
    [SerializeField] private ScriptableRendererFeature screamerRenderFeature;
    [SerializeField] private Material materialScreamer;
    private string nombrePropiedadNoise = "_Noise_Amount";
    private string nombrePropiedadGlitch = "_Glitch_Strength";
    [SerializeField] private float valorMaximoNoise = 1f;
    [SerializeField] private float valorMaximoGlitch = 1f;
    [SerializeField] private float velocidadTransicionEfecto = 3f;
    [SerializeField] private float velocidadTransicionEfectoSalida = 0.5f;
    private int idPropiedadNoise;
    private int idPropiedadGlitch;
    private float noiseValorObjetivo = 0f;
    private float noiseValorActual = 0f;
    private float glitchValorObjetivo = 0f;
    private float glitchValorActual = 0f;
    private bool screamerFeatureActiva = false;

    bool screamerActive = false;
    bool playerInside = false;
    bool playerHasExited = false;   // obliga al jugador a salir antes de reactivar
    private Collider triggerCol;


    private void Awake()
    {
        // Aseguramos estado inicial
        screamerCamera.gameObject.SetActive(false);
        triggerCol = GetComponent<BoxCollider>();

        idPropiedadNoise = Shader.PropertyToID(nombrePropiedadNoise);
        idPropiedadGlitch = Shader.PropertyToID(nombrePropiedadGlitch);
        if (materialScreamer != null)
        {
            materialScreamer.SetFloat(idPropiedadNoise, 0f);
            materialScreamer.SetFloat(idPropiedadGlitch, 0f);
        }
        if (screamerRenderFeature != null)
            screamerRenderFeature.SetActive(false);
    }

    private void Update()
    {
        ActualizarTransicionEfecto();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        // Solo se activa si el jugador ya salió antes (o es la primera vez)
        if (screamerActive) return;
        if (playerInside) return;
        if (!playerHasExited && playerInside) return;
        playerInside = true;
        playerHasExited = false;
        TriggerScreamer();
    }
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
        playerHasExited = true;   // ya salió, puede volver a activarse
    }
    void TriggerScreamer()
    {
        screamerActive = true;
        playerCamera.gameObject.SetActive(false);
        playerMesh?.SetActive(false);
        screamerCamera.gameObject.SetActive(true);
        npcAnimator.SetTrigger(screamerTrigger);

        if (audioSource != null && screamerSound != null)
            audioSource.PlayOneShot(screamerSound);

        // Prender shader
        if (materialScreamer != null)
        {
            noiseValorObjetivo = valorMaximoNoise;
            glitchValorObjetivo = valorMaximoGlitch;
            if (screamerRenderFeature != null && !screamerFeatureActiva)
            {
                screamerRenderFeature.SetActive(true);
                screamerFeatureActiva = true;
            }
        }

        StartCoroutine(WaitForScreamerEnd());
    }
    System.Collections.IEnumerator WaitForScreamerEnd()
    {
        yield return null;
        yield return null; // dos frames para asegurar que el Animator actualizó
        // Esperar a que entre al estado del screamer
        float timeout = 10f;
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            AnimatorStateInfo stateInfo = npcAnimator.GetCurrentAnimatorStateInfo(0);
            // Cuando entre al estado screamer y esté por terminar
            if (stateInfo.IsName("Screamer") && stateInfo.normalizedTime >= 0.95f)
                break;
            elapsed += Time.deltaTime;
            yield return null;
        }
        EndScreamer();
    }
    void EndScreamer()
    {
        screamerActive = false;
        screamerCamera.gameObject.SetActive(false);
        playerCamera.gameObject.SetActive(true);
        playerMesh?.SetActive(true);
        triggerCol.enabled = false;

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        // Apagar shader (con fade, la feature se desactiva sola al llegar a 0)
        if (materialScreamer != null)
        {
            noiseValorObjetivo = 0f;
            glitchValorObjetivo = 0f;
        }
    }

    private void ActualizarTransicionEfecto()
    {
        if (materialScreamer == null) return;

        bool noiseListo = Mathf.Approximately(noiseValorActual, noiseValorObjetivo);
        bool glitchListo = Mathf.Approximately(glitchValorActual, glitchValorObjetivo);

        float velocidadNoise = noiseValorObjetivo <= 0f ? velocidadTransicionEfectoSalida : velocidadTransicionEfecto;
        float velocidadGlitch = glitchValorObjetivo <= 0f ? velocidadTransicionEfectoSalida : velocidadTransicionEfecto;

        if (!noiseListo)
        {
            noiseValorActual = Mathf.MoveTowards(noiseValorActual, noiseValorObjetivo, velocidadNoise * Time.deltaTime);
            materialScreamer.SetFloat(idPropiedadNoise, noiseValorActual);
        }

        if (!glitchListo)
        {
            glitchValorActual = Mathf.MoveTowards(glitchValorActual, glitchValorObjetivo, velocidadGlitch * Time.deltaTime);
            materialScreamer.SetFloat(idPropiedadGlitch, glitchValorActual);
        }

        if (noiseListo && glitchListo && noiseValorObjetivo <= 0f && glitchValorObjetivo <= 0f
            && screamerRenderFeature != null && screamerFeatureActiva)
        {
            screamerRenderFeature.SetActive(false);
            screamerFeatureActiva = false;
        }
    }
}