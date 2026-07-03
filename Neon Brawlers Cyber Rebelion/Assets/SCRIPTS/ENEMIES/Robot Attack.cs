using System.Collections;
using UnityEngine;

public class RobotAttack : MonoBehaviour
{
    [SerializeField] private int damage;
    [SerializeField] private float delayTimeDamage = 3f;
    [SerializeField] private float attackDuration = 1.5f;

    private EnemyPatrol enemyPatrol;
    private Animator animator;


    private void Start()
    {
        enemyPatrol = GetComponent<EnemyPatrol>();
        animator = GetComponentInChildren<Animator>();
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
            return;
        }

        if (enemyPatrol.isAttacking || enemyPatrol.isBeingManipulated) return;

        if (other.CompareTag("Player") && enemyPatrol.canAttack)
        {
            StartCoroutine(AttackSequence(other.gameObject));
        }
    }

    private IEnumerator AttackSequence(GameObject playerObj)
    {
        enemyPatrol.isAttacking = true;
        enemyPatrol.StopAgentForAttack();

        Vector3 directionToPlayer = (playerObj.transform.position - transform.position).normalized;
        directionToPlayer.y = 0;
        if (directionToPlayer != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }

        PlayerMovement playerMovement = playerObj.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.SetMovementLock(true);
        }

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        PlayerHealth playerDamage = playerObj.GetComponent<PlayerHealth>();
        if (playerDamage != null)
        {
            playerDamage.RecibirDanio(damage);
        }

        yield return new WaitForSeconds(attackDuration);

        if (playerMovement != null)
        {
            playerMovement.SetMovementLock(false);
        }

        enemyPatrol.isAttacking = false;
        enemyPatrol.ResumeAgentAfterAttack();

        enemyPatrol.canAttack = false;
        yield return new WaitForSeconds(delayTimeDamage);
        enemyPatrol.canAttack = true;
    }

    public void StopRobotAttack()
    {
        enemyPatrol.canAttack = false;
    }

    public void RobotCanAttack()
    {
        enemyPatrol.canAttack = true;
    }

    private IEnumerator DelayTime()
    {
        yield return new WaitForSeconds(delayTimeDamage);
        RobotCanAttack();
    }

}
