using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    [SerializeField] private AutomaticDoor automaticDoor;

    private void OnTriggerEnter(Collider other) => automaticDoor.HandleTriggerEnter(other);
    private void OnTriggerExit(Collider other) => automaticDoor.HandleTriggerExit(other);
}
