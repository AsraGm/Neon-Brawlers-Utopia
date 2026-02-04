using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ✅ VERSIÓN ACTUALIZADA - GameManager con restauración completa de items físicos
/// NUEVAS CARACTERÍSTICAS:
/// - Registro de todos los ItemRecolectable del mundo
/// - Restauración física de items al cargar checkpoint
/// - Sistema completo de persistencia de estado del mundo
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }

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

    #region Constantes de PlayerPrefs
    private const string KEY_HAY_GUARDADO = "HayDatosGuardados";
    private const string KEY_POSICION_X = "PosicionX";
    private const string KEY_POSICION_Y = "PosicionY";
    private const string KEY_POSICION_Z = "PosicionZ";
    private const string KEY_ROTACION_X = "RotacionX";
    private const string KEY_ROTACION_Y = "RotacionY";
    private const string KEY_ROTACION_Z = "RotacionZ";
    private const string KEY_ROTACION_W = "RotacionW";
    private const string KEY_VIDA = "Vida";
    private const string KEY_VIDA_MAXIMA = "VidaMaxima";
    private const string KEY_INVENTARIO = "Inventario";
    private const string KEY_ITEMS_RECOLECTADOS = "ItemsRecolectados";
    private const string KEY_MISION_ACTUAL = "MisionActual";
    #endregion

    #region Control de Estado del Juego
    public enum EstadoJuego
    {
        Menu,
        Jugando,
        Pausado,
        GameOver
    }

    [Header("Estado del Juego")]
    public EstadoJuego estadoActual = EstadoJuego.Menu;

    public delegate void CambioEstado(EstadoJuego nuevoEstado);
    public event CambioEstado OnCambioEstado;

    public void CambiarEstado(EstadoJuego nuevoEstado)
    {
        if (estadoActual == nuevoEstado) return;

        estadoActual = nuevoEstado;
        OnCambioEstado?.Invoke(nuevoEstado);

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

    public void PausarJuego(bool pausar)
    {
        if (pausar)
            CambiarEstado(EstadoJuego.Pausado);
        else
            CambiarEstado(EstadoJuego.Jugando);
    }
    #endregion

    #region Sistema de Checkpoints
    [System.Serializable]
    public class DatosCheckpoint
    {
        public Vector3 posicionJugador;
        public Quaternion rotacionJugador;
        public float vidaJugador;
        public float vidaMaximaJugador;
        public List<string> inventario = new List<string>();
        public Dictionary<string, bool> estadoObjetos = new Dictionary<string, bool>();
        public List<string> itemsRecolectados = new List<string>();
        public int indiceMisionActual = 0;

        public DatosCheckpoint()
        {
            inventario = new List<string>();
            estadoObjetos = new Dictionary<string, bool>();
            itemsRecolectados = new List<string>();
        }
    }

    [Header("Sistema de Checkpoints")]
    [SerializeField] private Transform jugador;
    [SerializeField] private bool autoGuardarEnCheckpoint = true;

    [Header("Guardado Automático")]
    [SerializeField] private bool cargarCheckpointAlIniciar = true;
    [SerializeField] private bool autoGuardarAlCerrar = true;

    private DatosCheckpoint checkpointActual;
    private DatosCheckpoint checkpointUltimoGuardado;

    // ✅ NUEVO: Variables para sistema de items recolectados
    private HashSet<string> itemsRecolectadosEnEstaPartida = new HashSet<string>();

    // ✅ NUEVO: Diccionario de todos los items físicos del mundo
    private Dictionary<string, ItemRecolectable> todosLosItemsDelMundo = new Dictionary<string, ItemRecolectable>();

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

        // Guardar vida del jugador
        var playerHealth = jugador.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            checkpointActual.vidaJugador = playerHealth.vidaActual;
            checkpointActual.vidaMaximaJugador = playerHealth.vidaMaxima;
        }

        // Guardar inventario completo
        if (InventoryUIManager.Instance != null)
        {
            checkpointActual.inventario = new List<string>(InventoryUIManager.Instance.ObtenerItemsIDs());
            Debug.Log($"[GameManager] Inventario guardado: {checkpointActual.inventario.Count} items");
        }

        // ✅ NUEVO: Guardar lista de items recolectados del mundo
        checkpointActual.itemsRecolectados = new List<string>(itemsRecolectadosEnEstaPartida);
        Debug.Log($"[GameManager] Items recolectados guardados: {checkpointActual.itemsRecolectados.Count}");

        // Guardar misión actual
        if (ObjetivoManager.Instance != null)
        {
            checkpointActual.indiceMisionActual = ObjetivoManager.Instance.ObtenerIndiceMisionActual();
            Debug.Log($"[GameManager] Misión actual guardada: #{checkpointActual.indiceMisionActual}");
        }

        // Clonar para el último guardado
        checkpointUltimoGuardado = ClonearCheckpoint(checkpointActual);

        Debug.Log($"[GameManager] ✅ Checkpoint guardado - Posición: {checkpointActual.posicionJugador}, Vida: {checkpointActual.vidaJugador}/{checkpointActual.vidaMaximaJugador}");

        // Guardar persistente
        if (autoGuardarEnCheckpoint)
        {
            GuardarJuegoPersistente();
        }
    }

    /// <summary>
    /// Clona un checkpoint de forma profunda
    /// </summary>
    private DatosCheckpoint ClonearCheckpoint(DatosCheckpoint origen)
    {
        if (origen == null)
        {
            Debug.LogWarning("[GameManager] Intentando clonar checkpoint null");
            return null;
        }

        var clon = new DatosCheckpoint
        {
            posicionJugador = origen.posicionJugador,
            rotacionJugador = origen.rotacionJugador,
            vidaJugador = origen.vidaJugador,
            vidaMaximaJugador = origen.vidaMaximaJugador,
            inventario = new List<string>(origen.inventario),
            estadoObjetos = new Dictionary<string, bool>(origen.estadoObjetos),
            itemsRecolectados = new List<string>(origen.itemsRecolectados),
            indiceMisionActual = origen.indiceMisionActual
        };

        return clon;
    }

    /// <summary>
    /// Carga el último checkpoint guardado CON RESTAURACIÓN COMPLETA
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

        // Restaurar vida AL VALOR DEL CHECKPOINT
        var playerHealth = jugador.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.EstablecerVida(
                checkpointUltimoGuardado.vidaJugador,
                checkpointUltimoGuardado.vidaMaximaJugador
            );
            playerHealth.ResetearEstadoMuerte();
            Debug.Log($"[GameManager] Vida restaurada: {checkpointUltimoGuardado.vidaJugador}/{checkpointUltimoGuardado.vidaMaximaJugador}");
        }

        // Restaurar inventario
        if (InventoryUIManager.Instance != null && checkpointUltimoGuardado.inventario != null)
        {
            InventoryUIManager.Instance.LimpiarInventario();

            foreach (string itemID in checkpointUltimoGuardado.inventario)
            {
                InventoryUIManager.Instance.AgregarItemPorID(itemID);
            }

            Debug.Log($"[GameManager] Inventario restaurado: {checkpointUltimoGuardado.inventario.Count} items");
        }

        // ✅ NUEVO: Restaurar items recolectados del mundo
        if (checkpointUltimoGuardado.itemsRecolectados != null)
        {
            RestaurarItemsRecolectados(checkpointUltimoGuardado.itemsRecolectados);
        }

        // ✅ NUEVO: Restaurar estado FÍSICO de los objetos en el mundo
        RestaurarEstadoFisicoItems();

        // Restaurar misión
        if (ObjetivoManager.Instance != null)
        {
            ObjetivoManager.Instance.CargarEstadoMision(checkpointUltimoGuardado.indiceMisionActual);
        }

        Debug.Log("[GameManager] 🔄 Checkpoint cargado completamente");
    }

    // ==========================================
    // ✅ NUEVO: Métodos para sistema de items recolectados
    // ==========================================

    /// <summary>
    /// Registra un item como recolectado
    /// </summary>
    public void RegistrarItemRecolectado(string itemID, string nombreObjeto)
    {
        string identificador = $"{itemID}_{nombreObjeto}";
        itemsRecolectadosEnEstaPartida.Add(identificador);

        Debug.Log($"[GameManager] Item registrado como recolectado: {identificador}");
    }

    /// <summary>
    /// Verifica si un item ya fue recolectado
    /// </summary>
    public bool ItemFueRecolectado(string itemID, string nombreObjeto)
    {
        string identificador = $"{itemID}_{nombreObjeto}";
        return itemsRecolectadosEnEstaPartida.Contains(identificador);
    }

    /// <summary>
    /// Restaura la lista de items recolectados desde un checkpoint
    /// </summary>
    private void RestaurarItemsRecolectados(List<string> itemsRecolectados)
    {
        itemsRecolectadosEnEstaPartida.Clear();

        foreach (string identificador in itemsRecolectados)
        {
            itemsRecolectadosEnEstaPartida.Add(identificador);
        }

        Debug.Log($"[GameManager] Items recolectados restaurados: {itemsRecolectadosEnEstaPartida.Count}");
    }

    // ==========================================
    // ✅ NUEVO: Sistema de Restauración de Items Físicos
    // ==========================================

    /// <summary>
    /// Registra un ItemRecolectable en el diccionario global
    /// </summary>
    public void RegistrarItemEnMundo(string identificador, ItemRecolectable item)
    {
        if (string.IsNullOrEmpty(identificador))
        {
            Debug.LogWarning("[GameManager] Intentando registrar item con identificador vacío");
            return;
        }

        if (todosLosItemsDelMundo.ContainsKey(identificador))
        {
            Debug.LogWarning($"[GameManager] Item '{identificador}' ya estaba registrado. Reemplazando...");
            todosLosItemsDelMundo[identificador] = item;
        }
        else
        {
            todosLosItemsDelMundo.Add(identificador, item);
            Debug.Log($"[GameManager] Item registrado en mundo: {identificador}");
        }
    }

    /// <summary>
    /// Elimina un item del registro
    /// </summary>
    public void DesregistrarItemEnMundo(string identificador)
    {
        if (todosLosItemsDelMundo.ContainsKey(identificador))
        {
            todosLosItemsDelMundo.Remove(identificador);
            Debug.Log($"[GameManager] Item desregistrado: {identificador}");
        }
    }

    /// <summary>
    /// Restaura el estado físico de TODOS los items del mundo basándose en el checkpoint
    /// </summary>
    private void RestaurarEstadoFisicoItems()
    {
        if (checkpointUltimoGuardado == null)
        {
            Debug.LogWarning("[GameManager] No hay checkpoint para restaurar items físicos");
            return;
        }

        Debug.Log("[GameManager] 🔄 Iniciando restauración de items físicos...");
        Debug.Log($"[GameManager] Total items en mundo: {todosLosItemsDelMundo.Count}");
        Debug.Log($"[GameManager] Items en checkpoint: {checkpointUltimoGuardado.itemsRecolectados.Count}");

        int itemsReactivados = 0;
        int itemsDesactivados = 0;

        foreach (var kvp in todosLosItemsDelMundo)
        {
            string identificador = kvp.Key;
            ItemRecolectable item = kvp.Value;

            if (item == null || item.gameObject == null)
            {
                Debug.LogWarning($"[GameManager] Item '{identificador}' es null, saltando...");
                continue;
            }

            // Verificar si este item estaba recolectado en el checkpoint
            bool estabaRecolectadoEnCheckpoint = checkpointUltimoGuardado.itemsRecolectados.Contains(identificador);

            if (estabaRecolectadoEnCheckpoint)
            {
                // Este item SÍ estaba recolectado → DESACTIVAR
                if (item.gameObject.activeSelf)
                {
                    item.gameObject.SetActive(false);
                    itemsDesactivados++;
                    Debug.Log($"[GameManager] ❌ Item desactivado: {identificador}");
                }
            }
            else
            {
                // Este item NO estaba recolectado → ACTIVAR
                if (!item.gameObject.activeSelf)
                {
                    item.gameObject.SetActive(true);
                    item.ResetearEstado();
                    itemsReactivados++;
                    Debug.Log($"[GameManager] ✅ Item reactivado: {identificador}");
                }
            }
        }

        Debug.Log($"[GameManager] 🔄 Restauración completa: {itemsReactivados} items reactivados, {itemsDesactivados} desactivados");
    }
    #endregion

    #region Guardado Persistente
    public void GuardarJuegoPersistente()
    {
        if (checkpointUltimoGuardado == null)
        {
            Debug.LogWarning("[GameManager] No hay checkpoint para guardar");
            return;
        }

        // Guardar posición
        PlayerPrefs.SetFloat(KEY_POSICION_X, checkpointUltimoGuardado.posicionJugador.x);
        PlayerPrefs.SetFloat(KEY_POSICION_Y, checkpointUltimoGuardado.posicionJugador.y);
        PlayerPrefs.SetFloat(KEY_POSICION_Z, checkpointUltimoGuardado.posicionJugador.z);

        // Guardar rotación
        PlayerPrefs.SetFloat(KEY_ROTACION_X, checkpointUltimoGuardado.rotacionJugador.x);
        PlayerPrefs.SetFloat(KEY_ROTACION_Y, checkpointUltimoGuardado.rotacionJugador.y);
        PlayerPrefs.SetFloat(KEY_ROTACION_Z, checkpointUltimoGuardado.rotacionJugador.z);
        PlayerPrefs.SetFloat(KEY_ROTACION_W, checkpointUltimoGuardado.rotacionJugador.w);

        // Guardar vida
        PlayerPrefs.SetFloat(KEY_VIDA, checkpointUltimoGuardado.vidaJugador);
        PlayerPrefs.SetFloat(KEY_VIDA_MAXIMA, checkpointUltimoGuardado.vidaMaximaJugador);

        // Guardar inventario
        string inventarioString = string.Join(",", checkpointUltimoGuardado.inventario);
        PlayerPrefs.SetString(KEY_INVENTARIO, inventarioString);

        // Guardar items recolectados
        string itemsRecolectadosString = string.Join(",", checkpointUltimoGuardado.itemsRecolectados);
        PlayerPrefs.SetString(KEY_ITEMS_RECOLECTADOS, itemsRecolectadosString);

        // Guardar misión actual
        PlayerPrefs.SetInt(KEY_MISION_ACTUAL, checkpointUltimoGuardado.indiceMisionActual);

        // Marcar que hay datos guardados
        PlayerPrefs.SetInt(KEY_HAY_GUARDADO, 1);

        PlayerPrefs.Save();
        Debug.Log("[GameManager] 💾 Juego guardado persistentemente");
    }

    public void CargarJuegoPersistente()
    {
        if (!HayDatosGuardados())
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

        // Cargar rotación
        float rotX = PlayerPrefs.GetFloat(KEY_ROTACION_X);
        float rotY = PlayerPrefs.GetFloat(KEY_ROTACION_Y);
        float rotZ = PlayerPrefs.GetFloat(KEY_ROTACION_Z);
        float rotW = PlayerPrefs.GetFloat(KEY_ROTACION_W);
        checkpointUltimoGuardado.rotacionJugador = new Quaternion(rotX, rotY, rotZ, rotW);

        // Cargar vida
        checkpointUltimoGuardado.vidaJugador = PlayerPrefs.GetFloat(KEY_VIDA);
        checkpointUltimoGuardado.vidaMaximaJugador = PlayerPrefs.GetFloat(KEY_VIDA_MAXIMA);

        // Cargar inventario
        string inventarioString = PlayerPrefs.GetString(KEY_INVENTARIO);
        if (!string.IsNullOrEmpty(inventarioString))
        {
            checkpointUltimoGuardado.inventario = new List<string>(inventarioString.Split(','));
        }

        // Cargar items recolectados
        string itemsRecolectadosString = PlayerPrefs.GetString(KEY_ITEMS_RECOLECTADOS, "");
        if (!string.IsNullOrEmpty(itemsRecolectadosString))
        {
            checkpointUltimoGuardado.itemsRecolectados = new List<string>(itemsRecolectadosString.Split(','));
        }

        // Cargar misión actual
        checkpointUltimoGuardado.indiceMisionActual = PlayerPrefs.GetInt(KEY_MISION_ACTUAL, 0);

        Debug.Log("[GameManager] 📂 Juego cargado desde guardado persistente");

        // Aplicar datos cargados
        CargarCheckpoint();
    }

    public void BorrarDatosGuardados()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[GameManager] 🗑️ Datos guardados eliminados");
    }

    public bool HayDatosGuardados()
    {
        return PlayerPrefs.GetInt(KEY_HAY_GUARDADO, 0) == 1;
    }
    #endregion

    #region Auto-Guardado
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && autoGuardarAlCerrar)
        {
            Debug.Log("[GameManager] 💾 Auto-guardado: Aplicación pausada");
            GuardarJuegoPersistente();
        }
    }

    private void OnApplicationQuit()
    {
        if (autoGuardarAlCerrar)
        {
            Debug.Log("[GameManager] 💾 Auto-guardado: Aplicación cerrándose");
            GuardarJuegoPersistente();
        }
    }
    #endregion

    #region Inicialización
    private void InicializarSistema()
    {
        Debug.Log("[GameManager] Sistema inicializado correctamente");

        if (jugador == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                jugador = player.transform;
            }
            else
            {
                Debug.LogWarning("[GameManager] No se encontró objeto con tag 'Player'");
            }
        }

        if (cargarCheckpointAlIniciar && HayDatosGuardados())
        {
            StartCoroutine(CargarCheckpointAlIniciarCorrutina());
        }
        else
        {
            StartCoroutine(InicializarPrimeraMisionCorrutina());
        }
    }

    private System.Collections.IEnumerator CargarCheckpointAlIniciarCorrutina()
    {
        yield return null;
        CargarJuegoPersistente();
    }

    private System.Collections.IEnumerator InicializarPrimeraMisionCorrutina()
    {
        yield return null;
        if (ObjetivoManager.Instance != null)
        {
            ObjetivoManager.Instance.InicializarPrimeraMision();
        }
    }

    public void ReiniciarNivel()
    {
        CargarCheckpoint();
    }

    public void AsignarJugador(Transform jugadorTransform)
    {
        jugador = jugadorTransform;
        Debug.Log("[GameManager] Jugador asignado correctamente");
    }
    #endregion

    #region Debug
    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F1))
        {
            GuardarCheckpoint();
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            CargarCheckpoint();
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            BorrarDatosGuardados();
        }
#endif
    }

    private void OnDrawGizmos()
    {
        if (checkpointUltimoGuardado != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(checkpointUltimoGuardado.posicionJugador, 1f);
            Gizmos.DrawLine(
                checkpointUltimoGuardado.posicionJugador,
                checkpointUltimoGuardado.posicionJugador + Vector3.up * 2f
            );

            Vector3 forward = checkpointUltimoGuardado.rotacionJugador * Vector3.forward;
            Gizmos.DrawLine(
                checkpointUltimoGuardado.posicionJugador,
                checkpointUltimoGuardado.posicionJugador + forward * 1.5f
            );
        }
    }
    #endregion
}