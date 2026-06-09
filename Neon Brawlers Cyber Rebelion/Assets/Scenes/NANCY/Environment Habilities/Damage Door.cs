using UnityEngine;

public class DamageDoor : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 100;

    private InteractableDoor door;

    private void Start()
    {
        door = GetComponentInParent<InteractableDoor>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (door == null) return;

        if (door.isStunned) return;

        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.RecibirDanio(damage);
            else
                Debug.LogWarning("PlayerHealth no encontrado en " + other.gameObject.name);
        }
    }
}
