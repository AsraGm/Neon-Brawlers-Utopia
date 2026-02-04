using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ✅ NUEVO: ItemDatabase - Base de datos centralizada de todos los items
/// Este ScriptableObject contiene referencias a TODOS los ItemData del juego
/// Permite cargar items por ID para el sistema de guardado/carga
/// 
/// CÓMO USAR:
/// 1. Click derecho en Project > Create > Inventario > Item Database
/// 2. Arrastra TODOS los ItemData a la lista "todosLosItems"
/// 3. Asigna este ItemDatabase en InventoryUIManager (Inspector)
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventario/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Header("=== BASE DE DATOS DE ITEMS ===")]
    [Tooltip("IMPORTANTE: Arrastra aquí TODOS los ItemData del juego")]
    [SerializeField] private List<ItemData> todosLosItems = new List<ItemData>();

    [Header("=== DEBUG ===")]
    [SerializeField] private bool mostrarLogsDetallados = false;

    /// <summary>
    /// Diccionario interno para búsqueda rápida (se genera automáticamente)
    /// </summary>
    private Dictionary<string, ItemData> diccionarioItems;

    /// <summary>
    /// Inicializa el diccionario (se llama automáticamente al acceder)
    /// </summary>
    private void InicializarDiccionario()
    {
        if (diccionarioItems != null) return;

        diccionarioItems = new Dictionary<string, ItemData>();

        foreach (var item in todosLosItems)
        {
            if (item == null)
            {
                Debug.LogWarning("[ItemDatabase] Hay un item NULL en la lista todosLosItems");
                continue;
            }

            if (string.IsNullOrEmpty(item.itemID))
            {
                Debug.LogWarning($"[ItemDatabase] Item '{item.name}' no tiene itemID asignado");
                continue;
            }

            if (diccionarioItems.ContainsKey(item.itemID))
            {
                Debug.LogError($"[ItemDatabase] ⚠️ ITEM DUPLICADO: '{item.itemID}' aparece más de una vez. Solo se usará el primero.");
                continue;
            }

            diccionarioItems[item.itemID] = item;
        }

        if (mostrarLogsDetallados)
        {
            Debug.Log($"[ItemDatabase] Diccionario inicializado con {diccionarioItems.Count} items");
        }
    }

    /// <summary>
    /// Obtiene un ItemData por su ID
    /// </summary>
    /// <param name="itemID">ID único del item</param>
    /// <returns>ItemData si se encuentra, null si no existe</returns>
    public ItemData ObtenerItemPorID(string itemID)
    {
        // Inicializar diccionario si es necesario
        InicializarDiccionario();

        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogWarning("[ItemDatabase] Intentando obtener item con ID vacío");
            return null;
        }

        if (diccionarioItems.TryGetValue(itemID, out ItemData item))
        {
            if (mostrarLogsDetallados)
            {
                Debug.Log($"[ItemDatabase] ✅ Item encontrado: {itemID} -> {item.nombreDisplay}");
            }
            return item;
        }
        else
        {
            Debug.LogWarning($"[ItemDatabase] ❌ Item con ID '{itemID}' NO ENCONTRADO en la base de datos");
            return null;
        }
    }

    /// <summary>
    /// Verifica si existe un item con el ID dado
    /// </summary>
    public bool ExisteItem(string itemID)
    {
        InicializarDiccionario();
        return diccionarioItems.ContainsKey(itemID);
    }

    /// <summary>
    /// Obtiene la lista completa de todos los items
    /// </summary>
    public List<ItemData> ObtenerTodosLosItems()
    {
        return new List<ItemData>(todosLosItems);
    }

    /// <summary>
    /// Obtiene todos los items de un tipo específico
    /// </summary>
    public List<ItemData> ObtenerItemsPorTipo(TipoItem tipo)
    {
        List<ItemData> itemsFiltrados = new List<ItemData>();

        foreach (var item in todosLosItems)
        {
            if (item != null && item.tipo == tipo)
            {
                itemsFiltrados.Add(item);
            }
        }

        return itemsFiltrados;
    }

    /// <summary>
    /// Obtiene la cantidad total de items en la base de datos
    /// </summary>
    public int ObtenerCantidadTotal()
    {
        return todosLosItems.Count;
    }

    /// <summary>
    /// Valida la base de datos (útil para debugging)
    /// </summary>
    [ContextMenu("Validar Base de Datos")]
    public void ValidarBaseDatos()
    {
        Debug.Log("=== VALIDANDO ITEM DATABASE ===");

        int itemsValidos = 0;
        int itemsNulos = 0;
        int itemsSinID = 0;
        int itemsDuplicados = 0;
        HashSet<string> idsEncontrados = new HashSet<string>();

        foreach (var item in todosLosItems)
        {
            if (item == null)
            {
                itemsNulos++;
                Debug.LogWarning("[ItemDatabase] ⚠️ Item NULL encontrado en la lista");
                continue;
            }

            if (string.IsNullOrEmpty(item.itemID))
            {
                itemsSinID++;
                Debug.LogWarning($"[ItemDatabase] ⚠️ Item '{item.name}' no tiene itemID asignado");
                continue;
            }

            if (idsEncontrados.Contains(item.itemID))
            {
                itemsDuplicados++;
                Debug.LogError($"[ItemDatabase] ❌ DUPLICADO: itemID '{item.itemID}' usado en múltiples items");
                continue;
            }

            idsEncontrados.Add(item.itemID);
            itemsValidos++;
        }

        Debug.Log($"[ItemDatabase] === RESUMEN ===");
        Debug.Log($"[ItemDatabase] ✅ Items válidos: {itemsValidos}");
        if (itemsNulos > 0) Debug.LogWarning($"[ItemDatabase] ⚠️ Items nulos: {itemsNulos}");
        if (itemsSinID > 0) Debug.LogWarning($"[ItemDatabase] ⚠️ Items sin ID: {itemsSinID}");
        if (itemsDuplicados > 0) Debug.LogError($"[ItemDatabase] ❌ Items duplicados: {itemsDuplicados}");
        Debug.Log($"[ItemDatabase] Total en lista: {todosLosItems.Count}");
    }

    /// <summary>
    /// Limpia el diccionario (fuerza reconstrucción)
    /// </summary>
    [ContextMenu("Limpiar Cache")]
    public void LimpiarCache()
    {
        diccionarioItems = null;
        Debug.Log("[ItemDatabase] Cache limpiado. El diccionario se reconstruirá en el próximo acceso.");
    }
}
