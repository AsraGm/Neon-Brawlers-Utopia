using UnityEngine;
using System.Collections;

public class InteractableDoor : MonoBehaviour
{
    [Header("Stun")]
    [SerializeField] private float stunDuration = 3f;
    [SerializeField] private Collider damageCollider;

    [SerializeField] private AutomaticDoor automaticDoor;

    public bool isStunned { get; private set; }
    public bool doorOpen;

    private Animator animator;
    private Coroutine stunCoroutine;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void ApplyStun(float duration)
    {
        if (stunCoroutine != null)
            StopCoroutine(stunCoroutine);

        stunCoroutine = StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        animator.speed = 0f;
        damageCollider.enabled = false;

        yield return new WaitForSeconds(duration);

        animator.speed = 1f;
        damageCollider.enabled = true;
        isStunned = false;
    }

    public void OnCloseAnimationFinished()
    {
        if (automaticDoor != null)
        {
            automaticDoor.OnCloseAnimationFinished();
        }
    }
}
