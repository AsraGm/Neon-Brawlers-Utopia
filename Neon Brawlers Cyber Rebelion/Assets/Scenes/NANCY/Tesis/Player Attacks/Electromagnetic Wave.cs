using UnityEngine;
using UnityEngine.UI;

public class ElectromagneticWave : MonoBehaviour
{
    [Header("Configuración de Onda")]
    [SerializeField] private float waveRange = 10f;
    [SerializeField] private float stunDuration = 3f;
    [SerializeField] private float cooldown = 5f;

    [Header("Capas")]
    [SerializeField] private LayerMask enemyLayer;

    [SerializeField] private Image cooldownUi;

    private bool isCooldown = false;
    private float cooldownTimer = 0f;

    private void Update()
    {
        if (isCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isCooldown = false;
            }
        }

        cooldownUi.fillAmount = cooldownTimer / cooldown;
    }

    public void ActivarOnda()
    {
        if (isCooldown)
        {
            Debug.Log("Espera cooldown");
            return;
        }

        Collider[] enemigos = Physics.OverlapSphere(transform.position, waveRange, enemyLayer);

        Debug.Log($"Enemigos detectados: {enemigos.Length}");

        foreach (Collider col in enemigos)
        {
            EnemyPatrol enemy = col.GetComponentInChildren<EnemyPatrol>();

            if (enemy == null)
            {
                enemy = col.GetComponentInParent<EnemyPatrol>();
            }

            if (enemy != null)
            {
                enemy.ApplyStun(stunDuration);
            }
        }

        isCooldown = true;
        cooldownTimer = cooldown;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, waveRange);
    }

    public bool EstaCooldown()
    {
        return isCooldown;
    }

    public float GetCooldownRestante()
    {
        return cooldownTimer;
    }
}
