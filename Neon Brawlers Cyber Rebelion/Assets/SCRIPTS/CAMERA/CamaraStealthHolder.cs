using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CamaraStealthHolder : MonoBehaviour
{
    #region Preview

    [Header("Preview de cámara")]

    [Tooltip("Distancia utilizada para previsualizar la dirección y el frustum de la cámara")]
    [Min(0.1f)]
    [SerializeField]
    private float previewDistance = 4f;

    [Tooltip("Campo de visión vertical utilizado únicamente para dibujar el preview")]
    [Range(1f, 179f)]
    [SerializeField]
    private float previewFieldOfView = 60f;

    [Tooltip("Relación de aspecto utilizada para dibujar el preview. 1.777 representa 16:9")]
    [Min(0.1f)]
    [SerializeField]
    private float previewAspect = 16f / 9f;

    [Tooltip("Tamaño del icono que representa la posición del holder")]
    [Min(0.01f)]
    [SerializeField]
    private float holderSize = 0.25f;

    [Tooltip("Muestra una representación aproximada del campo de visión de la cámara")]
    [SerializeField]
    private bool showFrustum = true;

    [Tooltip("Muestra los ejes locales del holder")]
    [SerializeField]
    private bool showLocalAxes = true;

    #endregion

    #region API Pública

    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;
    public Vector3 Forward => transform.forward;

    #endregion

    #region Gizmos

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        bool selected = Selection.activeGameObject == gameObject || Selection.activeTransform == transform;

        DrawHolderGizmos(selected);
    }

#endif

    private void DrawHolderGizmos(bool selected)
    {
        float safeDistance = Mathf.Max(previewDistance, 0.1f);
        float safeAspect = Mathf.Max(previewAspect, 0.1f);
        float safeHolderSize = Mathf.Max(holderSize, 0.01f);
        float safeFieldOfView = Mathf.Clamp(previewFieldOfView, 1f, 179f);

        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 up = transform.up;

        DrawHolderIcon(origin, safeHolderSize, selected);
        DrawForwardDirection(origin, forward, right, up, safeDistance, safeHolderSize, selected);

        if (showFrustum)
        {
            DrawFrustum(origin, forward, right, up, safeDistance, safeAspect, safeFieldOfView, selected);
        }

        if (showLocalAxes)
        {
            DrawLocalAxes(origin, right, up, forward, safeHolderSize);
        }
    }

    private void DrawHolderIcon(Vector3 origin, float size, bool selected)
    {
        Gizmos.color = selected ? new Color(1f, 0.85f, 0.1f, 1f) : new Color(1f, 0.65f, 0.1f, 0.8f);

        Gizmos.DrawWireSphere(origin, size);
        Gizmos.DrawWireCube(origin, new Vector3(size * 1.5f, size, size * 0.75f));
    }

    private void DrawForwardDirection(Vector3 origin, Vector3 forward, Vector3 right, Vector3 up, float distance, float size, bool selected)
    {
        Vector3 directionEnd = origin + forward * distance;

        Gizmos.color = selected ? new Color(1f, 0.9f, 0.15f, 1f) : new Color(1f, 0.7f, 0.1f, 0.85f);

        Gizmos.DrawLine(origin, directionEnd);

        float arrowSize = Mathf.Max(size * 1.5f, distance * 0.08f);

        Vector3 arrowBase = directionEnd - forward * arrowSize;
        Vector3 arrowLeft = arrowBase + right * arrowSize * 0.5f;
        Vector3 arrowRight = arrowBase - right * arrowSize * 0.5f;
        Vector3 arrowUp = arrowBase + up * arrowSize * 0.5f;
        Vector3 arrowDown = arrowBase - up * arrowSize * 0.5f;

        Gizmos.DrawLine(directionEnd, arrowLeft);
        Gizmos.DrawLine(directionEnd, arrowRight);
        Gizmos.DrawLine(directionEnd, arrowUp);
        Gizmos.DrawLine(directionEnd, arrowDown);
    }

    private void DrawFrustum(Vector3 origin, Vector3 forward, Vector3 right, Vector3 up, float distance, float aspect, float fieldOfView, bool selected)
    {
        float halfVerticalSize = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad) * distance;
        float halfHorizontalSize = halfVerticalSize * aspect;

        Vector3 center = origin + forward * distance;

        Vector3 topLeft = center + up * halfVerticalSize - right * halfHorizontalSize;
        Vector3 topRight = center + up * halfVerticalSize + right * halfHorizontalSize;
        Vector3 bottomLeft = center - up * halfVerticalSize - right * halfHorizontalSize;
        Vector3 bottomRight = center - up * halfVerticalSize + right * halfHorizontalSize;

        Gizmos.color = selected ? new Color(1f, 0.85f, 0.1f, 0.8f) : new Color(1f, 0.65f, 0.1f, 0.45f);

        Gizmos.DrawLine(origin, topLeft);
        Gizmos.DrawLine(origin, topRight);
        Gizmos.DrawLine(origin, bottomLeft);
        Gizmos.DrawLine(origin, bottomRight);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }

    private void DrawLocalAxes(Vector3 origin, Vector3 right, Vector3 up, Vector3 forward, float size)
    {
        float axisLength = size * 2.5f;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + right * axisLength);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + up * axisLength);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(origin, origin + forward * axisLength);
    }

    #endregion
}