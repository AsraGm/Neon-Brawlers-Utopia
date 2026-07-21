using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("=== VIDA ===")]
    public float vidaMaxima = 100f;
    public float vidaActual = 100f;

    [Header("=== UI (OPCIONAL) ===")]
    [SerializeField] private Slider barraVida;
    [SerializeField] private TextMeshProUGUI textoVida;

    [Header("=== EFECTOS (OPCIONAL) ===")]
    [SerializeField] private AudioClip sonidoDanio;
    [SerializeField] private AudioClip sonidoMuerte;
    [SerializeField] private ParticleSystem particulasDanio;

    //[Header("=== CAMERA SHAKE ===")]
    //[SerializeField] private CinemachineImpulseSource impulseSource;
    //[SerializeField] private float fuerzaImpulso = 0.4f;

    [Header("=== VIGNETTE DE DAÑO ===")]
    [SerializeField] private ScriptableRendererFeature vignetteRenderFeature;
    [SerializeField] private Material materialVignette;
    [SerializeField] private float cambioDanoMaximo = 80f;
    [SerializeField] private float velocidadTransicionVignette = 3f;

    private int idPropiedadVignette;
    private float vignetteRadiusObjetivo = 1f;
    private float vignetteRadiusActual = 1f;
    private bool vignetteFeatureActiva = false;

    [Header("=== CONFIGURACIÓN ===")]
    [SerializeField] private float tiempoEsperaAntesDeCargarCheckpoint = 1.5f;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool mostrarLogs = true;

    private AudioSource audioSource;
    [SerializeField] private bool estaMuerto = false; //serialized para pruebas

    private void Start()
    {
        // Crear AudioSource si no existe
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        idPropiedadVignette = Shader.PropertyToID("_Vignette_radius");

        // Inicializar vida
        vidaActual = vidaMaxima;
        ActualizarUI();
        ActualizarObjetivoVignette();

        if (materialVignette != null)
        {
            vignetteRadiusActual = vignetteRadiusObjetivo;
            materialVignette.SetFloat(idPropiedadVignette, vignetteRadiusActual);
        }

        if (vignetteRenderFeature != null)
        {
            vignetteRenderFeature.SetActive(false);
            vignetteFeatureActiva = false;
        }

        if (mostrarLogs)
        {
            Debug.Log($"[PlayerHealth] Inicializado con {vidaActual}/{vidaMaxima} vida");
        }
    }

    public void RecibirDanio(float cantidad)
    {
        if (estaMuerto) return;

        vidaActual -= cantidad;
        vidaActual = Mathf.Max(0, vidaActual);

        if (mostrarLogs)
        {
            Debug.Log($"[PlayerHealth] Daño recibido: {cantidad}. Vida actual: {vidaActual}/{vidaMaxima}");
        }

        // TODO: Integración con PostProcessManager (futuro)
        /*
        if (PostProcessManager.Instance != null)
        {
            PostProcessManager.Instance.SetDamageVignette(vidaActual / vidaMaxima);
        }
        */

        // Efectos
        ReproducirEfectosDanio();
        ActualizarUI();
        ActualizarObjetivoVignette();

        // Verificar muerte
        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    public void Curar(float cantidad)
    {
        if (estaMuerto) return;

        vidaActual += cantidad;
        vidaActual = Mathf.Min(vidaMaxima, vidaActual);

        if (mostrarLogs)
        {
            Debug.Log($"[PlayerHealth] Curación: {cantidad}. Vida actual: {vidaActual}/{vidaMaxima}");
        }

        ActualizarUI();
        ActualizarObjetivoVignette();
    }

    public void EstablecerVida(float nuevaVida, float nuevaVidaMaxima)
    {
        vidaMaxima = nuevaVidaMaxima;
        vidaActual = nuevaVida;
        estaMuerto = false;

        if (mostrarLogs)
        {
            Debug.Log($"[PlayerHealth] Vida establecida desde checkpoint: {vidaActual}/{vidaMaxima}");
        }

        ActualizarUI();
        ActualizarObjetivoVignette();
    }

    public void ResetearEstadoMuerte()
    {
        estaMuerto = false;

        if (mostrarLogs)
        {
            Debug.Log("[PlayerHealth] Estado de muerte reseteado");
        }
    }

    private void Morir()
    {
        if (estaMuerto) return;

        estaMuerto = true;
        vidaActual = vidaMaxima;
        HabilidadesManager.instance.playerIsHiding = false;

        TransitionDeath.Instance?.IniciarGlitchLuegoFade();

        if (mostrarLogs)
        {
            Debug.Log("[PlayerHealth] ☠️ Jugador muerto. Cargando checkpoint...");
        }

        // TODO: Integración con PostProcessManager (futuro)
        /*
        if (PostProcessManager.Instance != null)
        {
            PostProcessManager.Instance.ActivarVignetteMuerte();
        }
        */

        ActualizarObjetivoVignette();

        // Reproducir efectos de muerte
        if (sonidoMuerte != null && audioSource != null)
        {
            audioSource.PlayOneShot(sonidoMuerte);
        }

        // Esperar un momento antes de cargar checkpoint (para que se vean efectos)
        Invoke(nameof(CargarCheckpoint), tiempoEsperaAntesDeCargarCheckpoint);
    }

    private void CargarCheckpoint()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CargarCheckpoint();
            ResetearEstadoMuerte();
            TransitionDeath.Instance?.TerminarFadeLuegoGlitch();
        }
        else
        {
            Debug.LogError("[PlayerHealth] ❌ No hay GameManager para cargar checkpoint");
        }
    }

    private void ReproducirEfectosDanio()
    {
        // Audio
        if (sonidoDanio != null && audioSource != null)
        {
            audioSource.PlayOneShot(sonidoDanio);
        }

        // Partículas
        if (particulasDanio != null)
        {
            particulasDanio.Play();
        }

        ////Camera shake
        //if (impulseSource != null)
        //{
        //    float intensidad = Mathf.Clamp01((vidaMaxima - vidaActual) / cambioDanoMaximo);
        //    Vector3 direccionShake = new Vector3(0f, -0.5f, -1f).normalized;
        //    impulseSource.GenerateImpulse(direccionShake * intensidad * fuerzaImpulso);
        //}
    }

    private void ActualizarUI()
    {
        // Actualizar barra de vida
        if (barraVida != null)
        {
            barraVida.maxValue = vidaMaxima;
            barraVida.value = vidaActual;
        }

        // Actualizar texto
        if (textoVida != null)
        {
            textoVida.text = $"{Mathf.CeilToInt(vidaActual)}/{Mathf.CeilToInt(vidaMaxima)}";
        }
    }

    private void ActualizarObjetivoVignette()
    {
        if (materialVignette == null) return;

        float danioAcumulado = vidaMaxima - vidaActual;
        vignetteRadiusObjetivo = 1f - Mathf.Clamp01(danioAcumulado / cambioDanoMaximo);

        if (danioAcumulado > 0f && vignetteRenderFeature != null && !vignetteFeatureActiva)
        {
            vignetteRenderFeature.SetActive(true);
            vignetteFeatureActiva = true;
        }
    }

    private void ActualizarTransicionVignette()
    {
        if (materialVignette == null) return;
        if (Mathf.Approximately(vignetteRadiusActual, vignetteRadiusObjetivo))
        {
            if (vignetteRadiusObjetivo >= 1f && vignetteRenderFeature != null && vignetteFeatureActiva)
            {
                vignetteRenderFeature.SetActive(false);
                vignetteFeatureActiva = false;
            }
            return;
        }

        vignetteRadiusActual = Mathf.MoveTowards(vignetteRadiusActual, vignetteRadiusObjetivo, velocidadTransicionVignette * Time.deltaTime);
        materialVignette.SetFloat(idPropiedadVignette, vignetteRadiusActual);
    }

    public void EstablecerVidaMaxima(float nuevaVidaMaxima)
    {
        vidaMaxima = nuevaVidaMaxima;
        vidaActual = Mathf.Min(vidaActual, vidaMaxima);
        ActualizarUI();
        ActualizarObjetivoVignette();

        if (mostrarLogs)
        {
            Debug.Log($"[PlayerHealth] Vida máxima cambiada a: {vidaMaxima}");
        }
    }

    public float ObtenerPorcentajeVida()
    {
        return vidaActual / vidaMaxima;
    }

    public bool EstaVivo()
    {
        return vidaActual > 0 && !estaMuerto;
    }

#if UNITY_EDITOR
    private void Update()
    {
        // K = Recibir 20 de daño
        if (Input.GetKeyDown(KeyCode.K))
        {
            RecibirDanio(20f);
        }

        // L = Morir instantáneamente
        if (Input.GetKeyDown(KeyCode.L))
        {
            RecibirDanio(vidaActual);
        }

        // H = Curarse completamente
        if (Input.GetKeyDown(KeyCode.H))
        {
            Curar(vidaMaxima);
        }

        ActualizarTransicionVignette();
    }

    [ContextMenu("Recibir 10 de Daño")]
    private void TestDanio10()
    {
        RecibirDanio(10f);
    }

    [ContextMenu("Recibir 50 de Daño")]
    private void TestDanio50()
    {
        RecibirDanio(50f);
    }

    [ContextMenu("Morir Instantáneamente")]
    private void TestMuerte()
    {
        RecibirDanio(vidaActual);
    }

    [ContextMenu("Curación Completa")]
    private void TestCuracionCompleta()
    {
        Curar(vidaMaxima);
    }

    [ContextMenu("Establecer Vida a 50%")]
    private void TestVida50()
    {
        EstablecerVida(vidaMaxima * 0.5f, vidaMaxima);
    }
#endif
}