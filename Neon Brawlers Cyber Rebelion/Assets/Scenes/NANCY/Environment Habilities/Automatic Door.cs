using System.Collections;
using UnityEngine;

public class AutomaticDoor : MonoBehaviour
{
    [SerializeField] private InteractableDoor doorOpen;
    [SerializeField] private Animator animator;
    [SerializeField] private float automaticClose = 5f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!doorOpen.doorOpen)
            {
                animator.SetBool("Open", true);
                animator.SetBool("Close", false);
                doorOpen.doorOpen = true;

                StartCoroutine(AutomaticClose());
            }
            else
            {
                animator.SetBool("Close", true);
                animator.SetBool("Open", false);
                doorOpen.doorOpen = false;
            }
        }
    }

    private IEnumerator AutomaticClose()
    {
        yield return new WaitForSeconds(automaticClose);

        animator.SetBool("Close", true);
        animator.SetBool("Open", false);
        doorOpen.doorOpen = false;
    }
}
