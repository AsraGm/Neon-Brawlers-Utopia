using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float walkSpeed = 6f;
    public float runSpeed = 10f;
    float currentSpeed;

    [Header("Gravedad")]
    public float gravity = -20f;
    private float verticalVelocity = 0f;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    [Header("Stamina")]
    public float maxStamina = 4f;
    public float staminaRecoveryTime = 5f;
    private float currentStamina;
    private float recoveryTimer = 0f;
    private bool isRecovering = false;
    private bool staminaDepleted = false;

    // Propiedades para el HUD??? a ver si asi era nancy
    public float StaminaNormalized => currentStamina / maxStamina;
    public bool IsRecovering => isRecovering;

    [Header("Agachado")]
    public float crouchHeight = 1f;        // altura del CC al agacharse
    public float crouchWalkSpeed = 2f;     // <-- velocidad al caminar agachado
    public float colliderLerpSpeed = 4f;   // renombrado para que sea claro
    private float normalHeight;
    private Vector3 normalCenter;
    public bool isCrouching = false;
    public LayerMask ceilingMask;          // para el raycast de techo
    public float ceilingCheckDistance = 0.5f; // margen extra sobre la cabeza
    public Transform orientation;

    Vector2 moveInput;
    Vector3 moveDirection;

    CharacterController cc;

    // variables para la interaccion con obstaculos
    Transform currentSnapPoint;
    float snapSpeed;
    bool inObstacle;

    // para las escaleras
    private bool onStairs;

    // referencia al efecto de velocidad
    public UniversalRendererData rendererData;
    ScriptableRendererFeature fullScreenFeature;

    Animator Playeranimator;

    // referencia al script de la cámara
    public ThirdPersonCam camScript;

    private void Start()
    {
        cc = GetComponent<CharacterController>();
        Playeranimator = GetComponentInChildren<Animator>();
        Playeranimator.updateMode = AnimatorUpdateMode.UnscaledTime;

        normalHeight = cc.height;
        normalCenter = cc.center;

        currentStamina = maxStamina;

        currentSpeed = walkSpeed;

        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature.name == "FSSpeed")
            {
                fullScreenFeature = feature;
                break;
            }
        }
    }

    private void Update()
    {
        // Ground check igual que antes con raycast
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        ReadInput();
        RunPlayer();
        UpdateAnimations();
        HandleGravity();

        if (!inObstacle)
            MovePlayer();
        else
            HandleObstacleMovement();
    }

    // FixedUpdate ya no es necesario, CharacterController va en Update

    private void HandleGravity()
    {
        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // pequeño valor negativo para mantenerlo pegado al suelo
        else
            verticalVelocity += gravity * Time.deltaTime;
    }

    private void ReadInput()
    {
        if (Keyboard.current == null) return;

        // Si está bloqueado, limpiar input y salir
        if (IsLocked)
        {
            moveInput = Vector2.zero;
            return;
        }

        float horizontal =
            (Keyboard.current.dKey.isPressed ? 1 : 0) -
            (Keyboard.current.aKey.isPressed ? 1 : 0);

        float vertical =
            (Keyboard.current.wKey.isPressed ? 1 : 0) -
            (Keyboard.current.sKey.isPressed ? 1 : 0);

        moveInput = new Vector2(horizontal, vertical);

        // Agachado con V
        if (Keyboard.current.vKey.wasPressedThisFrame)
        {
            if (!isCrouching)
            {
                StartCrouch();
                Playeranimator?.SetTrigger("doCrouch");  // <-- dispara la animación una sola vez
            }
            else
                TryStandUp();
        }

        if (inObstacle && currentSnapPoint != null &&
            Vector3.Distance(transform.position, currentSnapPoint.position) > 1.2f)
        {
            ExitObstacleMode();
        }
    }

    private void MovePlayer()
    {
        if (IsLocked) return; // bloquea todo este metodo si estas escondido en un obstacle

        moveDirection = orientation.forward * moveInput.y + orientation.right * moveInput.x;

        Vector3 flatMove = moveDirection;
        flatMove.y = 0f;

        if (flatMove.magnitude < 0.1f)
        {
            // Sin input horizontal pero sí aplicamos gravedad
            cc.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
            return;
        }

        Vector3 motion;

        if (onStairs)
        {
            // Proyectar sobre la normal de la escalera igual que antes
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit stairsHit,
                playerHeight * 0.6f, whatIsGround))
            {
                Vector3 slopeDir = Vector3.ProjectOnPlane(flatMove, stairsHit.normal).normalized;

                float stairSpeed = walkSpeed;
                if (slopeDir.y > 0.01f)
                    stairSpeed *= 2f;

                motion = slopeDir * stairSpeed;
                // En escaleras no aplicamos gravedad para evitar rebotes
                motion.y = Mathf.Max(motion.y, 0f);
            }
            else
            {
                motion = flatMove.normalized * currentSpeed;
                motion.y = verticalVelocity;
            }
        }
        else
        {
            motion = flatMove.normalized * currentSpeed;
            motion.y = verticalVelocity;
        }

        cc.Move(motion * (isCrouching ? Time.unscaledDeltaTime : Time.deltaTime));

        // Rotación hacia donde se mueve (igual que antes)
        Vector3 flatDirection = new Vector3(moveDirection.x, 0f, moveDirection.z);
        if (flatDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flatDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.unscaledDeltaTime * 10f
            );
        }
    }

    private void RunPlayer()
    {
        bool isMoving = moveInput.magnitude > 0.1f;
        bool shiftHeld = Keyboard.current.leftShiftKey.isPressed;

        // Si presiona shift mientras recupera, detener recuperación
        if (shiftHeld && isRecovering)
        {
            isRecovering = false;
            recoveryTimer = 0f;
        }

        // Recuperación total (solo tras agotamiento completo)
        if (!shiftHeld && staminaDepleted)
        {
            if (!isRecovering)
            {
                isRecovering = true;
                recoveryTimer = 0f;
            }

            recoveryTimer += Time.deltaTime;

            HabilidadesManager.instance.cooldown = staminaRecoveryTime;
            HabilidadesManager.instance.cooldownTimer = staminaRecoveryTime - recoveryTimer;

            if (recoveryTimer >= staminaRecoveryTime)
            {
                currentStamina = maxStamina;
                staminaDepleted = false;
                isRecovering = false;
                recoveryTimer = 0f;
                HabilidadesManager.instance.cooldownTimer = 0f;
            }
        }

        bool canRun = !staminaDepleted && currentStamina > 0f;
        bool isRunning = !onStairs &&
                         !isCrouching &&
                         shiftHeld &&
                         isMoving &&
                         canRun;

        // Consumir stamina al correr
        if (isRunning)
        {
            currentStamina -= Time.unscaledDeltaTime;

            float staminaDamage = maxStamina - currentStamina;

            if (staminaDamage > HabilidadesManager.instance.cooldownTimer)
            {
                HabilidadesManager.instance.cooldown = maxStamina;
                HabilidadesManager.instance.cooldownTimer = staminaDamage;
            }


            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                staminaDepleted = true;
                isRecovering = false;
                recoveryTimer = 0f;

                HabilidadesManager.instance.cooldown = staminaRecoveryTime;
                HabilidadesManager.instance.cooldownTimer = staminaRecoveryTime;
            }
        }
        else if (!shiftHeld && !staminaDepleted && currentStamina < maxStamina)
        {
            // Recuperación gradual si suelta shift sin haberse agotado
            currentStamina += Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }

        // Velocidad según estado
        if (isCrouching)
            currentSpeed = crouchWalkSpeed;
        else
            currentSpeed = isRunning ? runSpeed : walkSpeed;

        camScript.SetRunning(isRunning);
        SetEffect(isRunning);
    }

    void StartCrouch()
    {
        isCrouching = true;

        // Calculamos el nuevo center para que la BASE del collider no se mueva
        float centerY = normalCenter.y - (normalHeight - crouchHeight) / 2f;

        cc.height = crouchHeight;
        cc.center = new Vector3(normalCenter.x, centerY, normalCenter.z);
    }

    void TryStandUp()
    {
        // Raycast desde la cabeza hacia arriba para verificar espacio
        Vector3 topOfCapsule = transform.position + Vector3.up * (cc.height + cc.skinWidth);

        bool blocked = Physics.Raycast(topOfCapsule, Vector3.up, ceilingCheckDistance, ceilingMask);

        if (blocked)
        {
            Debug.Log("No hay espacio para levantarse");
            return;
        }

        isCrouching = false;
        cc.height = normalHeight;
        cc.center = normalCenter;
    }
    void SetEffect(bool active)
    {
        if (fullScreenFeature != null)
            fullScreenFeature.SetActive(active);
    }

    private void UpdateAnimations()
    {
        if (Playeranimator == null) return;

        bool isMoving = moveInput.magnitude > 0.1f;
        bool isUsingAbility = HabilidadesManager.instance != null &&
                              HabilidadesManager.instance.IsUsingAbility;

        // Calcular si está corriendo para la animación
        bool shiftHeld = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        bool isRunning = !onStairs && !isCrouching && shiftHeld && isMoving && !staminaDepleted && currentStamina > 0f;

        Playeranimator.SetBool("isMoving", isMoving);
        Playeranimator.SetBool("isRunning", isRunning);
        Playeranimator.SetBool("isUsingAbility", isUsingAbility);
        Playeranimator.SetBool("isCrouching", isCrouching);
    }

    void HandleObstacleMovement()
    {
        if (currentSnapPoint == null) return;

        // Replicamos la lógica original: movimiento solo sobre el eje paralelo a la pared
        Vector3 toPlayer = transform.position - currentSnapPoint.position;
        Vector3 wallNormal = currentSnapPoint.right;
        Vector3 parallel = Vector3.ProjectOnPlane(toPlayer, wallNormal);
        Vector3 move = currentSnapPoint.forward * moveInput.y;

        Vector3 targetPos = currentSnapPoint.position + parallel + move;
        Vector3 delta = targetPos - transform.position;

        // Usamos Move en vez de MovePosition, ignoramos la Y para no luchar con la gravedad
        Vector3 slideMotion = new Vector3(delta.x, verticalVelocity * Time.deltaTime, delta.z);
        cc.Move(slideMotion * snapSpeed * Time.unscaledDeltaTime);
    }

    // Triggers para escaleras (sin cambios en lógica)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Stairs"))
        {
            onStairs = true;
            currentSpeed = walkSpeed;
            camScript.SetRunning(false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Stairs"))
            onStairs = false;
    }


    // Métodos públicos que ObstacleInteraction sigue llamando igual
    public bool IsLocked { get; private set; }

    // Modo hide: mueve libremente pero a velocidad reducida, sin bloquear
    public void EnterHideMode(Transform snapPoint, float snapSpeed)
    {
        inObstacle = true;
        IsLocked = false;         // libre para moverse
        currentSnapPoint = snapPoint;
        this.snapSpeed = snapSpeed;
        currentSpeed = crouchWalkSpeed;  // velocidad reducida
        verticalVelocity = 0f;
    }

    public void ExitHideMode()
    {
        inObstacle = false;
        IsLocked = false;
        currentSnapPoint = null;
        currentSpeed = walkSpeed;     // restaurar velocidad normal
    }

    public void EnterObstacleMode(Transform snapPoint, float snapSpeed)
    {
        inObstacle = true;
        IsLocked = true;
        currentSnapPoint = snapPoint;
        this.snapSpeed = snapSpeed;
        currentSpeed = walkSpeed;
        verticalVelocity = 0f;
    }

    public void ExitObstacleMode()
    {
        inObstacle = false;
        IsLocked = false;
        currentSnapPoint = null;
    }
}
