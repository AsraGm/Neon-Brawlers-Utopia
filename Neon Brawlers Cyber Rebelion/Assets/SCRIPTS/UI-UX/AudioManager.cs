using UnityEngine;
using System.Collections;

/// <summary>
/// L2 - Audio Manager COMPLETO (TODO EN UNO)
/// Sistema simple y escalable - Solo agrega sonidos en el Inspector y usa LaunchAudio()
/// </summary>
public class AudioManager : MonoBehaviour
{
    #region Singleton
    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InicializarSistema();
    }
    #endregion

    #region ===== AGREGAR TUS SONIDOS AQUÍ (INSPECTOR) =====
    [Header("=== SONIDOS DE GAMEPLAY ===")]
    public Sound[] gameSoundsLevel;

    [Header("=== SONIDOS DE UI ===")]
    public Sound[] gameSoundsUI;

    [Header("=== MÚSICA ===")]
    public Sound[] gameSoundsMusic;

    [Header("=== SONIDOS AMBIENTALES ===")]
    public Sound[] gameSoundsAmbient;

    [Header("=== OTROS SONIDOS ===")]
    public Sound[] gameSoundsOther;
    #endregion

    #region ===== CONTROL DE VOLÚMENES =====
    [Header("=== VOLÚMENES GLOBALES ===")]
    [Range(0f, 1f)] public float volumenMusica = 0.7f;
    [Range(0f, 1f)] public float volumenEfectos = 1f;
    [Range(0f, 1f)] public float volumenAmbiente = 0.5f;
    [Range(0f, 1f)] public float volumenUI = 1f;

    [Header("=== AUTO-START (Opcional) ===")]
    [Tooltip("¿Reproducir música automáticamente al iniciar?")]
    public bool reproducirMusicaAlIniciar = false;
    [Tooltip("Nombre de la música que se reproducirá al iniciar")]
    public string nombreMusicaInicial = "MusicaMenu";
    #endregion

    // Variables internas (no tocar)
    [HideInInspector] public float[] originalGameVolumesLevel;
    [HideInInspector] public float[] originalGameVolumesUI;
    [HideInInspector] public float[] originalGameVolumesMusic;
    [HideInInspector] public float[] originalGameVolumesAmbient;
    [HideInInspector] public float[] originalGameVolumesOther;

    private Sound musicaActual;
    private Coroutine fadeCoroutine;

    #region ===== INICIALIZACIÓN (AUTOMÁTICA) =====
    private void InicializarSistema()
    {
        // Inicializar todas las categorías
        InicializarCategoria(gameSoundsLevel, ref originalGameVolumesLevel);
        InicializarCategoria(gameSoundsUI, ref originalGameVolumesUI);
        InicializarCategoria(gameSoundsMusic, ref originalGameVolumesMusic);
        InicializarCategoria(gameSoundsAmbient, ref originalGameVolumesAmbient);
        InicializarCategoria(gameSoundsOther, ref originalGameVolumesOther);

        CargarConfiguracion();

        Debug.Log($"[AudioManager] ✅ Inicializado con {TotalSonidos()} sonidos");

        // Auto-start música si está activado
        if (reproducirMusicaAlIniciar && !string.IsNullOrEmpty(nombreMusicaInicial))
        {
            LaunchAudio(nombreMusicaInicial, AudioType.music);
            Debug.Log($"[AudioManager] 🎵 Reproduciendo música inicial: {nombreMusicaInicial}");
        }
    }

    private void InicializarCategoria(Sound[] sounds, ref float[] originalVolumes)
    {
        if (sounds == null || sounds.Length == 0) return;

        originalVolumes = new float[sounds.Length];

        for (int i = 0; i < sounds.Length; i++)
        {
            Sound s = sounds[i];
            s.audioSource = gameObject.AddComponent<AudioSource>();
            s.audioSource.clip = s.clip;
            s.audioSource.loop = s.loop;
            s.audioSource.volume = s.volume;
            s.audioSource.pitch = s.pitch;
            s.audioSource.playOnAwake = false;
            originalVolumes[i] = s.volume;
        }
    }

    private int TotalSonidos()
    {
        int total = 0;
        if (gameSoundsLevel != null) total += gameSoundsLevel.Length;
        if (gameSoundsUI != null) total += gameSoundsUI.Length;
        if (gameSoundsMusic != null) total += gameSoundsMusic.Length;
        if (gameSoundsAmbient != null) total += gameSoundsAmbient.Length;
        if (gameSoundsOther != null) total += gameSoundsOther.Length;
        return total;
    }
    #endregion

    #region ===== FUNCIÓN PRINCIPAL - REPRODUCIR SONIDO =====
    /// <summary>
    /// ⭐ USA ESTA FUNCIÓN PARA TODO - Reproduce cualquier sonido
    /// Ejemplo: AudioManager.Instance.LaunchAudio("Pasos", AudioType.level);
    /// </summary>
    public void LaunchAudio(string nombre, AudioType audioType)
    {
        Sound[] soundArray = ObtenerArrayPorTipo(audioType);

        if (soundArray == null || soundArray.Length == 0)
        {
            Debug.LogWarning($"[AudioManager] ⚠️ No hay sonidos en: {audioType}");
            return;
        }

        Sound sound = System.Array.Find(soundArray, s => s.soundName == nombre);

        if (sound != null && sound.audioSource != null)
        {
            sound.audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"[AudioManager] ⚠️ No existe '{nombre}' en {audioType}");
        }
    }

    /// <summary>
    /// Detiene un sonido específico
    /// </summary>
    public void StopAudio(string nombre, AudioType audioType)
    {
        Sound[] soundArray = ObtenerArrayPorTipo(audioType);
        if (soundArray == null) return;

        Sound sound = System.Array.Find(soundArray, s => s.soundName == nombre);

        if (sound != null && sound.audioSource != null)
        {
            sound.audioSource.Stop();
        }
    }

    /// <summary>
    /// Reproduce sonido con pitch aleatorio (para variedad)
    /// </summary>
    public void LaunchAudioConPitchAleatorio(string nombre, AudioType audioType, float pitchMin = 0.9f, float pitchMax = 1.1f)
    {
        Sound[] soundArray = ObtenerArrayPorTipo(audioType);
        if (soundArray == null) return;

        Sound sound = System.Array.Find(soundArray, s => s.soundName == nombre);

        if (sound != null && sound.audioSource != null)
        {
            sound.audioSource.pitch = Random.Range(pitchMin, pitchMax);
            sound.audioSource.Play();
        }
    }
    #endregion

    #region ===== MÚSICA CON FADE =====
    /// <summary>
    /// Cambia la música con transición suave
    /// Ejemplo: AudioManager.Instance.CambiarMusica("MusicaBoss", 2f);
    /// </summary>
    public void CambiarMusica(string nombreMusica, float tiempoFade = 2f)
    {
        if (gameSoundsMusic == null || gameSoundsMusic.Length == 0) return;

        Sound nuevaMusica = System.Array.Find(gameSoundsMusic, s => s.soundName == nombreMusica);

        if (nuevaMusica == null)
        {
            Debug.LogWarning($"[AudioManager] No existe la música: {nombreMusica}");
            return;
        }

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeMusica(nuevaMusica, tiempoFade));
    }

    private IEnumerator FadeMusica(Sound nuevaMusica, float duracion)
    {
        float mitad = duracion / 2f;

        // Fade out música actual
        if (musicaActual != null && musicaActual.audioSource.isPlaying)
        {
            float volInicial = musicaActual.audioSource.volume;
            float tiempo = 0f;

            while (tiempo < mitad)
            {
                tiempo += Time.deltaTime;
                musicaActual.audioSource.volume = Mathf.Lerp(volInicial, 0f, tiempo / mitad);
                yield return null;
            }

            musicaActual.audioSource.Stop();
        }

        // Fade in nueva música
        musicaActual = nuevaMusica;
        musicaActual.audioSource.volume = 0f;
        musicaActual.audioSource.Play();

        float volObjetivo = nuevaMusica.volume * volumenMusica;
        float tiempo2 = 0f;

        while (tiempo2 < mitad)
        {
            tiempo2 += Time.deltaTime;
            musicaActual.audioSource.volume = Mathf.Lerp(0f, volObjetivo, tiempo2 / mitad);
            yield return null;
        }

        musicaActual.audioSource.volume = volObjetivo;
    }

    /// <summary>
    /// Pausar/reanudar música
    /// </summary>
    public void PausarMusica(bool pausar)
    {
        if (musicaActual != null && musicaActual.audioSource != null)
        {
            if (pausar) musicaActual.audioSource.Pause();
            else musicaActual.audioSource.UnPause();
        }
    }

    /// <summary>
    /// Detener toda la música
    /// </summary>
    public void DetenerMusica()
    {
        if (gameSoundsMusic != null)
        {
            foreach (Sound s in gameSoundsMusic)
            {
                if (s.audioSource != null && s.audioSource.isPlaying)
                    s.audioSource.Stop();
            }
        }
        musicaActual = null;
    }
    #endregion

    #region ===== AJUSTAR VOLÚMENES =====
    /// <summary>
    /// Ajusta volumen de efectos de sonido
    /// </summary>
    public void AjustarVolumenEfectos(float nuevoVolumen)
    {
        volumenEfectos = Mathf.Clamp01(nuevoVolumen);
        AplicarVolumen(gameSoundsLevel, originalGameVolumesLevel, volumenEfectos);
        AplicarVolumen(gameSoundsOther, originalGameVolumesOther, volumenEfectos);
        GuardarConfiguracion();
    }

    /// <summary>
    /// Ajusta volumen de música
    /// </summary>
    public void AjustarVolumenMusica(float nuevoVolumen)
    {
        volumenMusica = Mathf.Clamp01(nuevoVolumen);
        AplicarVolumen(gameSoundsMusic, originalGameVolumesMusic, volumenMusica);
        GuardarConfiguracion();
    }

    /// <summary>
    /// Ajusta volumen ambiental
    /// </summary>
    public void AjustarVolumenAmbiente(float nuevoVolumen)
    {
        volumenAmbiente = Mathf.Clamp01(nuevoVolumen);
        AplicarVolumen(gameSoundsAmbient, originalGameVolumesAmbient, volumenAmbiente);
        GuardarConfiguracion();
    }

    /// <summary>
    /// Ajusta volumen de UI
    /// </summary>
    public void AjustarVolumenUI(float nuevoVolumen)
    {
        volumenUI = Mathf.Clamp01(nuevoVolumen);
        AplicarVolumen(gameSoundsUI, originalGameVolumesUI, volumenUI);
        GuardarConfiguracion();
    }

    private void AplicarVolumen(Sound[] sounds, float[] originalVol, float multiplicador)
    {
        if (sounds == null || originalVol == null) return;

        for (int i = 0; i < sounds.Length && i < originalVol.Length; i++)
        {
            if (sounds[i].audioSource != null)
                sounds[i].audioSource.volume = originalVol[i] * multiplicador;
        }
    }

    /// <summary>
    /// Mutear todo el audio
    /// </summary>
    public void MutearTodo(bool mutear)
    {
        AudioListener.volume = mutear ? 0f : 1f;
    }
    #endregion

    #region ===== GUARDAR/CARGAR CONFIGURACIÓN =====
    public void GuardarConfiguracion()
    {
        PlayerPrefs.SetFloat("VolumenMusica", volumenMusica);
        PlayerPrefs.SetFloat("VolumenEfectos", volumenEfectos);
        PlayerPrefs.SetFloat("VolumenAmbiente", volumenAmbiente);
        PlayerPrefs.SetFloat("VolumenUI", volumenUI);
        PlayerPrefs.Save();
    }

    public void CargarConfiguracion()
    {
        if (PlayerPrefs.HasKey("VolumenMusica"))
        {
            volumenMusica = PlayerPrefs.GetFloat("VolumenMusica");
            AplicarVolumen(gameSoundsMusic, originalGameVolumesMusic, volumenMusica);
        }

        if (PlayerPrefs.HasKey("VolumenEfectos"))
        {
            volumenEfectos = PlayerPrefs.GetFloat("VolumenEfectos");
            AplicarVolumen(gameSoundsLevel, originalGameVolumesLevel, volumenEfectos);
            AplicarVolumen(gameSoundsOther, originalGameVolumesOther, volumenEfectos);
        }

        if (PlayerPrefs.HasKey("VolumenAmbiente"))
        {
            volumenAmbiente = PlayerPrefs.GetFloat("VolumenAmbiente");
            AplicarVolumen(gameSoundsAmbient, originalGameVolumesAmbient, volumenAmbiente);
        }

        if (PlayerPrefs.HasKey("VolumenUI"))
        {
            volumenUI = PlayerPrefs.GetFloat("VolumenUI");
            AplicarVolumen(gameSoundsUI, originalGameVolumesUI, volumenUI);
        }
    }
    #endregion

    #region ===== UTILIDADES =====
    private Sound[] ObtenerArrayPorTipo(AudioType tipo)
    {
        switch (tipo)
        {
            case AudioType.level: return gameSoundsLevel;
            case AudioType.ui: return gameSoundsUI;
            case AudioType.music: return gameSoundsMusic;
            case AudioType.ambient: return gameSoundsAmbient;
            case AudioType.other: return gameSoundsOther;
            default: return null;
        }
    }

    public bool ExisteSonido(string nombre, AudioType tipo)
    {
        Sound[] array = ObtenerArrayPorTipo(tipo);
        if (array == null) return false;
        return System.Array.Exists(array, s => s.soundName == nombre);
    }

    public bool EstaSonando(string nombre, AudioType tipo)
    {
        Sound[] array = ObtenerArrayPorTipo(tipo);
        if (array == null) return false;
        Sound sound = System.Array.Find(array, s => s.soundName == nombre);
        return sound != null && sound.audioSource != null && sound.audioSource.isPlaying;
    }
    #endregion
}

// ===== CLASES AUXILIARES =====
[System.Serializable]
public class Sound
{
    public string soundName;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    public bool loop = false;
    [HideInInspector] public AudioSource audioSource;
}

public enum AudioType
{
    level,      // Sonidos del juego
    ui,         // Sonidos de interfaz
    music,      // Música
    ambient,    // Ambiente
    other       // Otros
}