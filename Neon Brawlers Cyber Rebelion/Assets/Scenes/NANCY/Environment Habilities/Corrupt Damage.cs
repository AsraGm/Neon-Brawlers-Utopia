using UnityEngine;

public class CorruptDamage : MonoBehaviour
{
    [Header("Damage Areas")]
    [SerializeField]
    private DamageArea[] damageAreas = new DamageArea[]
        {
        new DamageArea { radius = 2f, damagePerSecond = 20f },  // Área cercana
        new DamageArea { radius = 5f, damagePerSecond = 10f },  // Área media
        new DamageArea { radius = 8f, damagePerSecond = 3f }    // Área lejana
        };

    [Header("Configuración")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstructionMask;

    private float timeInterval = 1f;
    private float nextCheckTime;
    private PlayerHealth currentTarget;

    [System.Serializable]
    public struct DamageArea
    {
        public float radius;
        public float damagePerSecond;
    }

    void Start()
    {
        // Ordenar áreas de menor a mayor radio 
        System.Array.Sort(damageAreas, (a, b) => a.radius.CompareTo(b.radius));
    }

    void Update()
    {
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + timeInterval;
            CheckAndApplyDamage();
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
                PlayerHealth player = hits[0].GetComponent<PlayerHealth>();
                if (player != null)
                {
                    Transform target = hits[0].transform;
                    Vector3 directionToTarget = (target.position - transform.position).normalized;
                    float distanceToTarget = Vector3.Distance(transform.position, target.position);

                    if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstructionMask))
                    {
                        float damage = damageAreas[i].damagePerSecond;
                        player.RecibirDanio(damage);
                        currentTarget = player;
                    }
                    else
                    {
                        currentTarget = null;
                    }
                }

                return;
            }
        }
        //dejar de hacerlo target si ya esta afuera
        currentTarget = null;
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
