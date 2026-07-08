using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]

public class FOVMesh : MonoBehaviour
{
    [Header("Referencia al FOV lógico")]
    [SerializeField] private FieldOfView fov;
    private EnemyPatrol enemyPatrol;

    [Header("Configuración visual")]
    [Range(10, 200)][SerializeField] private int rayCount = 60;
    [SerializeField] private float raycastHeightOffset = 0.05f;
    [SerializeField] private float visualHeightOffset = 5f;
    [SerializeField] private float originForwardOffset = 0.3f;
    [SerializeField] private float edgeDistanceThreshold = 0.5f;
    [Range(1, 5)][SerializeField] private int edgeResolveIterations = 4;
    [SerializeField] private float extraVisualRadius = 1.5f;

    [Header("Volumen (extrusión hacia el suelo)")]
    [SerializeField] private bool buildVolume = true;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundSearchDistance = 10f;
    [SerializeField] private float fallbackGroundY = 0f;
    [SerializeField] private float groundSkin = 0.02f;

    [Header("Shader / Material")]
    [SerializeField] private Material fovMaterial;
    [SerializeField] private string color1 = "_Fresnel_Color";
    [SerializeField] private string color2 = "_OtroColor";
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color alertColor = new Color(1f, 0f, 0f, 0.35f);

    [Header("Performance")]
    [SerializeField] private int updateEveryNFrames = 1;

    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private int frameCounter;

    private int colorPropID;
    private int secondColorPropID;

    private struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float distance;
        public float angle;
    }

    private struct EdgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;
    }

    private void Awake()
    {
        mesh = new Mesh { name = "FOV Mesh" };
        GetComponent<MeshFilter>().mesh = mesh;
        meshRenderer = GetComponent<MeshRenderer>();

        colorPropID = Shader.PropertyToID(color1);
        secondColorPropID = Shader.PropertyToID(color2);
        enemyPatrol = GetComponentInParent<EnemyPatrol>();

        if (fovMaterial != null)
        {
            meshRenderer.material = fovMaterial;
        }
    }

    private void LateUpdate()
    {
        frameCounter++;
        if (frameCounter >= updateEveryNFrames)
        {
            frameCounter = 0;
            DrawFieldOfView();
            UpdateShaderProperties();
        }
    }

    private void UpdateShaderProperties()
    {
        if (meshRenderer.material == null) return;

        bool isChasing = enemyPatrol != null && enemyPatrol.isChasing;
        Color targetColor = isChasing ? alertColor : normalColor;

        meshRenderer.material.SetColor(colorPropID, targetColor);
        meshRenderer.material.SetColor(secondColorPropID, targetColor);
    }
    private void DrawFieldOfView()
    {
        float angle = fov.angle;
        float radius = fov.radius + extraVisualRadius;
        int stepCount = rayCount;
        float stepAngleSize = angle / stepCount;

        var viewPoints = new List<Vector3>();
        ViewCastInfo oldViewCast = default;

        for (int i = 0; i <= stepCount; i++)
        {
            float currentAngle = -angle / 2f + stepAngleSize * i + transform.eulerAngles.y;
            ViewCastInfo newViewCast = ViewCast(currentAngle, radius);

            if (i > 0)
            {
                bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.distance - newViewCast.distance) > edgeDistanceThreshold;

                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded))
                {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast, radius);
                    if (edge.pointA != Vector3.zero) viewPoints.Add(edge.pointA);
                    if (edge.pointB != Vector3.zero) viewPoints.Add(edge.pointB);
                }
            }

            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }

        if (buildVolume)
            BuildVolumeMesh(viewPoints, radius);
        else
            BuildFlatMesh(viewPoints);
    }

    private void BuildFlatMesh(List<Vector3> viewPoints)
    {
        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        float heightCorrection = visualHeightOffset - raycastHeightOffset;
        float radius = fov.radius + extraVisualRadius;

        Vector3 apexWorld = transform.position + transform.forward * originForwardOffset + Vector3.up * heightCorrection;
        vertices[0] = transform.InverseTransformPoint(apexWorld);
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < vertexCount - 1; i++)
        {
            Vector3 localPoint = transform.InverseTransformPoint(viewPoints[i]);
            vertices[i + 1] = localPoint;

            float normalizedDist = Mathf.Clamp01(localPoint.magnitude / radius);
            uvs[i + 1] = new Vector2(normalizedDist, 0f);

            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void BuildVolumeMesh(List<Vector3> viewPoints, float radius)
    {
        int ringCount = viewPoints.Count;
        if (ringCount < 2)
        {
            mesh.Clear();
            return;
        }

        float groundY = GetGroundWorldY();

        float heightCorrection = visualHeightOffset - raycastHeightOffset;

        int topApexIndex = 0;
        int topRingStart = 1;
        int bottomApexIndex = ringCount + 1;
        int bottomRingStart = ringCount + 2;

        int vertexCount = (ringCount + 1) * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];

        Vector3 topApexWorld = transform.position + transform.forward * originForwardOffset + Vector3.up * heightCorrection;
        vertices[topApexIndex] = transform.InverseTransformPoint(topApexWorld);
        uvs[topApexIndex] = new Vector2(0.5f, 0.5f);

        Vector3 bottomApexWorld = new Vector3(topApexWorld.x, groundY + groundSkin, topApexWorld.z);
        vertices[bottomApexIndex] = transform.InverseTransformPoint(bottomApexWorld);
        uvs[bottomApexIndex] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < ringCount; i++)
        {
            Vector3 worldPoint = viewPoints[i];

            Vector3 topLocal = transform.InverseTransformPoint(worldPoint);
            vertices[topRingStart + i] = topLocal;

            float normalizedDist = Mathf.Clamp01(topLocal.magnitude / radius);
            uvs[topRingStart + i] = new Vector2(normalizedDist, 1f);

            Vector3 bottomWorld = new Vector3(worldPoint.x, groundY + groundSkin, worldPoint.z);
            Vector3 bottomLocal = transform.InverseTransformPoint(bottomWorld);
            vertices[bottomRingStart + i] = bottomLocal;
            uvs[bottomRingStart + i] = new Vector2(normalizedDist, 0f);
        }

        List<int> triangles = new List<int>((ringCount - 1) * 3 * 4);

        for (int i = 0; i < ringCount - 1; i++)
        {
            triangles.Add(topApexIndex);
            triangles.Add(topRingStart + i);
            triangles.Add(topRingStart + i + 1);
        }

        for (int i = 0; i < ringCount - 1; i++)
        {
            triangles.Add(bottomApexIndex);
            triangles.Add(bottomRingStart + i + 1);
            triangles.Add(bottomRingStart + i);
        }

        for (int i = 0; i < ringCount - 1; i++)
        {
            int topA = topRingStart + i;
            int topB = topRingStart + i + 1;
            int botA = bottomRingStart + i;
            int botB = bottomRingStart + i + 1;

            triangles.Add(topA);
            triangles.Add(botA);
            triangles.Add(topB);

            triangles.Add(topB);
            triangles.Add(botA);
            triangles.Add(botB);
        }

        AddSideCap(triangles, topApexIndex, topRingStart, bottomApexIndex, bottomRingStart, 0);
        AddSideCap(triangles, topApexIndex, topRingStart, bottomApexIndex, bottomRingStart, ringCount - 1);

        mesh.Clear();
        mesh.indexFormat = vertexCount > 65000
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

     private void AddSideCap(List<int> triangles, int topApex, int topRingStart, int bottomApex, int bottomRingStart, int ringIndex)
    {
        int top = topRingStart + ringIndex;
        int bottom = bottomRingStart + ringIndex;

        triangles.Add(topApex);
        triangles.Add(bottomApex);
        triangles.Add(top);

        triangles.Add(top);
        triangles.Add(bottomApex);
        triangles.Add(bottom);
    }

    private float GetGroundWorldY()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundSearchDistance, groundMask))
        {
            return hit.point.y;
        }
        return fallbackGroundY;
    }

    private ViewCastInfo ViewCast(float globalAngle, float radius)
    {
        Vector3 dir = DirFromAngle(globalAngle);
        Vector3 origin = transform.position + transform.forward * originForwardOffset + Vector3.up * raycastHeightOffset;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, radius, fov.ObstructionMask))
        {
            return new ViewCastInfo { hit = true, point = hit.point, distance = hit.distance, angle = globalAngle };
        }

        return new ViewCastInfo { hit = false, point = origin + dir * radius, distance = radius, angle = globalAngle };
    }

    private EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast, float radius)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < edgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) / 2f;
            ViewCastInfo newViewCast = ViewCast(angle, radius);

            bool edgeDstThresholdExceeded = Mathf.Abs(minViewCast.distance - newViewCast.distance) > edgeDistanceThreshold;
            if (newViewCast.hit == minViewCast.hit && !edgeDstThresholdExceeded)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }

        return new EdgeInfo { pointA = minPoint, pointB = maxPoint };
    }

    private Vector3 DirFromAngle(float angleInDegrees)
    {
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}

