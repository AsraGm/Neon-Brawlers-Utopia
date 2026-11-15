using UnityEngine;

public class VacioGlitch : MonoBehaviour
{
    private Renderer fondoRenderer;
    public float offsetYMin = -0.15f;
    public float offsetYMax = 0.15f;
    public float offsetSpeed = 1f;

    private Material mat;
    private float direccion = 1f;

    private void Start()
    {
        fondoRenderer = GetComponent<Renderer>();
        mat = fondoRenderer.material;
    }

    private void Update()
    {
        Vector2 offset = mat.mainTextureOffset;

        offset.y += direccion * offsetSpeed * Time.unscaledDeltaTime;

        if (offset.y >= offsetYMax)
        {
            offset.y = offsetYMax;
            direccion = -1f;
        }
        else if (offset.y <= offsetYMin)
        {
            offset.y = offsetYMin;
            direccion = 1f;
        }

        mat.mainTextureOffset = offset;
    }
}
