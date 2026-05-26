using UnityEngine;

public class FootstepSpawner : MonoBehaviour
{
    [Header("Transforms de los pies")]
    public Transform leftFoot;
    public Transform rightFoot;

    [Header("Prefab de huella")]
    public GameObject footprintPrefab;

    [Header("Configuración")]
    public float lifetime = 2f;
    public LayerMask groundLayer;
    public float raycastDistance = 0.3f;

    private bool isSlowMotionActive = false;

    private bool leftWasGrounded = false;
    private bool rightWasGrounded = false;

    public void SetSlowMotionActive(bool active)
    {
        isSlowMotionActive = active;

        if (!active)
        {
            leftWasGrounded = false;
            rightWasGrounded = false;
        }
    }

    private void Update()
    {
        if (!isSlowMotionActive) return;

        CheckFoot(leftFoot, ref leftWasGrounded);
        CheckFoot(rightFoot, ref rightWasGrounded);
    }

    private void CheckFoot(Transform foot, ref bool wasGrounded)
    {
        if (foot == null || footprintPrefab == null) return;

        bool isGrounded = Physics.Raycast(
            foot.position,
            Vector3.down,
            out RaycastHit hit,
            raycastDistance,
            groundLayer
        );

        if (isGrounded && !wasGrounded)
            SpawnFootprint(hit.point, hit.normal, foot.rotation);

        wasGrounded = isGrounded;
    }

    private void SpawnFootprint(Vector3 position, Vector3 normal, Quaternion footRotation)
    {
        Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, normal);
        Quaternion finalRotation = surfaceRotation * Quaternion.Euler(0, footRotation.eulerAngles.y, 0);

        GameObject print = Instantiate(footprintPrefab, position, finalRotation);
        Destroy(print, lifetime);
    }
}