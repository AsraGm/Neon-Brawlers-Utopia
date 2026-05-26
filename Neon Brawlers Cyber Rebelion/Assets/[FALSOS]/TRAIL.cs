using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TRAIL : MonoBehaviour
{
    #region Variables

    [Header("Mesh Part")]
    public float meshRefreshRate = 0.1f;
    public Transform positionToSpawn;

    [Header("Shader Part")]
    public Material mat;
    public string shaderVarRef = "_Alpha";
    public float shaderVarRate = 0.1f;
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
            GameObject gObj = new GameObject($"TrailMesh_{i}");
            gObj.transform.SetPositionAndRotation(positionToSpawn.position, positionToSpawn.rotation);

            MeshRenderer mr = gObj.AddComponent<MeshRenderer>();
            MeshFilter mf = gObj.AddComponent<MeshFilter>();

            Mesh mesh = new Mesh();
            skinnedMeshRenderers[i].BakeMesh(mesh);
            mf.mesh = mesh;

            Material matInstance = new Material(mat);
            mr.material = matInstance;

            StartCoroutine(FadeAndDestroy(mr, matInstance, mesh, gObj));
        }
    }

    private IEnumerator FadeAndDestroy(MeshRenderer mr, Material matInstance, Mesh mesh, GameObject gObj)
    {
        float alpha = matInstance.GetFloat(shaderVarRef);

        while (alpha > 0f)
        {
            alpha -= shaderVarRate;
            matInstance.SetFloat(shaderVarRef, Mathf.Max(alpha, 0f));
            yield return new WaitForSecondsRealtime(shaderVarRefreshRate);
        }

        Destroy(matInstance);
        Destroy(mesh);
        Destroy(gObj);
    }
}