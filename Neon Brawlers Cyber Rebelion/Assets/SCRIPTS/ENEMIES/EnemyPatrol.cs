using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyPatrol : MonoBehaviour
{
    private NavMeshAgent agent;
    private FieldOfView fieldOfView;

    [Header("Patrol")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform[] waypoints;
    private int waypointIndex;
    private Vector3 target;
    [SerializeField] private float minIdleTime = 1f;
    [SerializeField] private float maxIdleTime = 3f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float patrolSpeed = 3f;
    [SerializeField] private float chaseStoppingDistance = 1.5f;
    [SerializeField] private float patrolStoppingDistance = 0.5f;
    [SerializeField] private float chaseRadius = 15f;
    [Tooltip("Tiempo que tiene para acercarse al jugador al ser alertado")]
    [SerializeField] private float chasingTolerance = 10f;

    [Header("Movement Settings")]
    [SerializeField] private float angularSpeed = 360f;
    [SerializeField] private float acceleration = 10f;

    public bool isStunned /*{ get; private set; }*/ = false;
    public bool isChasing /*{ get; private set; }*/ = false;
    private bool isWaiting = false;
    private Transform player;
    private Coroutine idleCoroutine;
    private RobotAttack robotAttack;
    public bool alertedByDrone = false;
    public bool canAttack = true;

    public bool isBeingManipulated { get; private set; } = false;
    public bool isAttacking = false;
    private Rigidbody rb;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        fieldOfView = GetComponent<FieldOfView>();
        rb = GetComponent<Rigidbody>();

        if (agent != null)
        {
            agent.speed = patrolSpeed;
            agent.stoppingDistance = patrolStoppingDistance;
            agent.angularSpeed = angularSpeed;
            agent.acceleration = acceleration;
            agent.autoBraking = true;
            UpdateDestination();
        }
        player = GameObject.Find("Player").transform;
        robotAttack = GetComponent<RobotAttack>();
    }

    void Update()
    {
        if (isStunned || isBeingManipulated || isAttacking) return;

        if (animator != null)
        {
            animator.SetBool("IsWalking", !isWaiting && !isChasing);
            animator.SetBool("IsChasing", isChasing);
        }

        CheckChaseRange();

        if (isChasing && player != null)
        {
            ChasePlayer();
        }
        else if (!isWaiting)
        {
            Patrol();
        }

        HandleRotation();
    }

    void CheckChaseRange()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        bool playerInRange = distanceToPlayer <= chaseRadius;

        if (alertedByDrone || (playerInRange && fieldOfView != null && fieldOfView.canSeePlayer))
        {
            if (!isChasing)
            {
                StartChasing();
            }
        }

        if (isChasing)
        {
            if (!alertedByDrone && !playerInRange)
            {
                StopChasing();
            }
            else if (alertedByDrone && !playerInRange)
            {
                StartCoroutine(DelayedUnalert());
            }
        }
    }

    private IEnumerator DelayedUnalert()
    {
        yield return new WaitForSeconds(chasingTolerance);
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer > chaseRadius)
        {
            alertedByDrone = false;
            StopChasing();
        }
    }

    public void StartChasing()
    {
        isChasing = true;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = chaseStoppingDistance;

        // Detiene la coroutine de espera si está activa
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
            isWaiting = false;
        }
    }

    void StopChasing()
    {
        isChasing = false;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = patrolStoppingDistance;
        UpdateDestination();
    }

    void ChasePlayer()
    {
        if (Vector3.Distance(agent.destination, player.transform.position) > 1f)
        {
            agent.SetDestination(player.transform.position);
        }
    }

    void Patrol()
    {
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            if (idleCoroutine == null)
            {
                idleCoroutine = StartCoroutine(WaitAtWaypoint());
            }
        }
    }

    IEnumerator WaitAtWaypoint()
    {
        isWaiting = true;
        yield return new WaitForSeconds(Random.Range(minIdleTime, maxIdleTime));

        IterateWaypointIndex();
        UpdateDestination();

        isWaiting = false;
        idleCoroutine = null;
    }

    void HandleRotation()
    {
        if (agent.velocity.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * agent.angularSpeed / 120f
            );
        }
    }

    void UpdateDestination()
    {
        if (agent != null && waypoints.Length > 0)
        {
            target = waypoints[waypointIndex].position;
            agent.SetDestination(target);
        }
    }

    void IterateWaypointIndex()
    {
        waypointIndex++;
        if (waypointIndex == waypoints.Length)
        {
            waypointIndex = 0;
        }
    }

    public void StopAgentForAttack()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    public void ResumeAgentAfterAttack()
    {
        if (agent != null && !isStunned && !isBeingManipulated)
        {
            agent.isStopped = false;
        }
    }

    #region AplicarStun
    public void ApplyStun(float duracion)
    {
        if (!isStunned)
        {
            StartCoroutine(StunCoroutine(duracion));
        }
    }

    private IEnumerator StunCoroutine(float duracion)
    {
        isStunned = true;

        if (animator != null)
            animator.speed = 0f;

        if (robotAttack != null)
        {
            robotAttack.StopRobotAttack();
        }

        // parar animaciones

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
            isWaiting = false;
        }

        yield return new WaitForSeconds(duracion);

        if (animator != null)
            animator.speed = 1f;

        isStunned = false;
        canAttack = true;

        if (robotAttack != null)
        {
            robotAttack.RobotCanAttack();
        }

        if (agent != null)
        {
            agent.isStopped = false;
        }
    }
    #endregion AplicarStun

    #region AplicarTelekinesis
    public void OnTelekinesisGrab()
    {
        isBeingManipulated = true;

        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = false;
        }

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // Detener coroutines activas
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
            isWaiting = false;
        }

        // Detener ataque si existe
        if (robotAttack != null)
        {
            robotAttack.StopRobotAttack();
        }

        Debug.Log("Enemigo agarrado por telekinesis");
    }
    #endregion AplicarTelekinesis

    #region PararStun
    public void StopStun()
    {
        if (isStunned)
        {
            StopAllCoroutines();
            isStunned = false;

            if (animator != null)
                animator.speed = 1f;

            agent.isStopped = false;
            StartChasing();
        }
    }
    #endregion PararStun

    #region PararTelekinesis
    public void OnTelekinesisRelease()
    {
        StartCoroutine(ReleaseFromTelekinesis());
    }

    private IEnumerator ReleaseFromTelekinesis()
    {
        isBeingManipulated = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        //tiempo para que retome su camino
        yield return new WaitForSeconds(3f);

        if (agent != null)
        {
            agent.isStopped = false;
            isChasing = false;
            agent.speed = patrolSpeed;
            agent.stoppingDistance = patrolStoppingDistance;
            UpdateDestination();
        }

        if (robotAttack != null)
        {
            robotAttack.RobotCanAttack();
        }

        Debug.Log("Enemigo liberado de telekinesis");
    }
    #endregion PararTelekinesis

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);
    }
}
