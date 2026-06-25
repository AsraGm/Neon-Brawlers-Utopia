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
        {
            cooldownTimer -= Time.unscaledDeltaTime;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            if (unlockedAbilities[0])
            {
                selectedAbility = 0;
                AudioManager.instance.Play("changeHability");
                Debug.Log("Habilidad 1 seleccionada: Slow Motion");
                SlowMoSelected?.Invoke();
            }
            else
            {
                Debug.Log("Habilidad 1 no desbloqueada");
            }
        }
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            if (unlockedAbilities[1])
            {
                selectedAbility = 1;
                AudioManager.instance.Play("changeHability");
                Debug.Log("Habilidad 2 seleccionada: Electromagnetic wave");
                ElectroWaveSelected?.Invoke();
            }
            else
            {
                Debug.Log("Habilidad 2 no desbloqueada");
            }
        }
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            if (unlockedAbilities[2])
            {
                selectedAbility = 2;
                AudioManager.instance.Play("changeHability");
                Debug.Log("Habilidad 3 seleccionada: Telekinesis");
                TelekinesisSelected?.Invoke();
            }
            else
            {
                Debug.Log("Habilidad 3 no desbloqueada");
            }
        }

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            if (unlockedAbilities[selectedAbility])
            {
                ExecuteAbility();
            }
            else
            {
                Debug.Log("Esta habilidad aun no esta desbloqueada");
            }
        }

        if (Keyboard.current.cKey.wasReleasedThisFrame)
        {
            AbilityRelease();
        }
    }

    void ExecuteAbility()
    {
        IsUsingAbility = true;

        switch (selectedAbility)
        {
            case 0:
                playerAnimator?.SetTrigger("doSlowTime");  
                GetComponent<SlowTime>()?.UseSlowTime();
                break;
            case 1:
                playerAnimator?.SetTrigger("doElectroWave"); 
                GetComponent<ElectromagneticWave>()?.ActivarOnda();
                break;
            case 2:
                playerAnimator?.SetTrigger("doTelekinesis"); 
                GetComponent<Telekinesis>()?.StartTelekinesis();
                break;
        }

        IsUsingAbility = false;
    }

    private void AbilityRelease()
    {
        if (selectedAbility == 2)
        {
            GetComponent<Telekinesis>()?.StopTelekinesis();
        }
    }

    public void Cooldown(float cooldownTime)
    {
        cooldownTimer = cooldownTime;
        cooldown = cooldownTime;
    }

    //para desbloquear habilidades
    public void UnlockAbility(int abilityIndex)
    {
        if (abilityIndex >= 0 && abilityIndex < 3)
        {
            unlockedAbilities[abilityIndex] = true;
            Debug.Log($"Se desbloqueo la habilidad: {abilityIndex + 1}");
        }
    }

    //verificar si la habilidad esta desbloqueada
    public bool IsAbilityUnlocked(int abilityIndex)
    {
        return unlockedAbilities[abilityIndex];
    }
}
