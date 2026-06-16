using UnityEngine;
using UnityEngine.Events;

public class ItemRecolectable : MonoBehaviour
{
    [Header("=== DATOS DEL ITEM ===")]
    [SerializeField] private ItemData itemData;

    [Header("=== CONFIGURACIÓN ===")]
    [SerializeField] private KeyCode teclaRecolectar = KeyCode.E;
    [SerializeField] private bool requiereSlowMo = false;

    public UnityEvent ItemCollected;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool logsDetallados = false;

    private bool jugadorCerca = false;
    private bool yaRecolectado = false;

    private void Start()
    {
        if (itemData == null)
        {
            Debug.LogError($"[ItemRecolectable] '{gameObject.name}' no tiene ItemData asignado.");
            enabled = false;
            return;
        }

        // Asegurarse de que el collider es trigger
        //Collider col = GetComponent<Collider>();
        //if (col != null)
        //    col.isTrigger = true;
        //else
        //    Debug.LogError($"[ItemRecolectable] '{gameObject.name}' no tiene Collider. Agrégale uno.");

        if (GameManager.Instance != null)
        {
            string id = ObtenerIdentificadorUnico();
            GameManager.Instance.RegistrarItemEnMundo(id, this);
            VerificarEstadoInicial();
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.DesregistrarItemEnMundo(ObtenerIdentificadorUnico());
    }

    private void VerificarEstadoInicial()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.ItemFueRecolectado(itemData.itemID, gameObject.name))
        {
            yaRecolectado = true;
            gameObject.SetActive(false);
        }
    }

    // ✅ SOLUCIÓN: usar trigger en lugar de distancia
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        jugadorCerca = true;

        if (logsDetallados)
            Debug.Log($"[ItemRecolectable] Jugador entró en rango de '{gameObject.name}' - Presiona {teclaRecolectar}");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        jugadorCerca = false;

        if (logsDetallados)
            Debug.Log($"[ItemRecolectable] Jugador salió del rango de '{gameObject.name}'");
    }

    private void Update()
    {
        if (yaRecolectado || !jugadorCerca) return;

        if (Input.GetKeyDown(teclaRecolectar))
        {
            if (requiereSlowMo && Time.timeScale >= 1f)
            {
                if (logsDetallados)
                    Debug.Log($"[ItemRecolectable] '{gameObject.name}' está desestabilizado.");

                //Efecto o sonido de glitcheo o retro que no lo recolecto
                return;
            }

            Recolectar();
        }
    }

    private void Recolectar()
    {
        yaRecolectado = true;
        ItemCollected?.Invoke();

        if (GameManager.Instance != null)
            GameManager.Instance.RegistrarItemRecolectado(itemData.itemID, gameObject.name);

        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.AgregarItem(itemData);
            Debug.Log($"[ItemRecolectable] ¡Recolectado! {itemData.nombreDisplay}");
        }
        else
        {
            Debug.LogError("[ItemRecolectable] No existe InventoryUIManager en la escena");
        }

        gameObject.SetActive(false);
    }

    public string ObtenerIdentificadorUnico()
    {
        if (itemData == null) return $"NULL_{gameObject.name}";
        return $"{itemData.itemID}_{gameObject.name}";
    }

    public void ResetearEstado()
    {
        yaRecolectado = false;
        jugadorCerca = false;
    }

    public ItemData ObtenerItemData() => itemData;
    public bool EstaRecolectado() => yaRecolectado;
}