using UnityEngine;

public class ElectricCurrentDamage : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 100;

    public void ApplyStun()
    {
        Destroy(this.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
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
