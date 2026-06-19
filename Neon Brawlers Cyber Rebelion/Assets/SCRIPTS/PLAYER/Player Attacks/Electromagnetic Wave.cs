using UnityEngine;

public class ElectromagneticWave : MonoBehaviour
{
    [Header("Configuración de Onda")]
    [SerializeField] private float waveRange = 10f;
    [SerializeField] private float stunDuration = 3f;

    [Header("Cooldown")]
    [SerializeField] private float cooldown = 5f;
    [SerializeField] private Renderer _rend;

    [Header("Capas")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Effects")]
    [SerializeField] ParticleSystem particles;

    private MaterialPropertyBlock _mpb;

    private void Start()
    {
        _mpb = new MaterialPropertyBlock();
    }

    private void Update()
    {
        float progress = (HabilidadesManager.instance.cooldownTimer / HabilidadesManager.instance.cooldown) * 5f;
        _mpb.SetFloat("_Remove_Segments", progress);
        _rend.SetPropertyBlock(_mpb);
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

            InteractableDoor door = col.GetComponentInChildren<InteractableDoor>();
            if (door == null)
                door = col.GetComponentInParent<InteractableDoor>();
            if (door != null)
                door.ApplyStun(stunDuration);

            ElectricCurrentDamage electricity = col.GetComponentInChildren<ElectricCurrentDamage>();
            if (electricity == null)
                electricity = col.GetComponentInParent<ElectricCurrentDamage>();
            if (electricity != null)
                electricity.ApplyStun();

            Detector detector = col.GetComponentInChildren<Detector>();
            if (detector == null)
                detector = col.GetComponentInParent<Detector>();
            if (detector != null)
                detector.ApplyStun();
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
