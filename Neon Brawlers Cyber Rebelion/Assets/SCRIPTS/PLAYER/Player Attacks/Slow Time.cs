using UnityEngine;
using UnityEngine.UI;

public class SlowTime : MonoBehaviour
{
    [Header("Time Slow Settings")]
    [SerializeField] private float slowMotionScale = 0.2f;
    [SerializeField] private float slowMotionDuration = 5f;

    [Header("Cooldown")]
    [SerializeField] private float cooldownTime = 10f;
    [SerializeField] private Image cooldownUi;

    [Header("Effects")]
    [SerializeField] private ParticleSystem particles;

    [Header("Visual Effects")]
    [SerializeField] private TRAIL trail;
    [SerializeField] private FootstepSpawner footstepSpawner;

    private bool isSlowMotionActive = false;
    private float slowMotionTimer = 0f;

    private void Start()
    {
        // Búsqueda automática si no se asignan en el Inspector
        if (trail == null)
            trail = GetComponentInChildren<TRAIL>();

        if (footstepSpawner == null)
            footstepSpawner = GetComponentInChildren<FootstepSpawner>();
    }

    private void Update()
    {
        if (isSlowMotionActive)
        {
            slowMotionTimer -= Time.unscaledDeltaTime;

            if (slowMotionTimer <= 0)
                DeactivateSlowMotion();
        }

        cooldownUi.fillAmount = HabilidadesManager.instance.cooldownTimer / HabilidadesManager.instance.cooldown;
    }

    public void UseSlowTime()
    {
        if (HabilidadesManager.instance.cooldownTimer <= 0 && !isSlowMotionActive)
        {
            ActivateSlowMotion();
        }
        else if (HabilidadesManager.instance.cooldownTimer > 0)
        {
            Debug.Log($"Slow Motion en cooldown: {HabilidadesManager.instance.cooldownTimer:F1}s");
        }
    }

    private void ActivateSlowMotion()
    {
        isSlowMotionActive = true;
        slowMotionTimer = slowMotionDuration;

        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        particles?.Play();
        AudioManager.instance.Play("slowMotion");
        HabilidadesManager.instance.Cooldown(cooldownTime);

        // Activar efectos visuales
        trail?.StartTrail();
        footstepSpawner?.SetSlowMotionActive(true);

        Debug.Log("Slow Motion Activado");
    }

    private void DeactivateSlowMotion()
    {
        isSlowMotionActive = false;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        // Desactivar efectos visuales
        trail?.StopTrail();
        footstepSpawner?.SetSlowMotionActive(false);

        Debug.Log("Slow Motion Desactivado");
    }
}