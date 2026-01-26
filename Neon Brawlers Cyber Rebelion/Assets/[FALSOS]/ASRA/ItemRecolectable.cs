using UnityEngine;

/// <summary>
/// Script para items recolectables en el mundo
/// </summary>
public class ItemRecolectable : MonoBehaviour
{
    [Header("=== DATOS DEL ITEM ===")]
    [SerializeField] private ItemData itemData;

    [Header("=== CONFIGURACIÓN ===")]
    [SerializeField] private float rangoDeteccion = 2f;
    [SerializeField] private KeyCode teclaRecolectar = KeyCode.E;

    [Header("=== UI OPCIONAL ===")]
    [SerializeField] private bool mostrarTextoProximidad = true;

    private Transform jugador;
    private bool jugadorCerca = false;
    private bool yaRecolectado = false;

    private void Start()
    {
        // Buscar al jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            jugador = playerObj.transform;
        else
            Debug.LogWarning("[ItemRecolectable] No se encontró jugador con tag 'Player'");
    }

    private void Update()
    {
        if (yaRecolectado || jugador == null) return;

        // Detectar proximidad
        float distancia = Vector3.Distance(transform.position, jugador.position);
        jugadorCerca = distancia <= rangoDeteccion;

        // Recolectar
        if (jugadorCerca && Input.GetKeyDown(teclaRecolectar))
        {
            Recolectar();
        }
    }

    private void Recolectar()
    {
        yaRecolectado = true;

        // Agregar al inventario
        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.AgregarItem(itemData);
            Debug.Log($"[ItemRecolectable] ¡Recolectado! {itemData.itemID}");
        }
        else
        {
            Debug.LogError("[ItemRecolectable] No existe InventoryUIManager en la escena");
        }

        // Destruir el objeto
        gameObject.SetActive(false);
        // O si prefieres: Destroy(gameObject);
    }

    // Visualizar rango de detección en el editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = jugadorCerca ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
    }

    // Mostrar texto en Scene View
    private void OnDrawGizmos()
    {
        if (mostrarTextoProximidad && jugadorCerca && !yaRecolectado)
        {
            // Esto solo se ve en Scene View, no en Game
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, rangoDeteccion * 0.5f);
        }
    }
}