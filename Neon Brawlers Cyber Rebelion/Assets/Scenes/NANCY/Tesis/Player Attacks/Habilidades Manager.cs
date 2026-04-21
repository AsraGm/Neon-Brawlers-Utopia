
using UnityEngine;
using UnityEngine.InputSystem;

public class HabilidadesManager : MonoBehaviour
{
    public static HabilidadesManager instance { get; private set; }

    private int selectedAbility = 0;
    [SerializeField] private bool[] unlockedAbilities = { false, false, false };

    public float cooldown;
    public float cooldownTimer = 0;

    [Header("Provisional UI")]
    [SerializeField] private RectTransform arrow;

    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.unscaledDeltaTime;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            selectedAbility = 0;
            AudioManager.instance.Play("changeHability");
            Debug.Log("Habilidad 1 seleccionada: Slow Motion");
            arrow.anchoredPosition = new Vector2(-880, -394);
        }
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            selectedAbility = 1;
            AudioManager.instance.Play("changeHability");
            Debug.Log("Habilidad 2 seleccionada: Electromagnetic wave");
            arrow.anchoredPosition = new Vector2(-287, -394);
        }
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            selectedAbility = 2;
            AudioManager.instance.Play("changeHability");
            Debug.Log("Habilidad 3 seleccionada: Telekinesis");
            arrow.anchoredPosition = new Vector2(374, -394);
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
        switch (selectedAbility)
        {
            case 0:
                GetComponent<SlowTime>()?.UseSlowTime();
                Debug.Log("Se uso habilidad 1");
                break;
            case 1:
                GetComponent<ElectromagneticWave>()?.ActivarOnda();
                Debug.Log("Se uso habilidad 2");
                break;
            case 2:
                GetComponent<Telekinesis>()?.StartTelekinesis();
                Debug.Log("Se uso habilidad 3");
                break;
        }
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
