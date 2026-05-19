using UnityEngine;
using System.Collections.Generic;

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
    public enum EstadoJuego { Menu, Jugando, Pausado, GameOver }

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
        CambiarEstado(pausar ? EstadoJuego.Pausado : EstadoJuego.Jugando);
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

    [SerializeField] private DatosCheckpoint checkpointActual;   //serialized para pruebas
    [SerializeField] private DatosCheckpoint checkpointUltimoGuardado;   //serialized para pruebas

    private HashSet<string> itemsRecolectadosEnEstaPartida = new HashSet<string>();
    private Dictionary<string, ItemRecolectable> todosLosItemsDelMundo = new Dictionary<string, ItemRecolectable>();

    public void GuardarCheckpoint()
    {
        // Re-buscar jugador por si cambió de escena
        if (jugador == null) BuscarJugador();

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

        //Usar PlayerDamage en lugar de PlayerHealth
        var playerDamage = jugador.GetComponent<PlayerDamage>();
        if (playerDamage != null)
        {
            checkpointActual.vidaJugador = playerDamage.vida;
            checkpointActual.vidaMaximaJugador = 100f; // ajusta si tienes vida máxima definida
        }

        if (InventoryUIManager.Instance != null)
        {
            checkpointActual.inventario = new List<string>(InventoryUIManager.Instance.ObtenerItemsIDs());
            Debug.Log($"[GameManager] Inventario guardado: {checkpointActual.inventario.Count} items");
        }

        checkpointActual.itemsRecolectados = new List<string>(itemsRecolectadosEnEstaPartida);
        Debug.Log($"[GameManager] Items recolectados guardados: {checkpointActual.itemsRecolectados.Count}");

        if (ObjetivoManager.Instance != null)
        {
            checkpointActual.indiceMisionActual = ObjetivoManager.Instance.ObtenerIndiceMisionActual();
        }

        checkpointUltimoGuardado = ClonearCheckpoint(checkpointActual);
        Debug.Log($"[GameManager] Checkpoint guardado - Posición: {checkpointActual.posicionJugador}");

        if (autoGuardarEnCheckpoint)
            GuardarJuegoPersistente();
    }

    private DatosCheckpoint ClonearCheckpoint(DatosCheckpoint origen)
    {
        if (origen == null) return null;

        return new DatosCheckpoint
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
    }

    public void CargarCheckpoint()
    {
        if (checkpointUltimoGuardado == null)
        {
            Debug.LogWarning("[GameManager] No hay checkpoint guardado para cargar");
            return;
        }

        //Re-buscar jugador por si cambió de escena
        if (jugador == null) BuscarJugador();

        if (jugador == null)
        {
            Debug.LogError("[GameManager] No hay referencia al jugador para cargar checkpoint");
            return;
        }

        jugador.position = checkpointUltimoGuardado.posicionJugador;
        jugador.rotation = checkpointUltimoGuardado.rotacionJugador;

        //Usar PlayerDamage en lugar de PlayerHealth
        var playerDamage = jugador.GetComponent<PlayerDamage>();
        if (playerDamage != null)
        {
            playerDamage.vida = checkpointUltimoGuardado.vidaJugador;
            Debug.Log($"[GameManager] Vida restaurada: {playerDamage.vida}");
        }

        if (InventoryUIManager.Instance != null && checkpointUltimoGuardado.inventario != null)
        {
            InventoryUIManager.Instance.LimpiarInventario();
            foreach (string itemID in checkpointUltimoGuardado.inventario)
                InventoryUIManager.Instance.AgregarItemPorID(itemID);

            Debug.Log($"[GameManager] Inventario restaurado: {checkpointUltimoGuardado.inventario.Count} items");
        }

        if (checkpointUltimoGuardado.itemsRecolectados != null)
            RestaurarItemsRecolectados(checkpointUltimoGuardado.itemsRecolectados);

        RestaurarEstadoFisicoItems();

        if (ObjetivoManager.Instance != null)
            ObjetivoManager.Instance.CargarEstadoMision(checkpointUltimoGuardado.indiceMisionActual);

        Debug.Log("[GameManager] Checkpoint cargado completamente");
    }
    #endregion

    #region Sistema de Items Recolectados
    public void RegistrarItemRecolectado(string itemID, string nombreObjeto)
    {
        string identificador = $"{itemID}_{nombreObjeto}";
        itemsRecolectadosEnEstaPartida.Add(identificador);
        Debug.Log($"[GameManager] Item registrado como recolectado: {identificador}");
    }

    public bool ItemFueRecolectado(string itemID, string nombreObjeto)
    {
        string identificador = $"{itemID}_{nombreObjeto}";
        return itemsRecolectadosEnEstaPartida.Contains(identificador);
    }

    private void RestaurarItemsRecolectados(List<string> itemsRecolectados)
    {
        itemsRecolectadosEnEstaPartida.Clear();
        foreach (string id in itemsRecolectados)
            itemsRecolectadosEnEstaPartida.Add(id);

        Debug.Log($"[GameManager] Items recolectados restaurados: {itemsRecolectadosEnEstaPartida.Count}");
    }

    public void RegistrarItemEnMundo(string identificador, ItemRecolectable item)
    {
        if (string.IsNullOrEmpty(identificador)) return;

        if (todosLosItemsDelMundo.ContainsKey(identificador))
            todosLosItemsDelMundo[identificador] = item;
        else
            todosLosItemsDelMundo.Add(identificador, item);

        Debug.Log($"[GameManager] Item registrado en mundo: {identificador}");
    }

    public void DesregistrarItemEnMundo(string identificador)
    {
        if (todosLosItemsDelMundo.ContainsKey(identificador))
            todosLosItemsDelMundo.Remove(identificador);
    }

    private void RestaurarEstadoFisicoItems()
    {
        if (checkpointUltimoGuardado == null) return;

        int reactivados = 0, desactivados = 0;

        foreach (var kvp in todosLosItemsDelMundo)
        {
            ItemRecolectable item = kvp.Value;
            if (item == null || item.gameObject == null) continue;

            bool estabaRecolectado = checkpointUltimoGuardado.itemsRecolectados.Contains(kvp.Key);

            if (estabaRecolectado)
            {
                if (item.gameObject.activeSelf) { item.gameObject.SetActive(false); desactivados++; }
            }
            else
            {
                if (!item.gameObject.activeSelf) { item.gameObject.SetActive(true); item.ResetearEstado(); reactivados++; }
            }
        }

        Debug.Log($"[GameManager] Restauración: {reactivados} reactivados, {desactivados} desactivados");
    }
    #endregion

    #region Guardado Persistente
    public void GuardarJuegoPersistente()
    {
        if (checkpointUltimoGuardado == null) return;

        PlayerPrefs.SetFloat(KEY_POSICION_X, checkpointUltimoGuardado.posicionJugador.x);
        PlayerPrefs.SetFloat(KEY_POSICION_Y, checkpointUltimoGuardado.posicionJugador.y);
        PlayerPrefs.SetFloat(KEY_POSICION_Z, checkpointUltimoGuardado.posicionJugador.z);
        PlayerPrefs.SetFloat(KEY_ROTACION_X, checkpointUltimoGuardado.rotacionJugador.x);
        PlayerPrefs.SetFloat(KEY_ROTACION_Y, checkpointUltimoGuardado.rotacionJugador.y);
        PlayerPrefs.SetFloat(KEY_ROTACION_Z, checkpointUltimoGuardado.rotacionJugador.z);
        PlayerPrefs.SetFloat(KEY_ROTACION_W, checkpointUltimoGuardado.rotacionJugador.w);
        PlayerPrefs.SetFloat(KEY_VIDA, checkpointUltimoGuardado.vidaJugador);
        PlayerPrefs.SetFloat(KEY_VIDA_MAXIMA, checkpointUltimoGuardado.vidaMaximaJugador);
        PlayerPrefs.SetString(KEY_INVENTARIO, string.Join(",", checkpointUltimoGuardado.inventario));
        PlayerPrefs.SetString(KEY_ITEMS_RECOLECTADOS, string.Join(",", checkpointUltimoGuardado.itemsRecolectados));
        PlayerPrefs.SetInt(KEY_MISION_ACTUAL, checkpointUltimoGuardado.indiceMisionActual);
        PlayerPrefs.SetInt(KEY_HAY_GUARDADO, 1);
        PlayerPrefs.Save();

        Debug.Log("[GameManager] Juego guardado persistentemente");
    }

    public void CargarJuegoPersistente()
    {
        if (!HayDatosGuardados()) return;

        checkpointUltimoGuardado = new DatosCheckpoint
        {
            posicionJugador = new Vector3(
                PlayerPrefs.GetFloat(KEY_POSICION_X),
                PlayerPrefs.GetFloat(KEY_POSICION_Y),
                PlayerPrefs.GetFloat(KEY_POSICION_Z)
            ),
            rotacionJugador = new Quaternion(
                PlayerPrefs.GetFloat(KEY_ROTACION_X),
                PlayerPrefs.GetFloat(KEY_ROTACION_Y),
                PlayerPrefs.GetFloat(KEY_ROTACION_Z),
                PlayerPrefs.GetFloat(KEY_ROTACION_W)
            ),
            vidaJugador = PlayerPrefs.GetFloat(KEY_VIDA),
            vidaMaximaJugador = PlayerPrefs.GetFloat(KEY_VIDA_MAXIMA),
            indiceMisionActual = PlayerPrefs.GetInt(KEY_MISION_ACTUAL, 0)
        };

        string inventarioStr = PlayerPrefs.GetString(KEY_INVENTARIO);
        if (!string.IsNullOrEmpty(inventarioStr))
            checkpointUltimoGuardado.inventario = new List<string>(inventarioStr.Split(','));

        string itemsStr = PlayerPrefs.GetString(KEY_ITEMS_RECOLECTADOS, "");
        if (!string.IsNullOrEmpty(itemsStr))
            checkpointUltimoGuardado.itemsRecolectados = new List<string>(itemsStr.Split(','));

        Debug.Log("[GameManager] Juego cargado desde guardado persistente");
        CargarCheckpoint();
    }

    public void BorrarDatosGuardados()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[GameManager] Datos guardados eliminados");
    }

    public bool HayDatosGuardados()
    {
        return PlayerPrefs.GetInt(KEY_HAY_GUARDADO, 0) == 1;
    }
    #endregion

    #region Auto-Guardado
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && autoGuardarAlCerrar) GuardarJuegoPersistente();
    }

    private void OnApplicationQuit()
    {
        if (autoGuardarAlCerrar) GuardarJuegoPersistente();
    }
    #endregion

    #region Inicialización
    private void InicializarSistema()
    {
        Debug.Log("[GameManager] Sistema inicializado correctamente");
        BuscarJugador();

        if (cargarCheckpointAlIniciar && HayDatosGuardados())
            StartCoroutine(CargarCheckpointAlIniciarCorrutina());
        else
            StartCoroutine(InicializarPrimeraMisionCorrutina());
    }

    // FIX: Método separado para buscar jugador, reutilizable
    private void BuscarJugador()
    {
        if (jugador != null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            jugador = player.transform;
            Debug.Log("[GameManager] Jugador encontrado automáticamente");
        }
        else
        {
            Debug.LogWarning("[GameManager] No se encontró objeto con tag 'Player'");
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
            ObjetivoManager.Instance.InicializarPrimeraMision();
    }

    public void ReiniciarNivel() { CargarCheckpoint(); }

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
        if (Input.GetKeyDown(KeyCode.F1)) GuardarCheckpoint();
        if (Input.GetKeyDown(KeyCode.F2)) CargarCheckpoint();
        if (Input.GetKeyDown(KeyCode.F3)) BorrarDatosGuardados();
#endif
    }

    private void OnDrawGizmos()
    {
        if (checkpointUltimoGuardado != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(checkpointUltimoGuardado.posicionJugador, 1f);
        }
    }
    #endregion

    #region Reseteo
    public void LimpiarRegistroItems()
    {
        itemsRecolectadosEnEstaPartida.Clear();
        todosLosItemsDelMundo.Clear();
        Debug.Log("[GameManager] Registro de items limpiado");
    }

    public void BorrarCheckpoint()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[GameManager] Checkpoint borrado");
    }
    #endregion
}