using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// L1 - Game Manager (Singleton)
/// Manager central que controla el estado del juego, checkpoints y guardado
/// ESTE SCRIPT DEBE EXISTIR ANTES DE CUALQUIER OTRO SISTEMA
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern - solo una instancia
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

    #region L1.1 - Control de Estado del Juego
    public enum EstadoJuego
    {
        Menu,
        Jugando,
        Pausado,
        GameOver
    }

    [Header("Estado del Juego")]
    public EstadoJuego estadoActual = EstadoJuego.Menu;

    // Eventos para que otros sistemas reaccionen a cambios de estado
    public delegate void CambioEstado(EstadoJuego nuevoEstado);
    public event CambioEstado OnCambioEstado;

    /// <summary>
    /// Cambia el estado del juego y notifica a todos los sistemas suscritos
    /// </summary>
    public void CambiarEstado(EstadoJuego nuevoEstado)
    {
        if (estadoActual == nuevoEstado) return;

        estadoActual = nuevoEstado;

        // Notificar a otros sistemas
        OnCambioEstado?.Invoke(nuevoEstado);

        // Ajustar Time.timeScale según el estado
        switch (nuevoEstado)
        {
            case EstadoJuego.Menu:
            case EstadoJuego.GameOver:
                Time.timeScale = 0f;
                break;
            case EstadoJuego.Pausado:
                Time.timeScale = 0f;
                break;
            case EstadoJuego.Jugando:
                Time.timeScale = 1f;
                break;
        }

        Debug.Log($"[GameManager] Estado cambiado a: {nuevoEstado}");
    }

    /// <summary>
    /// Pausar/despausar el juego
    /// </summary>
    public void PausarJuego(bool pausar)
    {
        if (pausar)
            CambiarEstado(EstadoJuego.Pausado);
        else
            CambiarEstado(EstadoJuego.Jugando);
    }
    #endregion

    #region L1.2 - Sistema de Checkpoints
    [System.Serializable]
    public class DatosCheckpoint
    {
        public Vector3 posicionJugador;
        public Quaternion rotacionJugador;
        public float vidaJugador;
        public List<string> inventario = new List<string>();
        public Dictionary<string, bool> estadoObjetos = new Dictionary<string, bool>();

        public DatosCheckpoint()
        {
            inventario = new List<string>();
            estadoObjetos = new Dictionary<string, bool>();
        }
    }

    [Header("Sistema de Checkpoints")]
    [SerializeField] private Transform jugador;
    [SerializeField] private bool autoGuardarEnCheckpoint = true;

    private DatosCheckpoint checkpointActual;
    private DatosCheckpoint checkpointUltimoGuardado;

    /// <summary>
    /// Guarda el estado actual del juego en un checkpoint
    /// </summary>
    public void GuardarCheckpoint()
    {
        if (jugador == null)
        {
            Debug.LogError("[GameManager] No hay referencia al jugador para guardar checkpoint");
            return;
        }

        checkpointActual = new DatosCheckpoint
        {
            posicionJugador = jugador.position,
            rotacionJugador = jugador.rotation
        };

        // Guardar vida del jugador (si tiene el componente)
        // TODO: Descomentar cuando exista PlayerHealth (D1)
        /*
        var playerHealth = jugador.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            checkpointActual.vidaJugador = playerHealth.vidaActual;
        }
        */

        // Guardar inventario (si existe InventoryUI)
        // TODO: Descomentar cuando exista InventoryUI (J2)
        /*
        if (InventoryUI.Instance != null)
        {
            checkpointActual.inventario = new List<string>(InventoryUI.Instance.ObtenerItemsIDs());
        }
        */

        // Clonar para el último guardado
        checkpointUltimoGuardado = ClonearCheckpoint(checkpointActual);

        Debug.Log($"[GameManager] Checkpoint guardado en posición: {checkpointActual.posicionJugador}");

        // Guardar persistente si está activado
        if (autoGuardarEnCheckpoint)
        {
            GuardarJuego();
        }
    }

    /// <summary>
    /// Carga el último checkpoint guardado
    /// </summary>
    public void CargarCheckpoint()
    {
        if (checkpointUltimoGuardado == null)
        {
            Debug.LogWarning("[GameManager] No hay checkpoint guardado para cargar");
            return;
        }

        if (jugador == null)
        {
            Debug.LogError("[GameManager] No hay referencia al jugador para cargar checkpoint");
            return;
        }

        // Restaurar posición y rotación
        jugador.position = checkpointUltimoGuardado.posicionJugador;
        jugador.rotation = checkpointUltimoGuardado.rotacionJugador;

        // Restaurar vida
        // TODO: Descomentar cuando exista PlayerHealth (D1)
        /*
        var playerHealth = jugador.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.vidaActual = checkpointUltimoGuardado.vidaJugador;
        }
        */

        // Limpiar y restaurar inventario
        // TODO: Descomentar cuando exista InventoryUI (J2)
        /*
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.LimpiarInventario();
            foreach (string itemID in checkpointUltimoGuardado.inventario)
            {
                // Aquí deberías tener un sistema para recrear items desde IDs
                // InventoryUI.Instance.AgregarItemPorID(itemID);
            }
        }
        */

        // Resetear efectos de post-processing
        // TODO: Descomentar cuando exista PostProcessManager (N1)
        /*
        if (PostProcessManager.Instance != null)
        {
            PostProcessManager.Instance.ResetearEfectos();
        }
        */

        Debug.Log("[GameManager] Checkpoint cargado correctamente");

        CambiarEstado(EstadoJuego.Jugando);
    }

    /// <summary>
    /// Maneja la muerte del jugador
    /// </summary>
    public void JugadorMuerto()
    {
        Debug.Log("[GameManager] Jugador muerto - Preparando para cargar checkpoint");
        CambiarEstado(EstadoJuego.GameOver);

        // Aquí puedes agregar delay antes de cargar
        Invoke(nameof(CargarCheckpoint), 2f);
    }

    /// <summary>
    /// Clona un checkpoint (deep copy)
    /// </summary>
    private DatosCheckpoint ClonearCheckpoint(DatosCheckpoint original)
    {
        var clon = new DatosCheckpoint
        {
            posicionJugador = original.posicionJugador,
            rotacionJugador = original.rotacionJugador,
            vidaJugador = original.vidaJugador,
            inventario = new List<string>(original.inventario),
            estadoObjetos = new Dictionary<string, bool>(original.estadoObjetos)
        };
        return clon;
    }
    #endregion

    #region L1.3 - Sistema de Guardado Persistente
    [Header("Guardado Persistente")]
    [SerializeField] private string nombreArchivoGuardado = "SaveData";

    private const string KEY_POSICION_X = "PosX";
    private const string KEY_POSICION_Y = "PosY";
    private const string KEY_POSICION_Z = "PosZ";
    private const string KEY_VIDA = "Vida";
    private const string KEY_INVENTARIO = "Inventario";

    /// <summary>
    /// Guarda el juego en PlayerPrefs
    /// </summary>
    public void GuardarJuego()
    {
        if (checkpointActual == null)
        {
            Debug.LogWarning("[GameManager] No hay checkpoint para guardar");
            return;
        }

        // Guardar posición
        PlayerPrefs.SetFloat(KEY_POSICION_X, checkpointActual.posicionJugador.x);
        PlayerPrefs.SetFloat(KEY_POSICION_Y, checkpointActual.posicionJugador.y);
        PlayerPrefs.SetFloat(KEY_POSICION_Z, checkpointActual.posicionJugador.z);

        // Guardar vida
        PlayerPrefs.SetFloat(KEY_VIDA, checkpointActual.vidaJugador);

        // Guardar inventario como string separado por comas
        string inventarioString = string.Join(",", checkpointActual.inventario);
        PlayerPrefs.SetString(KEY_INVENTARIO, inventarioString);

        PlayerPrefs.Save();
        Debug.Log("[GameManager] Juego guardado correctamente");
    }

    /// <summary>
    /// Carga el juego desde PlayerPrefs
    /// </summary>
    public void CargarJuego()
    {
        if (!PlayerPrefs.HasKey(KEY_POSICION_X))
        {
            Debug.LogWarning("[GameManager] No hay datos guardados para cargar");
            return;
        }

        checkpointUltimoGuardado = new DatosCheckpoint();

        // Cargar posición
        float x = PlayerPrefs.GetFloat(KEY_POSICION_X);
        float y = PlayerPrefs.GetFloat(KEY_POSICION_Y);
        float z = PlayerPrefs.GetFloat(KEY_POSICION_Z);
        checkpointUltimoGuardado.posicionJugador = new Vector3(x, y, z);

        // Cargar vida
        checkpointUltimoGuardado.vidaJugador = PlayerPrefs.GetFloat(KEY_VIDA);

        // Cargar inventario
        string inventarioString = PlayerPrefs.GetString(KEY_INVENTARIO);
        if (!string.IsNullOrEmpty(inventarioString))
        {
            checkpointUltimoGuardado.inventario = new List<string>(inventarioString.Split(','));
        }

        Debug.Log("[GameManager] Juego cargado correctamente");

        // Aplicar datos cargados
        CargarCheckpoint();
    }

    /// <summary>
    /// Borra todos los datos guardados
    /// </summary>
    public void BorrarDatosGuardados()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[GameManager] Datos guardados eliminados");
    }

    /// <summary>
    /// Verifica si existen datos guardados
    /// </summary>
    public bool HayDatosGuardados()
    {
        return PlayerPrefs.HasKey(KEY_POSICION_X);
    }
    #endregion

    #region Inicialización y Utilidades
    private void InicializarSistema()
    {
        Debug.Log("[GameManager] Sistema inicializado correctamente");

        // Buscar jugador si no está asignado
        if (jugador == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                jugador = player.transform;
            }
        }
    }

    /// <summary>
    /// Reinicia el nivel actual
    /// </summary>
    public void ReiniciarNivel()
    {
        CargarCheckpoint();
    }

    /// <summary>
    /// Asigna la referencia del jugador (llamar desde el script del jugador)
    /// </summary>
    public void AsignarJugador(Transform jugadorTransform)
    {
        jugador = jugadorTransform;
        Debug.Log("[GameManager] Jugador asignado correctamente");
    }
    #endregion

    #region Debug/Testing
    private void Update()
    {
        // Atajos de teclado para testing (ELIMINAR EN BUILD FINAL)
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F1))
        {
            GuardarCheckpoint();
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            CargarCheckpoint();
        }
#endif
    }

    private void OnDrawGizmos()
    {
        // Visualizar posición del último checkpoint guardado
        if (checkpointUltimoGuardado != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(checkpointUltimoGuardado.posicionJugador, 1f);
            Gizmos.DrawLine(
                checkpointUltimoGuardado.posicionJugador,
                checkpointUltimoGuardado.posicionJugador + Vector3.up * 2f
            );
        }
    }
    #endregion
}