using System.Collections;
using UnityEngine;

public class JumpscareManager : MonoBehaviour
{
    public static JumpscareManager instance { get; private set; }

    [Header("Cámara del jugador")]
    public Camera playerCamera;
    public GameObject playerMesh;

    [Header("Jumpscares")]
    public JumpscareData[] jumpscares;

    [System.Serializable]
    public class JumpscareData
    {
        public string nombre;
        public Camera camara;
        public Animator animator;
        public string triggerAnimacion = "doJumpscare";
        public float duracion = 3f;
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        // Aseguramos que todas las cámaras de jumpscare arrancan apagadas
        foreach (var j in jumpscares)
            if (j.camara != null)
                j.camara.gameObject.SetActive(false);
    }

    public void ActivarJumpscare(string nombreEnemigo, System.Action onFinish)
    {
        JumpscareData data = System.Array.Find(jumpscares, j => j.nombre == nombreEnemigo);
        if (data == null)
        {
            Debug.LogWarning($"Jumpscare '{nombreEnemigo}' no encontrado");
            onFinish?.Invoke();
            return;
        }

        StartCoroutine(SecuenciaJumpscare(data, onFinish));
    }

    private IEnumerator SecuenciaJumpscare(JumpscareData data, System.Action onFinish)
    {
        // Apagar cámara y mesh del jugador
        if (playerCamera != null) playerCamera.gameObject.SetActive(false);
        if (playerMesh != null) playerMesh.SetActive(false);

        // Activar cámara del jumpscare
        data.camara.gameObject.SetActive(true);

        // Disparar animación
        if (data.animator != null)
            data.animator.SetTrigger(data.triggerAnimacion);

        // Esperar duración
        yield return new WaitForSecondsRealtime(data.duracion);

        // Restaurar
        data.camara.gameObject.SetActive(false);
        if (playerCamera != null) playerCamera.gameObject.SetActive(true);
        if (playerMesh != null) playerMesh.SetActive(true);

        onFinish?.Invoke();
    }
}