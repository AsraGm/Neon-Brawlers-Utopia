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

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip deniedSound;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        bool tieneAmbos = InventoryUIManager.Instance.TieneItem(item1) &&
                           InventoryUIManager.Instance.TieneItem(item2);

        if (tieneAmbos)
        {
            if (audioSource != null && openSound != null)
                audioSource.PlayOneShot(openSound);

            StartCoroutine(ScanearTarjetas());
            WaveCollected?.Invoke();
        }
        else
        {
            if (audioSource != null && deniedSound != null)
                audioSource.PlayOneShot(deniedSound);

            Debug.Log("No tienes los items necesarios");
        }
    }

    private IEnumerator ScanearTarjetas()
    {
        yield return new WaitForSeconds(scanningTime);
    }
}
