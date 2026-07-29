using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class NextLevel : MonoBehaviour
{
    public UnityEvent WaveCollected;

    [Tooltip("Tiempo para que las animaciones pasen")]
    [SerializeField] private float scanningTime;
    [SerializeField] private string item1;
    [SerializeField] private string item2;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (InventoryUIManager.Instance.TieneItem(item1) && (InventoryUIManager.Instance.TieneItem(item2)))
            {
                StartCoroutine(ScanearTarjetas());
                WaveCollected?.Invoke();
            }
        }
        else
        {
            Debug.Log("No se registraron esos objetos");
        }
    }

    private IEnumerator ScanearTarjetas()
    {
        yield return new WaitForSeconds(scanningTime);
    }
}
