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

    private bool isSlowMotionActive = false;
    private float slowMotionTimer = 0f;
    private float cooldownTimer = 0f;

    private void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.unscaledDeltaTime;
        }

        if (isSlowMotionActive)
        {
            slowMotionTimer -= Time.unscaledDeltaTime;

            if (slowMotionTimer <= 0)
            {
                DeactivateSlowMotion();
            }
        }

        cooldownUi.fillAmount = cooldownTimer / cooldownTime;
    }

    public void UseSlowTime()
    {
        if (cooldownTimer <= 0 && !isSlowMotionActive)
        {
            ActivateSlowMotion();
        }
        else if (cooldownTimer > 0)
        {
            Debug.Log($"Slow Motion en cooldown: {cooldownTimer:F1}s");
        }
    }

    private void ActivateSlowMotion()
    {
        isSlowMotionActive = true;
        slowMotionTimer = slowMotionDuration;
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        Debug.Log("Slow Motion Activado");
    }

    private void DeactivateSlowMotion()
    {
        isSlowMotionActive = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        cooldownTimer = cooldownTime;

        Debug.Log("Slow Motion Desactivado");
    }
}
