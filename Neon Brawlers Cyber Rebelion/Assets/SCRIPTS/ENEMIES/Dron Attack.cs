using UnityEngine;
using System.Collections;

public class DronAttack : MonoBehaviour
{
    [Header("Rangos de Distancias")]
    [Tooltip("Area donde los robots se activan")]
    [SerializeField] private float detectionRange = 10f;
    [Tooltip("Distancia del jugador para activar a los robots")]
    [SerializeField] private float attackRange = 3f;

    [Header("Configuracion")]
    [Tooltip("Los tipo de enemigos que va a alertar")]
    [SerializeField] private LayerMask EnemyLayer;
    [SerializeField] private LayerMask obstructionMask;
    [Tooltip("Tiempo para reactivar el daño al ser reactivados del stun")]
    [SerializeField] private float delayTimeDamage = 3f;

    private Transform player;
    private EnemyPatrol enemyPatrol;

    private void Start()
    {
        player = GameObject.Find("Player").transform;
        enemyPatrol = GetComponent<EnemyPatrol>();
    }

    private void Update()
    {
        if (enemyPatrol.isStunned || !enemyPatrol.canAttack)
        {
            enemyPatrol.canAttack = false;
            return;
        }
        else
        {
            CheckDistance();
        }
    }

    private void CheckDistance()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= attackRange)
        {
            Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;

            //revisa que no haya una pared entre el jugador y solo activar los robots cuando este de frente
            if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstructionMask))
            {
                Debug.Log("El enemigo ya esta pegado al player");
                Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRange, EnemyLayer);

                foreach (Collider col in hitColliders)
                {
                    EnemyPatrol patrolScript = col.GetComponent<EnemyPatrol>();

                    if (patrolScript != null)
                    {
                        patrolScript.alertedByDrone = true;
                    }
                }
            }
        }
    }

    private void DronCanAttack()
    {
        enemyPatrol.canAttack = true;
    }

    private IEnumerator DelayTime()
    {
        yield return new WaitForSeconds(delayTimeDamage);
        DronCanAttack();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (enemyPatrol.isStunned)
        {
            if (other.CompareTag("Player"))
            {
                enemyPatrol.StopStun();
                StartCoroutine(DelayTime());
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.DrawRay(transform.position, transform.forward * attackRange);
    }
}
