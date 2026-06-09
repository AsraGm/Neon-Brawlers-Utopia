using System.Collections;
using UnityEngine;

public class DamageDetector : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float baseDamage = 5f;
    [SerializeField] private PlayerHealth player;

    private float currentDamage;
    private Coroutine damageCoroutine;

    void OnEnable()
    {
        Debug.Log(player.vidaActual);

        currentDamage = baseDamage;

        if (damageCoroutine != null)
            StopCoroutine(damageCoroutine);

        damageCoroutine = StartCoroutine(DamageLoop());
    }

    void OnDisable()
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
    }

    private IEnumerator DamageLoop()
    {
        while (player.vidaActual > 0 && player.EstaVivo())
        {
            player.RecibirDanio(currentDamage);
            currentDamage *= 2;

            if (!player.EstaVivo())
            {
                this.enabled = false;
                yield break;
            }

            yield return new WaitForSeconds(1f);
        }

        this.enabled = false;
    }
}
