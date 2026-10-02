using UnityEngine;

public class ProximidadOutline : MonoBehaviour
{
    [SerializeField] Renderer rend;
    [SerializeField] Material outlineMat;   
    [SerializeField] Material normalMat;

    void Start()
    {
        rend.sharedMaterials = new[] { normalMat };
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            rend.sharedMaterials = new[] { outlineMat, normalMat };
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            rend.sharedMaterials = new[] { normalMat };
        }
    }
}
