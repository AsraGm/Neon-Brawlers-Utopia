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

    [Header("Effects")]
    [SerializeField] ParticleSystem particles;

    private void Update()
    {
        cooldownUi.fillAmount = HabilidadesManager.instance.cooldownTimer / HabilidadesManager.instance.cooldown;
    }

    public void ActivarOnda()
    {
        if (HabilidadesManager.instance.cooldownTimer > 0f)
        {
            Debug.Log("Espera cooldown");
            return;
        }

        HabilidadesManager.instance.Cooldown(cooldown);

        particles.Play();
        AudioManager.instance.Play("electromagneticWave");

        Collider[] enemigos = Physics.OverlapSphere(transform.position, waveRange, enemyLayer);
        Debug.Log($"Enemigos detectados: {enemigos.Length}");

        foreach (Collider col in enemigos)
        {
            EnemyPatrol enemy = col.GetComponentInChildren<EnemyPatrol>();
            if (enemy == null)
                enemy = col.GetComponentInParent<EnemyPatrol>();

            if (enemy != null)
                enemy.ApplyStun(stunDuration);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, waveRange);
    }

    public bool EstaCooldown()
    {
        return HabilidadesManager.instance.cooldownTimer > 0f;
    }
    public float GetCooldownRestante()
    {
        return HabilidadesManager.instance.cooldownTimer;
    }
}
