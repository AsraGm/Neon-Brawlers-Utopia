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
    [SerializeField] private Transform originPoint;

    [Header("Alturas de referencia")]
    [SerializeField] private float eyeHeight = 1.6f;

    public Transform Origin => originPoint != null ? originPoint : transform;
    public LayerMask ObstructionMask => obstructionMask;
    public bool canSeePlayer;

    private float originalRadius;
    private Coroutine radiusChangeCoroutine;

    private void Start()
    {
        originalRadius = radius;
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
        Transform origin = Origin;

        Collider[] rangeChecks = Physics.OverlapSphere(origin.position, radius, targetMask);

        if (rangeChecks.Length != 0)
        {
            Transform target = rangeChecks[0].transform;
            Vector3 targetPoint = GetSightPoint(target);
            Vector3 eyeOrigin = origin.position + Vector3.up * eyeHeight;
            Vector3 directionToTarget = (targetPoint - eyeOrigin).normalized;
            Vector3 flatDirection = new Vector3(directionToTarget.x, 0, directionToTarget.z).normalized;

            if (Vector3.Angle(origin.forward, flatDirection) < angle / 2)
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

    public void SetRadius(float targetRadius, float duration)
    {
        if (radiusChangeCoroutine != null)
        {
            StopCoroutine(radiusChangeCoroutine);
        }
        radiusChangeCoroutine = StartCoroutine(LerpRadius(targetRadius, duration));
    }

    public void ResetRadius(float duration)
    {
        SetRadius(originalRadius, duration);
    }

    private IEnumerator LerpRadius(float targetRadius, float duration)
    {
        float startRadius = radius;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            radius = Mathf.Lerp(startRadius, targetRadius, t);
            yield return null;
        }

        radius = targetRadius;
        radiusChangeCoroutine = null;
    }
}
