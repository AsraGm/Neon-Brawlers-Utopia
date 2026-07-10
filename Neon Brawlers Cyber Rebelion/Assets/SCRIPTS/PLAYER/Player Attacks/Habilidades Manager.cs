using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class HabilidadesManager : MonoBehaviour
{
    public static HabilidadesManager instance { get; private set; }

    private int selectedAbility = 0;
    [SerializeField] private bool[] unlockedAbilities = { false, false, false };

    public bool IsUsingAbility { get; private set; }

    private Animator playerAnimator;

    public float cooldown;
    public float cooldownTimer = 0;
    public bool playerIsHiding;

    [Header("Habilidades UI")]
    public UnityEvent ElectroWaveSelected;
    public UnityEvent SlowMoSelected;
    public UnityEvent TelekinesisSelected;

    void Start()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        playerAnimator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (cooldownTimer > 0)
            cooldownTimer -= Time.unscaledDeltaTime;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            if (unlockedAbilities[0]) { selectedAbility = 0; AudioManager.instance.Play("changeHability"); SlowMoSelected?.Invoke(); }
            else Debug.Log("Habilidad 1 no desbloqueada");
        }
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            if (unlockedAbilities[1]) { selectedAbility = 1; AudioManager.instance.Play("changeHability"); ElectroWaveSelected?.Invoke(); }
            else Debug.Log("Habilidad 2 no desbloqueada");
        }
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            if (unlockedAbilities[2]) { selectedAbility = 2; AudioManager.instance.Play("changeHability"); TelekinesisSelected?.Invoke(); }
            else Debug.Log("Habilidad 3 no desbloqueada");
        }

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            if (unlockedAbilities[selectedAbility])
                ExecuteAbility();
            else
                Debug.Log("Esta habilidad aun no esta desbloqueada");
        }

        if (Keyboard.current.cKey.wasReleasedThisFrame)
            AbilityRelease();
    }

    void ExecuteAbility()
    {
        switch (selectedAbility)
        {
            case 0:
                // Solo animar si el cooldown lo permite
                if (cooldownTimer <= 0 && !SlowTime.IsSlowActive)
                {
                    IsUsingAbility = true;
                    playerAnimator?.SetTrigger("doSlowTime");
                }
                GetComponent<SlowTime>()?.UseSlowTime();
                break;

            case 1:
                // Solo animar si el cooldown lo permite
                if (cooldownTimer <= 0)
                {
                    IsUsingAbility = true;
                    playerAnimator?.SetTrigger("doElectroWave");
                }
                GetComponent<ElectromagneticWave>()?.ActivarOnda();
                break;

            case 2:
                // Solo animar si el cooldown lo permite y no hay objeto agarrado
                var telekinesis = GetComponent<Telekinesis>();
                if (cooldownTimer <= 0)
                {
                    IsUsingAbility = true;
                    playerAnimator?.SetTrigger("doTelekinesis");
                }
                telekinesis?.StartTelekinesis();
                break;
        }

        // Resetear en el siguiente frame para no bloquear el Animator
        IsUsingAbility = false;
    }

    private void AbilityRelease()
    {
        if (selectedAbility == 2)
            GetComponent<Telekinesis>()?.StopTelekinesis();
    }

    public void Cooldown(float cooldownTime)
    {
        cooldownTimer = cooldownTime;
        cooldown = cooldownTime;
    }

    public void UnlockAbility(int abilityIndex)
    {
        if (abilityIndex >= 0 && abilityIndex < 3)
        {
            unlockedAbilities[abilityIndex] = true;
            Debug.Log($"Se desbloqueo la habilidad: {abilityIndex + 1}");
        }
    }

    public bool IsAbilityUnlocked(int abilityIndex)
    {
        return unlockedAbilities[abilityIndex];
    }
}