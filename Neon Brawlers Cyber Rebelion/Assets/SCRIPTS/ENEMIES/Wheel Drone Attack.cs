using UnityEngine;
using System.Collections;

public class WheelDroneAttack : MonoBehaviour
{
    [Header("Áreas de Daño")]
    [SerializeField]
    private DamageArea[] damageAreas = new DamageArea[]
        {
        new DamageArea { radius = 2f, damagePerSecond = 20f },  // Área cercana
        new DamageArea { radius = 5f, damagePerSecond = 10f },  // Área media
        new DamageArea { radius = 8f, damagePerSecond = 3f }    // Área lejana
        };

    [Header("Configuración")]
    [SerializeField] private LayerMask playerLayer;
    private float timeInterval = 1f;

    private float nextCheckTime;
    private PlayerDamage currentTarget;
    private EnemyPatrol enemyPatrolScript;

    [SerializeField] private float delayTimeDamage = 3f;

    [System.Serializable]
    public struct DamageArea
    {
        public float radius;
        public float damagePerSecond;
    }

    void Start()
    {
        enemyPatrolScript = GetComponent<EnemyPatrol>();
        // Ordenar áreas de menor a mayor radio 
        System.Array.Sort(damageAreas, (a, b) => a.radius.CompareTo(b.radius));
    }

    void Update()
    {
        if (enemyPatrolScript.isStunned || !enemyPatrolScript.canAttack)
        {
            enemyPatrolScript.canAttack = false;
            return;
        }
        else
        {
            if (enemyPatrolScript.isChasing && Time.time >= nextCheckTime)
            {
                nextCheckTime = Time.time + timeInterval;
                CheckAndApplyDamage();
            }
        }
    }

    void CheckAndApplyDamage()
    {
        // Revisar desde el área más pequeña hacia afuera
        for (int i = 0; i < damageAreas.Length; i++)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, damageAreas[i].radius, playerLayer);

            if (hits.Length > 0)
            {
                //Aplicar daño depende el area
                PlayerDamage player = hits[0].GetComponent<PlayerDamage>();
                if (player != null)
                {
                    float damage = damageAreas[i].damagePerSecond;
                    player.Die(damage);
                    currentTarget = player;
                }
                return; 
            }
        }
        //dejar de hacerlo target si ya esta afuera
        currentTarget = null;
    }

    private void DronCanAttack()
    {
        enemyPatrolScript.canAttack = true;
    }

    private IEnumerator DelayTime()
    {
        yield return new WaitForSeconds(delayTimeDamage);
        DronCanAttack();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (enemyPatrolScript.isStunned)
        {
            if (other.CompareTag("Player"))
            {
                enemyPatrolScript.StopStun();
                StartCoroutine(DelayTime());
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Color[] colors = { new Color(1f, 0.3f, 0f), new Color(1f, 0.5f, 0f), Color.yellow };

        for (int i = 0; i < damageAreas.Length; i++)
        {
            Gizmos.color = colors[i % colors.Length];
            Gizmos.DrawWireSphere(transform.position, damageAreas[i].radius);
        }
    }
}
