using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        // Inicializar vida
        vidaActual = vidaMaxima;
        ActualizarUI();

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

    public void EstablecerVidaMaxima(float nuevaVidaMaxima)
    {
        vidaMaxima = nuevaVidaMaxima;
        vidaActual = Mathf.Min(vidaActual, vidaMaxima);
        ActualizarUI();

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