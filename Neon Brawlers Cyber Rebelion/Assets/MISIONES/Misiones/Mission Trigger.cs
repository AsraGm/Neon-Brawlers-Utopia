using UnityEngine;

public class MissionTrigger : MonoBehaviour
{
    [SerializeField] private string mensaje;
    [SerializeField] private int id;
    private bool activado = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !activado)
        {
            activado = true;
            ObjetivoManager.Instance.MostrarMensaje(mensaje);
            ObjetivoManager.Instance.CambiarAMision(id);
        }
    }
}
