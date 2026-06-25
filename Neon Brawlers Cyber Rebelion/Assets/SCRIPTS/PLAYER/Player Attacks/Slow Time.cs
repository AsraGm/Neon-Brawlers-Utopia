using UnityEngine;

public class SlowTime : MonoBehaviour
{
    [Header("Time Slow Settings")]
    [SerializeField] private float slowMotionScale = 0.2f;
    [SerializeField] private float slowMotionDuration = 5f;

    [Header("Cooldown")]
    [SerializeField] private float cooldownTime = 10f;
    [SerializeField] private Renderer _rend;

    [Header("Visual Effects")]
    [SerializeField] private TRAIL trail;
    [SerializeField] private FootstepSpawner footstepSpawner;

    private MaterialPropertyBlock _mpb;
    private bool isSlowMotionActive = false;
    private float slowMotionTimer = 0f;
    public static bool IsSlowActive { get; private set; }


    private void Start()
    {
        // Búsqueda automática si no se asignan en el Inspector
        if (trail == null)
            trail = GetComponentInChildren<TRAIL>();

        if (footstepSpawner == null)
            footstepSpawner = GetComponentInChildren<FootstepSpawner>();

        _mpb = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (isSlowMotionActive)
        {
            slowMotionTimer -= Time.unscaledDeltaTime;

            if (slowMotionTimer <= 0)
                DeactivateSlowMotion();
        }

        UpdateCooldownShader();
    }

    private void UpdateCooldownShader()
    {
        float progress = (HabilidadesManager.instance.cooldownTimer / HabilidadesManager.instance.cooldown) * 5f;
        _mpb.SetFloat("_Remove_Segments", progress);
        _rend.SetPropertyBlock(_mpb);
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

        IsSlowActive = true;       

        isSlowMotionActive = true;
        slowMotionTimer = slowMotionDuration;

        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        AudioManager.instance.Play("slowMotion");
        HabilidadesManager.instance.Cooldown(cooldownTime);

        // Activar efectos visuales
        trail?.StartTrail();
        footstepSpawner?.SetSlowMotionActive(true);

        Debug.Log("Slow Motion Activado");
    }

    private void DeactivateSlowMotion()
    {
        IsSlowActive = false;
        isSlowMotionActive = false;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        // Desactivar efectos visuales
        trail?.StopTrail();
        footstepSpawner?.SetSlowMotionActive(false);

        Debug.Log("Slow Motion Desactivado");
    }
}