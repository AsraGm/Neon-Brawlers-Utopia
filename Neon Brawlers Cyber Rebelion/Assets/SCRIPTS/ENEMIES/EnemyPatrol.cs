using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private float fovTransitionTime = 1.5f;

    [Header("Investigating")]
    [SerializeField] private float investigateWaitTime = 4f;
    [Tooltip("Distancia a la que se considera que ya llego al punto de ruido")]
    [SerializeField] private float investigateArrivalThreshold = 1f;
    private Coroutine investigateCoroutine;

    [Header("Symbols")]
    [SerializeField] private Renderer symbolRenderer;
    [SerializeField] private Material investigatingMat;
    [SerializeField] private Material alertMat;
    [SerializeField] private Animator symbolAnim;
    [SerializeField] private float offDelay = 0.3f;
    private Coroutine symbolCoroutine;

    [Header("Movement Settings")]
    [SerializeField] private float angularSpeed = 360f;
    [SerializeField] private float acceleration = 10f;

    [Header("Audio")]
    private AudioSource audioSource;
    [SerializeField] private AudioClip generalSound;
    [SerializeField] private AudioClip chaseSound;

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
    public bool isInvestigating { get; private set; } = false;
    private bool isWaitingAtNoisePoint = false;
    private Rigidbody rb;

    private Coroutine stunCoroutine;
    private Coroutine delayedUnalertCoroutine;

    //Dron attack
    public static readonly List<EnemyPatrol> ActiveEnemies = new List<EnemyPatrol>();

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        fieldOfView = GetComponent<FieldOfView>();
        rb = GetComponent<Rigidbody>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        NormalSound();

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

        if (symbolAnim != null)
        {
            symbolAnim.gameObject.SetActive(false);
        }
    }
    void Update()
    {
        if (isStunned || isBeingManipulated || isAttacking) return;

        if (animator != null)
        {
            animator.SetBool("IsWalking", !isWaiting && !isChasing && !isWaitingAtNoisePoint);
            animator.SetBool("IsChasing", isChasing);
        }

        CheckChaseRange();

        if (isChasing && player != null)
        {
            ChasePlayer();
        }
        else if (isInvestigating)
        {
            // El movimiento lo maneja la corrutina de investigacion.
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

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
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
                if (delayedUnalertCoroutine == null)
                {
                    delayedUnalertCoroutine = StartCoroutine(DelayedUnalert());
                }
            }
        }
    }

    private IEnumerator DelayedUnalert()
    {
        yield return new WaitForSeconds(chasingTolerance);

        if (!isStunned && !isBeingManipulated && player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer > chaseRadius)
            {
                alertedByDrone = false;
                StopChasing();
            }
        }

        delayedUnalertCoroutine = null;
    }

    public void StartChasing()
    {
        isChasing = true;

        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
            isWaiting = false;
        }

        if (investigateCoroutine != null)
        {
            StopCoroutine(investigateCoroutine);
            investigateCoroutine = null;
            isInvestigating = false;
            isWaitingAtNoisePoint = false;
        }

        ShowSymbol(alertMat);
        ChaseSound();

        if (agent != null)
        {
            agent.speed = chaseSpeed;
            agent.stoppingDistance = chaseStoppingDistance;
            agent.isStopped = false; 
        }

        if (robotAttack != null)
        {
            robotAttack.EnableAttackTrigger();
        }

        if (fieldOfView != null)
        {
            fieldOfView.SetRadius(chaseRadius, fovTransitionTime);
        }
    }

    void StopChasing()
    {
        isChasing = false;

        HideSymbol();
        NormalSound();

        if (agent != null)
        {
            agent.speed = patrolSpeed;
            agent.stoppingDistance = patrolStoppingDistance;
        }

        if (robotAttack != null)
        {
            robotAttack.DisableAttackTrigger();
        }

        if (fieldOfView != null)
        {
            fieldOfView.ResetRadius(fovTransitionTime);
        }

        UpdateDestination();
    }

    void ChasePlayer()
    {
        if (agent != null && Vector3.Distance(agent.destination, player.position) > 1f)
        {
            agent.SetDestination(player.position);
        }
    }

    void Patrol()
    {
        if (agent == null) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
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
        if (agent != null && agent.velocity.magnitude > 0.1f)
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
        if (agent != null && waypoints != null && waypoints.Length > 0)
        {
            target = waypoints[waypointIndex].position;
            agent.SetDestination(target);
        }
    }

    void IterateWaypointIndex()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        waypointIndex = (waypointIndex + 1) % waypoints.Length;
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

    #region InvestigarRuido
    public void InvestigateNoise(Vector3 noisePosition)
    {
        if (isChasing || isStunned || isBeingManipulated || isAttacking) return;

        if (investigateCoroutine != null)
        {
            StopCoroutine(investigateCoroutine);
        }

        ShowSymbol(investigatingMat);

        investigateCoroutine = StartCoroutine(InvestigateNoiseRoutine(noisePosition));
    }

    private IEnumerator InvestigateNoiseRoutine(Vector3 noisePosition)
    {
        isInvestigating = true;

        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
            isWaiting = false;
        }

        if (agent != null) agent.SetDestination(noisePosition);

        while (!isChasing && agent != null && (agent.pathPending || agent.remainingDistance > investigateArrivalThreshold))
        {
            yield return null;
        }

        if (isChasing)
        {
            investigateCoroutine = null;
            yield break;
        }

        isWaitingAtNoisePoint = true;
        yield return new WaitForSeconds(investigateWaitTime);
        isWaitingAtNoisePoint = false;

        if (!isChasing)
        {
            isInvestigating = false;
            HideSymbol();
            UpdateDestination();
        }

        investigateCoroutine = null;
    }
    #endregion InvestigarRuido

    #region Symbol
    private void ShowSymbol(Material mat)
    {
        if (symbolCoroutine != null)
        {
            StopCoroutine(symbolCoroutine);
            symbolCoroutine = null;
        }

        if (symbolRenderer != null)
        {
            symbolRenderer.enabled = true;
            symbolRenderer.sharedMaterial = mat;
        }

        if (symbolAnim != null)
        {
            symbolAnim.gameObject.SetActive(true); 
            symbolAnim.ResetTrigger("trigger");
            symbolAnim.Play("Entrada", 0, 0f); 
        }
    }

    private void HideSymbol()
    {
        if (symbolCoroutine != null)
        {
            StopCoroutine(symbolCoroutine);
        }

        if (gameObject.activeInHierarchy)
        {
            symbolCoroutine = StartCoroutine(OffSymbol());
        }
    }

    private IEnumerator OffSymbol()
    {
        if (symbolAnim != null)
        {
            symbolAnim.SetTrigger("trigger");
        }

        yield return new WaitForSeconds(offDelay);

        if (symbolRenderer != null)
        {
            symbolRenderer.enabled = false;
        }

        symbolCoroutine = null;
    }
    #endregion Symbol

    #region AplicarStun
    public void ApplyStun(float duracion)
    {
        if (!isStunned)
        {
            if (investigateCoroutine != null)
            {
                StopCoroutine(investigateCoroutine);
                investigateCoroutine = null;
                isInvestigating = false;
                isWaitingAtNoisePoint = false;
            }

            if (gameObject.activeInHierarchy)
            {
                stunCoroutine = StartCoroutine(StunCoroutine(duracion));
            }
        }
    }

    private IEnumerator StunCoroutine(float duracion)
    {
        isStunned = true;

        if (animator != null) animator.speed = 0f;

        if (robotAttack != null) robotAttack.StopRobotAttack();

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

        if (animator != null) animator.speed = 1f;

        isStunned = false;
        canAttack = true;

        if (robotAttack != null) robotAttack.RobotCanAttack();

        if (agent != null) agent.isStopped = false;

        stunCoroutine = null;
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

        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
            isWaiting = false;
        }

        if (investigateCoroutine != null)
        {
            StopCoroutine(investigateCoroutine);
            investigateCoroutine = null;
            isInvestigating = false;
            isWaitingAtNoisePoint = false;
        }

        HideSymbol();

        if (robotAttack != null) robotAttack.StopRobotAttack();
    }
    #endregion AplicarTelekinesis

    #region PararStun
    public void StopStun()
    {
        if (isStunned)
        {
            if (stunCoroutine != null)
            {
                StopCoroutine(stunCoroutine);
                stunCoroutine = null;
            }

            isStunned = false;

            if (animator != null) animator.speed = 1f;

            if (agent != null) agent.isStopped = false;
            StartChasing();
        }
    }
    #endregion PararStun

    #region PararTelekinesis
    public void OnTelekinesisRelease()
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ReleaseFromTelekinesis());
        }
    }

    private IEnumerator ReleaseFromTelekinesis()
    {
        isBeingManipulated = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        yield return new WaitForSeconds(3f);

        if (agent != null)
        {
            agent.isStopped = false;
            isChasing = false;
            agent.speed = patrolSpeed;
            agent.stoppingDistance = patrolStoppingDistance;
            UpdateDestination();

            if (robotAttack != null) robotAttack.DisableAttackTrigger();
        }

        HideSymbol();

        if (robotAttack != null) robotAttack.RobotCanAttack();
    }
    #endregion PararTelekinesis

    #region Audio
    private void NormalSound()
    {
        if (audioSource == null || generalSound == null) return;
        if (audioSource.clip == generalSound && audioSource.isPlaying) return;

        audioSource.clip = generalSound;
        audioSource.loop = true;
        audioSource.Play();
    }

    private void ChaseSound()
    {
        if (audioSource == null || chaseSound == null) return;
        if (audioSource.clip == chaseSound && audioSource.isPlaying) return;

        audioSource.clip = chaseSound;
        audioSource.loop = true;
        audioSource.Play();
    }
    #endregion Audio

    #region DronAttack
    private void OnEnable()
    {
        if (!ActiveEnemies.Contains(this))
            ActiveEnemies.Add(this);
    }

    private void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }
    #endregion DronAttack

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);
    }
}