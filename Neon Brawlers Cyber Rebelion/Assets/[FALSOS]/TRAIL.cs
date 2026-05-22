using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TRAIL : MonoBehaviour
{
    #region V A R I A B L E S
    public float activeTime = 2f;

    [Header("Mesh Part")]
    public float meshRefreshRate = 0.1f;
    public float meshDestroyDelay = 1.5f;
    public Transform positionToSpawn;

    [Header("Shader Part")]
    public Material mat;
    public string shaderVarRef = "_Alpha";
    public float shaderVarRate = 0.1f;
    public float shaderVarRrefeshRate = 0.05f;

    private bool isTrailActive;
    private SkinnedMeshRenderer[] skinnedMeshRenderers;
    #endregion

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isTrailActive)
        {
            isTrailActive = true;
            StartCoroutine(ActivateTrail(activeTime));
        }
    }

    IEnumerator ActivateTrail(float timeActive)
    {
        while (timeActive > 0)
        {
            timeActive -= meshRefreshRate;

            if (skinnedMeshRenderers == null)
                skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

            for (int i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                GameObject gObj = new GameObject();
                gObj.transform.SetPositionAndRotation(positionToSpawn.position, positionToSpawn.rotation);

                MeshRenderer mr = gObj.AddComponent<MeshRenderer>();
                MeshFilter mf = gObj.AddComponent<MeshFilter>();

                Mesh mesh = new Mesh();
                skinnedMeshRenderers[i].BakeMesh(mesh);
                mf.mesh = mesh;

                // ✅ Instancia propia por cada mesh para no afectar el mat original
                Material matInstance = new Material(mat);
                mr.material = matInstance;

                // ✅ Arranca el fade pasando el renderer para desactivarlo al final
                StartCoroutine(AnimateMaterialFloat(mr, matInstance, mesh, gObj));
            }

            yield return new WaitForSeconds(meshRefreshRate);
        }
        isTrailActive = false;
    }

    IEnumerator AnimateMaterialFloat(MeshRenderer mr, Material matInstance, Mesh mesh, GameObject gObj)
    {
        float valueToAnimate = matInstance.GetFloat(shaderVarRef);

        // Fade del alpha hasta 0
        while (valueToAnimate > 0f)
        {
            valueToAnimate -= shaderVarRate;
            matInstance.SetFloat(shaderVarRef, Mathf.Max(valueToAnimate, 0f));
            yield return new WaitForSeconds(shaderVarRrefeshRate);
        }

        // ✅ Alpha ya en 0, ahora sí destruyes limpio
        Destroy(matInstance);
        Destroy(mesh);
        Destroy(gObj);
    }
}