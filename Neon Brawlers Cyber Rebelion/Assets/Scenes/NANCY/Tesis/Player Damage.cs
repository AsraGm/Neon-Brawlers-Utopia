using UnityEngine;

public class PlayerDamage : MonoBehaviour
{
    [Range(0, 100)]
    public float vida;

    public void Die(float damage)
    {
        vida -= damage;

        if (vida <= 0)
        {
            transform.position = new Vector3(20, 5, 142);
        }
    }
}
