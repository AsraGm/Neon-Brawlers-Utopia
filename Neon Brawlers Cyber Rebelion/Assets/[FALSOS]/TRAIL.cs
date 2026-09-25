using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TRAIL : MonoBehaviour
{
    #region Variables
    [Header("Mesh Part")]
    [Tooltip("Cada cuántos segundos se genera una nueva copia de la malla mientras la estela está activa. Más bajo = estela más densa (más copias juntas), pero más costoso.")]
    public float meshRefreshRate = 0.1f;
    public Transform positionToSpawn;

    [Header("Shader Part")]
    public Material mat;
    public string shaderVarRef = "_Alpha";
    [Tooltip("Cuánto baja el valor de alpha en cada paso del desvanecido. Junto con shaderVarRefreshRate determina cuánto dura el fade completo.")]
    public float shaderVarRate = 0.1f;
    [Tooltip("Cada cuántos segundos se actualiza el valor de alpha durante el desvanecido. Más bajo = fade más suave/fluido.")]
    public float shaderVarRefreshRate = 0.05f;

    private bool isTrailActive = false;
    private Coroutine trailCoroutine;
    private SkinnedMeshRenderer[] skinnedMeshRenderers;

    #endregion

    public void StartTrail()
    {
        if (isTrailActive) return;

        isTrailActive = true;
        skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        trailCoroutine = StartCoroutine(TrailLoop());
    }

    public void StopTrail()
    {
        if (!isTrailActive) return;

        isTrailActive = false;

        if (trailCoroutine != null)
        {
            StopCoroutine(trailCoroutine);
            trailCoroutine = null;
        }
    }

    private IEnumerator TrailLoop()
    {
        while (isTrailActive)
        {
            SpawnMeshSnapshot();
            yield return new WaitForSecondsRealtime(meshRefreshRate);
        }
    }

    private void SpawnMeshSnapshot()
    {
        if (skinnedMeshRenderers == null || skinnedMeshRenderers.Length == 0) return;

        for (int i = 0; i < skinnedMeshRenderers.Length; i++)
        {
            SkinnedMeshRenderer smr = skinnedMeshRenderers[i];

            GameObject gObj = new GameObject($"TrailMesh_{i}");
            gObj.transform.SetPositionAndRotation(positionToSpawn.position, positionToSpawn.rotation);

            MeshRenderer mr = gObj.AddComponent<MeshRenderer>();
            MeshFilter mf = gObj.AddComponent<MeshFilter>();

            Mesh mesh = new Mesh();
            smr.BakeMesh(mesh);
            mf.mesh = mesh;

            Material[] originalMats = smr.sharedMaterials; // texturas reales del personaje
            int subCount = mesh.subMeshCount;
            Material[] matInstances = new Material[subCount];

            for (int s = 0; s < subCount; s++)
            {
                Material instance = new Material(mat); // tu shader holograma
                Material original = originalMats[Mathf.Min(s, originalMats.Length - 1)];

                if (original.HasProperty("_MainTex"))
                    instance.SetTexture("_MainTex", original.GetTexture("_MainTex"));

                matInstances[s] = instance;
            }

            mr.materials = matInstances; // ojo: "materials" (plural), no "material"

            StartCoroutine(FadeAndDestroy(mr, matInstances, mesh, gObj));
        }
    }

    private IEnumerator FadeAndDestroy(MeshRenderer mr, Material[] matInstances, Mesh mesh, GameObject gObj)
    {
        float alpha = matInstances[0].GetFloat(shaderVarRef);

        while (alpha > 0f)
        {
            alpha -= shaderVarRate;
            float clamped = Mathf.Max(alpha, 0f);
            for (int i = 0; i < matInstances.Length; i++)
                matInstances[i].SetFloat(shaderVarRef, clamped);

            yield return new WaitForSecondsRealtime(shaderVarRefreshRate);
        }

        for (int i = 0; i < matInstances.Length; i++)
            Destroy(matInstances[i]);

        Destroy(mesh);
        Destroy(gObj);
    }
}