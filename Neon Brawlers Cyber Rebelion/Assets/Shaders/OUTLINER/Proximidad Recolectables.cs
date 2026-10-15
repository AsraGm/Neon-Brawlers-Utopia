using UnityEngine;

public class ProximidadRecolectables : MonoBehaviour
{
    private Renderer rend;

    void Start()
    {
      rend = GetComponent<Renderer>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            rend.enabled = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            rend.enabled = false;
        }
    }
}
