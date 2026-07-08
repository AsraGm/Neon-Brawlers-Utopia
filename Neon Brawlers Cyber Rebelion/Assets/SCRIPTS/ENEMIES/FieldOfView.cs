using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FieldOfView : MonoBehaviour
{
    public float radius;
    [Range(0, 360)]
    public float angle;

    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstructionMask;

    [Header("Alturas de referencia")]
    [SerializeField] private float eyeHeight = 1.6f;

    public LayerMask ObstructionMask => obstructionMask;

    public bool canSeePlayer;

    private void Start()
    {
        StartCoroutine(FOVRoutine());
    }

    private IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);

        while (true)
        {
            yield return wait;
            FieldOfViewCheck();
        }
    }
    private void FieldOfViewCheck()
    {
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, radius, targetMask);

        if (rangeChecks.Length != 0)
        {
            Transform target = rangeChecks[0].transform;

            Vector3 targetPoint = GetSightPoint(target);
            Vector3 eyeOrigin = transform.position + Vector3.up * eyeHeight;
            Vector3 directionToTarget = (targetPoint - eyeOrigin).normalized;

            Vector3 flatDirection = new Vector3(directionToTarget.x, 0, directionToTarget.z).normalized;

            if (Vector3.Angle(transform.forward, flatDirection) < angle / 2)
            {
                if (HabilidadesManager.instance.playerIsHiding)
                {
                    canSeePlayer = false;
                    return;
                }

                float distanceToTarget = Vector3.Distance(eyeOrigin, targetPoint);

                if (!Physics.Raycast(eyeOrigin, directionToTarget, distanceToTarget, obstructionMask))
                {
                    canSeePlayer = true;
                }
                else
                {
                    canSeePlayer = false;
                }
            }
            else
            {
                canSeePlayer = false;
            }
        }
        else if (canSeePlayer)
        {
            canSeePlayer = false;
        }
    }

    private Vector3 GetSightPoint(Transform target)
    {
        CharacterController targetCC = target.GetComponent<CharacterController>();

        if (targetCC == null)
            targetCC = target.GetComponentInParent<CharacterController>();

        if (targetCC != null)
        {
            float topOfCapsule = targetCC.center.y + (targetCC.height / 2f);

            float eyeOffset = targetCC.height * 0.1f;

            return target.position + Vector3.up * (topOfCapsule - eyeOffset);
        }

        return target.position + Vector3.up * 1.5f;
    }
}
