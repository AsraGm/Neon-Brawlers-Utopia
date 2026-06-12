using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class NextLevel : MonoBehaviour
{
    public UnityEvent WaveCollected;

    [Tooltip("Tiempo para que las animaciones pasen")]
    [SerializeField] private float scanningTime;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (InventoryUIManager.Instance.TieneItem("Area_2") && (InventoryUIManager.Instance.TieneItem("Area_2.2")))
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
