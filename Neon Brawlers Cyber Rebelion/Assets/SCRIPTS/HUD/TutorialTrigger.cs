using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [TextArea(2, 5)]
    [SerializeField] private string mensaje;
    [SerializeField] private bool soloUnaVez = true;

    private bool yaSeVio = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (soloUnaVez && yaSeVio) return;

        yaSeVio = true;
        TutorialManager.instance?.ShowMessage(mensaje);
    }
}