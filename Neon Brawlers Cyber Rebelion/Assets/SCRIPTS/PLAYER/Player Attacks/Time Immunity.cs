using UnityEngine;
// Script para compensar la velocidad de movimiento al activar el Slow Time
public class TimeImmunity : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private float defaultWalkSpeed;
    private float defaultRunSpeed;
    private float defaultGravity;
    private bool isCompensating = false;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        defaultWalkSpeed = playerMovement.walkSpeed;
        defaultRunSpeed = playerMovement.runSpeed;
        defaultGravity = playerMovement.gravity;
    }

    void Update()
    {
        if (Time.timeScale < 0.8f && Time.timeScale > 0)
        {
            if (!isCompensating) StartCompensation();
            UpdateDynamicValues();
        }
        else if (isCompensating)
        {
            StopCompensation();
        }
    }

    // FixedUpdate ya no es necesario: CharacterController no usa fuerzas de Rigidbody

    void UpdateDynamicValues()
    {
        float multiplier = 1f / Time.timeScale;
        playerMovement.walkSpeed = defaultWalkSpeed * multiplier;
        playerMovement.runSpeed = defaultRunSpeed * multiplier;
        // Compensamos la gravedad igual que antes compensabas la física
        playerMovement.gravity = defaultGravity * multiplier;
    }

    void StartCompensation()
    {
        isCompensating = true;
    }

    void StopCompensation()
    {
        isCompensating = false;
        playerMovement.walkSpeed = defaultWalkSpeed;
        playerMovement.runSpeed = defaultRunSpeed;
        playerMovement.gravity = defaultGravity;
    }
}