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
    private int defaultStateHash;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    private void Start()
    {
        defaultStateHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
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

    public void ReturnToDefaultState()
    {
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
            stunCoroutine = null;
        }

        isStunned = false;
        animator.speed = 1f;

        if (damageCollider != null)
            damageCollider.enabled = true;

        animator.Play(defaultStateHash, 0, 0f);
        animator.Update(0f);
    }
}
