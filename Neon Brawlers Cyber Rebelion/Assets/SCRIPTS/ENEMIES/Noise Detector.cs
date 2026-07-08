using UnityEngine;

public class NoiseDetector : MonoBehaviour
{
    [SerializeField] private EnemyPatrol enemyPatrol;
    [SerializeField] private float normalNoise = 8f;
    [SerializeField] private float loudNoise = 15f;
    [SerializeField] private float checkInterval = 0.2f;
    [SerializeField] private float minDistanceToRetrigger = 1.5f;

    private Vector3 lastNoiseAHandled = new Vector3(9999f, 9999f, 9999f);
    private Vector3 lastNoiseBHandled = new Vector3(9999f, 9999f, 9999f);
    private float timer;

    void Awake()
    {
        if (enemyPatrol == null)
            enemyPatrol = GetComponent<EnemyPatrol>();
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        timer += Time.deltaTime;
        if (timer < checkInterval) return;
        timer = 0f;

        CheckNoise(GameManager.Instance.normalNoise,GameManager.Instance.normalNoisePos,normalNoise,ref lastNoiseAHandled);

        CheckNoise(GameManager.Instance.loudNoise,GameManager.Instance.loudNoisePos,loudNoise,ref lastNoiseBHandled);
    }

    void CheckNoise(bool noiseActive, Vector3 noisePosition, float radius, ref Vector3 lastHandled)
    {
        if (!noiseActive) return;

        if (enemyPatrol.isChasing || enemyPatrol.isStunned ||
            enemyPatrol.isBeingManipulated || enemyPatrol.isAttacking ||
            enemyPatrol.isInvestigating)
            return;

        float distance = Vector3.Distance(transform.position, noisePosition);
        if (distance > radius) return;

        if (Vector3.Distance(lastHandled, noisePosition) < minDistanceToRetrigger) return;

        lastHandled = noisePosition;
        enemyPatrol.InvestigateNoise(noisePosition);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, normalNoise);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loudNoise);
    }
}
