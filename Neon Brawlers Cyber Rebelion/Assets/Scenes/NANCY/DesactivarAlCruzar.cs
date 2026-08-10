using UnityEngine;
public class DesactivarAlCruzar : MonoBehaviour
{
    [Tooltip("Arrastra aquí todos los objetos que quieres desactivar")]
    public GameObject[] objetosADesactivar;

    [Tooltip("Si está activado, el trigger solo funciona una vez")]
    public bool soloUnaVez = true;

    private bool yaActivado = false;

    private void OnTriggerEnter(Collider other)
    {
        if (soloUnaVez && yaActivado) return;

        if (other.CompareTag("Player"))
        {
            foreach (GameObject obj in objetosADesactivar)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }

            yaActivado = true;
        }
    }
}

