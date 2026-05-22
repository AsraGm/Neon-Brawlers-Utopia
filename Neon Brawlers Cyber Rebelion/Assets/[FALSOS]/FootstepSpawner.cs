using UnityEngine;

public class FootstepSpawner : MonoBehaviour
{
    [Header("Transforms de los pies")]
    public Transform leftFoot;
    public Transform rightFoot;

    [Header("Prefab de huella")]
    public GameObject footprintPrefab;

    [Header("Configuración")]
    public float lifetime = 2f;          // Cuánto dura la huella antes de destruirse
    public LayerMask groundLayer;        // Qué capas considera "suelo"
    public float raycastDistance = 0.2f; // Qué tan abajo busca el suelo

    private bool leftWasGrounded = false;
    private bool rightWasGrounded = false;

    void Update()
    {
        CheckFoot(leftFoot, ref leftWasGrounded);
        CheckFoot(rightFoot, ref rightWasGrounded);
    }

    void CheckFoot(Transform foot, ref bool wasGrounded)
    {
        if (foot == null || footprintPrefab == null) return;

        // Raycast hacia abajo desde el pie
        bool isGrounded = Physics.Raycast(foot.position, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer);

        // Solo spawnea en el momento exacto que toca el suelo (flanco de bajada)
        if (isGrounded && !wasGrounded)
        {
            SpawnFootprint(hit.point, hit.normal, foot.rotation);
        }

        wasGrounded = isGrounded;
    }

    void SpawnFootprint(Vector3 position, Vector3 normal, Quaternion footRotation)
    {
        // Alinea el prefab con la normal del suelo
        Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, normal);

        // Mantiene la orientación de hacia donde mira el pie
        Quaternion finalRotation = surfaceRotation * Quaternion.Euler(0, footRotation.eulerAngles.y, 0);

        GameObject print = Instantiate(footprintPrefab, position, finalRotation);

        // Lo destruye después del lifetime configurado
        Destroy(print, lifetime);
    }
}