using UnityEngine;
using UnityEngine.Events;

public class ScannerAccess : MonoBehaviour
{
    public UnityEvent Access;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Interactable"))
        {
            Access?.Invoke();
        }
    }
}
