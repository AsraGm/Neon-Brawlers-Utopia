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
    [SerializeField] ParticleSystem particles;

    private bool isSlowMotionActive = false;
    private float slowMotionTimer = 0f;

    private void Update()
    {
        if (isSlowMotionActive)
        {
            slowMotionTimer -= Time.unscaledDeltaTime;

            if (slowMotionTimer <= 0)
            {
                DeactivateSlowMotion();
            }
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
            Debug.Log($"Slow Motion en cooldown: {HabilidadesManager.instance.cooldownTimer}");
        }
    }

    private void ActivateSlowMotion()
    {
        isSlowMotionActive = true;
        slowMotionTimer = slowMotionDuration;

        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        particles.Play();
        AudioManager.instance.Play("slowMotion");

        HabilidadesManager.instance.Cooldown(cooldownTime);

        Debug.Log("Slow Motion Activado");
    }

    private void DeactivateSlowMotion()
    {
        isSlowMotionActive = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        Debug.Log("Slow Motion Desactivado");
    }
}
