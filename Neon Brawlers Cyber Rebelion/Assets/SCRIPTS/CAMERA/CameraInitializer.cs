using UnityEngine;

public class CameraInitializer : MonoBehaviour
{
    public Camera mainCamera;

    private void Awake()
    {
        // Forzar que la main camera sea la activa al iniciar
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
            mainCamera.tag = "MainCamera";
        }

        // Por si hay otras cámaras activas que no deberían estarlo
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera cam in allCameras)
        {
            if (cam != mainCamera)
                cam.gameObject.SetActive(false);
        }
    }
}
