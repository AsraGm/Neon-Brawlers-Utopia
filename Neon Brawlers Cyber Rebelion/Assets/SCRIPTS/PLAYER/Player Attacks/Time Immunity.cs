using UnityEngine;

//Script para compensar la velocidad de movimiento al activar el Slow Time
public class TimeImmunity : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private Rigidbody rb;

    private float defaultWalkSpeed;
    private float defaultRunSpeed;
    private float defaultDrag;

    private bool isCompensating = false;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody>();

        defaultWalkSpeed = playerMovement.walkSpeed;
        defaultRunSpeed = playerMovement.runSpeed;
        defaultDrag = playerMovement.groundDrag;
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

    void FixedUpdate()
    {
        if (isCompensating)
        {
            float gravityMultiplier = 1f / (Time.timeScale * Time.timeScale);
            rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);

            float forceMultiplier = (1f / Time.timeScale) - 1f;

            Vector3 moveDir = playerMovement.orientation.forward * Input.GetAxisRaw("Vertical") +
                              playerMovement.orientation.right * Input.GetAxisRaw("Horizontal");

            if (moveDir.magnitude > 0.1f)
            {
                rb.AddForce(moveDir.normalized * playerMovement.walkSpeed * 10f * forceMultiplier, ForceMode.Force);
            }
        }
    }

    void UpdateDynamicValues()
    {
        float multiplier = 1f / Time.timeScale;

        playerMovement.walkSpeed = defaultWalkSpeed * multiplier;
        playerMovement.runSpeed = defaultRunSpeed * multiplier;
        playerMovement.groundDrag = defaultDrag * multiplier;
    }

    void StartCompensation()
    {
        isCompensating = true;
        rb.useGravity = false;
    }

    void StopCompensation()
    {
        isCompensating = false;
        rb.useGravity = true;

        playerMovement.walkSpeed = defaultWalkSpeed;
        playerMovement.runSpeed = defaultRunSpeed;
        playerMovement.groundDrag = defaultDrag;
    }
}
