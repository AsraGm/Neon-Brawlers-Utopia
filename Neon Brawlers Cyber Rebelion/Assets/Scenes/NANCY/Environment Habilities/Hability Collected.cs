using UnityEngine;
using UnityEngine.Events;

public class HabilityCollected : MonoBehaviour
{
    public UnityEvent WaveCollected;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            WaveCollected?.Invoke();
        }
    }

}
