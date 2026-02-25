using System.Collections;
using UnityEngine;

public class PlayerDamage : MonoBehaviour
{
    [Range(0, 100)]
    public float vida;

    [SerializeField] private Transform spawn;

    public void Die(float damage)
    {
        vida -= damage;

        if (vida <= 0)
        {
            transform.position = spawn.position;
        }
    }

}
