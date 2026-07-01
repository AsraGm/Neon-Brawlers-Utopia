using System.Collections;
using UnityEngine;

public class AutomaticDoor : MonoBehaviour
{
    [SerializeField] private float automaticCloseDelay = 5f;
    private InteractableDoor interactableDoor;

    private Animator animator;
    private Coroutine closeRoutine;
    private int playersInside = 0;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (interactableDoor == null)
            interactableDoor = GetComponent<InteractableDoor>();
    }

    public void HandleTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playersInside++;
        CancelPendingClose();
        OpenDoor();
    }

    public void HandleTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playersInside = Mathf.Max(0, playersInside - 1);
        if (playersInside != 0) return;

        CancelPendingClose();
        closeRoutine = StartCoroutine(AutomaticCloseAfterDelay());
    }

    private void CancelPendingClose()
    {
        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }
    }

    private void OpenDoor()
    {
        if (interactableDoor.doorOpen) return; 
        animator.SetBool("Open", true);
        animator.SetBool("Close", false);
        interactableDoor.doorOpen = true;
    }

    private void CloseDoor()
    {
        if (!interactableDoor.doorOpen) return; 
        animator.SetBool("Close", true);
        animator.SetBool("Open", false);
        interactableDoor.doorOpen = false;
    }

    private IEnumerator AutomaticCloseAfterDelay()
    {
        yield return new WaitForSeconds(automaticCloseDelay);
        if (playersInside == 0)
            CloseDoor();
        closeRoutine = null;
    }

    public void OnCloseAnimationFinished() { }

    [ContextMenu("Reset Player Count")]
    private void ResetPlayerCount() => playersInside = 0;
}
