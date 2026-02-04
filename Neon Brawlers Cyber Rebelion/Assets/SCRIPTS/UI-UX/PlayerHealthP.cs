using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ✅ VERSIÓN COMPATIBLE - Sistema de Salud del Jugador
/// Compatible con GameManager actualizado
/// Incluye todos los métodos necesarios para checkpoints
/// </summary>
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
    private bool estaMuerto = false;

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

    /// <summary>
    /// Aplica daño al jugador
    /// </summary>
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

    /// <summary>
    /// Cura al jugador
    /// </summary>
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

    /// <summary>
    /// ✅ MÉTODO REQUERIDO POR GAMEMANAGER
    /// Establece la vida a un valor específico (usado por checkpoints)
    /// </summary>
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

    /// <summary>
    /// ✅ MÉTODO REQUERIDO POR GAMEMANAGER
    /// Resetea el estado de muerte (usado al cargar checkpoint)
    /// </summary>
    public void ResetearEstadoMuerte()
    {
        estaMuerto = false;

        if (mostrarLogs)
        {
            Debug.Log("[PlayerHealth] Estado de muerte reseteado");
        }
    }

    /// <summary>
    /// Maneja la muerte del jugador
    /// </summary>
    private void Morir()
    {
        if (estaMuerto) return;

        estaMuerto = true;

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

    /// <summary>
    /// Carga el último checkpoint guardado
    /// </summary>
    private void CargarCheckpoint()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CargarCheckpoint();
        }
        else
        {
            Debug.LogError("[PlayerHealth] ❌ No hay GameManager para cargar checkpoint");
        }
    }

    /// <summary>
    /// Reproduce efectos de daño
    /// </summary>
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

    /// <summary>
    /// Actualiza la UI de vida
    /// </summary>
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

    /// <summary>
    /// Establece la vida máxima (útil para power-ups)
    /// </summary>
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

    /// <summary>
    /// Obtiene el porcentaje de vida actual (0-1)
    /// </summary>
    public float ObtenerPorcentajeVida()
    {
        return vidaActual / vidaMaxima;
    }

    /// <summary>
    /// Verifica si el jugador está vivo
    /// </summary>
    public bool EstaVivo()
    {
        return vidaActual > 0 && !estaMuerto;
    }

    // ============================================
    // MÉTODOS DE DEBUG (Solo en Editor)
    // ============================================
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