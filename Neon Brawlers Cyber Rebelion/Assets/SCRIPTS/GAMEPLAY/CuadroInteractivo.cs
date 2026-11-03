using UnityEngine;

public class CuadroInteractivo : MonoBehaviour
{
    [Header("Imagen")]
    [SerializeField] private Renderer cuadroRenderer;
    [SerializeField] private Texture texturaAlterna;

    private Texture texturaOriginal;
    private Material materialInstancia;

    private void Awake()
    {
        if (cuadroRenderer == null)
            cuadroRenderer = GetComponentInChildren<Renderer>();
        materialInstancia = cuadroRenderer.material;
        texturaOriginal = materialInstancia.mainTexture;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        materialInstancia.mainTexture = texturaAlterna;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        materialInstancia.mainTexture = texturaOriginal;
    }

    private void OnDestroy()
    {
        // Evita fugas de memoria por la instancia de material
        if (materialInstancia != null) Destroy(materialInstancia);
    }
}