using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Detector : MonoBehaviour
{
    [Header("Waypoints")]
    [SerializeField] private Transform[] waypoints;
    private int waypointIndex;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Stun")]
    [SerializeField] private float stunDuration = 2f;

    [SerializeField] private bool isStunned = false;
    public UnityEvent playerDetected;
    public UnityEvent gameRestart;
    public UnityEvent detectorStunned;

    void Update()
    {
        if (waypoints.Length == 0) return;

        MoveToWaypoint();
    }

    void MoveToWaypoint()
    {
        Transform target = waypoints[waypointIndex];

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
        }
    }

    public void ApplyStun()
    {
        if (!isStunned)
            StartCoroutine(StunCoroutine());
    }

    private IEnumerator StunCoroutine()
    {
        isStunned = true;
        detectorStunned?.Invoke();

        //Cambio de color a naranja
        //Parpoadeo

        yield return new WaitForSeconds(stunDuration);

        isStunned = false;

        //De regreso a verde
    }

    private IEnumerator Restart()
    {
        yield return new WaitForSeconds(5);
        gameRestart?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isStunned)
        {
            //se pone rojo

            playerDetected?.Invoke(); //la puerta se cierra y se libera el gas toxico

            StartCoroutine(Restart()); //regresa todo a como estaba
        }
    }
}
