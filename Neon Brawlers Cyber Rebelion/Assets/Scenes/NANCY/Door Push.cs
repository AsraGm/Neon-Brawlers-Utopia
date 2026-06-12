using UnityEngine;

public class DoorPush : MonoBehaviour
{
    [SerializeField] private float pushForce = 5f;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!hit.collider.CompareTag("PushableDoor"))
            return;

        Rigidbody rb = hit.collider.attachedRigidbody;

        if (rb == null || rb.isKinematic)
            return;

        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        rb.AddForce(pushDir * pushForce, ForceMode.Impulse);
    }
}
