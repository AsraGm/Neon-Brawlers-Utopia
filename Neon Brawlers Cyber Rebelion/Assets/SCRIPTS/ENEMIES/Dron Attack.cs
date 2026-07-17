using UnityEngine;
using System.Collections;

public class DronAttack : MonoBehaviour
{
    [Header("Rangos de Distancias")]
    [Tooltip("Area donde los robots se activan")]
    [SerializeField] private float detectionRange = 10f;
    [Tooltip("Distancia del jugador para activar a los robots")]
    [SerializeField] private float attackRange = 3f;

    [Header("Config")]
    [SerializeField] private LayerMask enemyLayer;
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
        if (enemyPatrol.isStunned || enemyPatrol.isBeingManipulated) return;
        if (!enemyPatrol.canAttack) return;

        CheckDistance();
    }

    private void CheckDistance()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > attackRange) return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;

        // Solo alerta si tiene rango de vision directa al jugador
        if (Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstructionMask))
            return;

        AlertNearbyEnemies();
    }

    private void AlertNearbyEnemies()
    {
        foreach (EnemyPatrol other in EnemyPatrol.ActiveEnemies)
        {
            if (other == enemyPatrol) continue;//asegurarme que no se alerte 

            if (((1 << other.gameObject.layer) & enemyLayer) == 0) continue;

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist <= detectionRange)
            {
                other.alertedByDrone = true;
            }
        }
    }

    private void DronCanAttack() => enemyPatrol.canAttack = true;

    private IEnumerator DelayTime()
    {
        yield return new WaitForSeconds(delayTimeDamage);
        DronCanAttack();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (enemyPatrol.isStunned && other.CompareTag("Player"))
        {
            enemyPatrol.StopStun();
            StartCoroutine(DelayTime());
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.DrawRay(transform.position, transform.forward * attackRange);
    }
}
