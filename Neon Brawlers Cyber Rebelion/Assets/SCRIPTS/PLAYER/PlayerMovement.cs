using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class PlayerMovement : MonoBehaviour
{
    #region Referencias

    CharacterController cc;
    Animator Playeranimator;

    [Tooltip("Transform que define la dirección hacia donde mira/se mueve el jugador")]
    public Transform orientation;

    #endregion

    #region Movimiento

    [Header("Movimiento")]
    [Tooltip("Velocidad de caminata normal")]
    public float walkSpeed = 6f;

    [Tooltip("Velocidad al correr")]
    public float runSpeed = 10f;

    float currentSpeed;

    Vector2 moveInput;
    Vector3 moveDirection;

    public bool IsMoving => moveInput.magnitude > 0.1f;
    public bool IsRunning { get; private set; }

    #endregion

    #region Gravedad

    [Header("Gravedad")]
    [Tooltip("Aceleración de gravedad aplicada al jugador")]
    public float gravity = -20f;

    private float verticalVelocity = 0f;

    #endregion

    #region Ground Check

    [Header("Ground Check")]
    [Tooltip("Altura del jugador usada para el raycast de detección de suelo")]
    public float playerHeight;

    [Tooltip("Capas consideradas como suelo")]
    public LayerMask whatIsGround;

    bool grounded;

    #endregion

    #region Stamina

    [Header("Stamina")]
    [Tooltip("Cantidad máxima de stamina disponible")]
    public float maxStamina = 4f;

    [Tooltip("Tiempo necesario para recuperar la stamina tras agotarla por completo")]
    public float staminaRecoveryTime = 5f;

    private float currentStamina;
    private float recoveryTimer = 0f;
    private bool isRecovering = false;
    private bool staminaDepleted = false;
    private float staminaCooldownOffset = 0f;
    private bool wasRunningLastFrame = false;

    public float StaminaNormalized => currentStamina / maxStamina;
    public bool IsRecovering => isRecovering;

    #endregion

    #region Agachado

    [Header("Agachado")]
    [Tooltip("Altura del CharacterController al estar agachado")]
    public float crouchHeight = 1f;

    [Tooltip("Velocidad de movimiento al caminar agachado")]
    public float crouchWalkSpeed = 2f;

    [Tooltip("Velocidad de interpolación del collider al agacharse o levantarse")]
    public float colliderLerpSpeed = 4f;

    [Tooltip("Indica si el jugador está agachado")]
    public bool isCrouching = false;

    [Tooltip("Capas consideradas como techo para el chequeo al levantarse")]
    public LayerMask ceilingMask;

    [Tooltip("Distancia extra sobre la cabeza para el raycast de techo")]
    public float ceilingCheckDistance = 0.5f;

    private float normalHeight;
    private Vector3 normalCenter;

    #endregion

    #region Estados Externos

    private bool isStunnedByEnemy = false;

    Transform currentSnapPoint;
    float snapSpeed;
    bool inObstacle;
    public bool IsLocked { get; private set; }

    private bool onStairs;

    #endregion

    #region Efecto Cansancio - Pixelado

    [Header("Efecto cansancio (pixelado)")]
    [Tooltip("Render feature que aplica el efecto de pixelado")]
    [SerializeField] private ScriptableRendererFeature fatigueRenderFeature;

    [Tooltip("Material que controla el shader de pixelado")]
    [SerializeField] private Material materialFatigue;

    [Tooltip("Tamaño de pixel en estado normal")]
    [SerializeField] private float normalPixelSize = 900f;

    [Tooltip("Tamaño de pixel al llegar al cansancio")]
    [SerializeField] private float cansancioPixelSize = 100f;

    [Tooltip("Valor medio al que subirá el pulso antes de bajar de nuevo")]
    [SerializeField] private float pulseMaxPixelSize = 400f;

    [Tooltip("Velocidad ultra rápida para la caída inicial al llegar a 0 stamina")]
    [SerializeField] private float velocidadImpactoInicial = 4000f;

    [Tooltip("Velocidad de transición para los pulsos del efecto de pixelado")]
    [SerializeField] private float velocidadTransicionFatigue = 800f;

    [Tooltip("Cantidad de pulsos que realiza el efecto antes de recuperarse")]
    [SerializeField] private int cantidadDePulsos = 3;

    private int idPropiedadPixel;
    private float pixelSizeObjetivo = 900f;
    private float pixelSizeActual = 900f;
    private bool fatigueFeatureActiva = false;

    private enum EstadoFatiga { Inactivo, ImpactoInicial, PulsoSubiendo, PulsoBajando, RecuperacionFinal }
    private EstadoFatiga estadoActualFatigue = EstadoFatiga.Inactivo;
    private int pulsosRestantes = 0;

    #endregion

    #region Efecto Cansancio - Viñeta

    [Header("Efecto cansancio (Vignette)")]
    [Tooltip("Render feature que aplica el efecto de viñeta")]
    [SerializeField] private ScriptableRendererFeature vignetteRenderFeature;

    [Tooltip("Material que controla el shader de viñeta")]
    [SerializeField] private Material materialVignette;

    [Tooltip("Intensidad de viñeta en estado normal")]
    [SerializeField] private float normalVignette = 0f;

    [Tooltip("Intensidad de viñeta al llegar al cansancio")]
    [SerializeField] private float cansancioVignette = 6f;

    [Tooltip("Valor al que baja la viñeta durante las pulsaciones medias")]
    [SerializeField] private float pulseMinVignette = 2f;

    [Tooltip("Velocidad inicial para que la viñeta llegue rápido a su valor de cansancio")]
    [SerializeField] private float velocidadVignetteInicial = 30f;

    [Tooltip("Velocidad de transición para los pulsos de la viñeta")]
    [SerializeField] private float velocidadTransicionVignette = 10f;

    private int idPropiedadVignette;
    private float vignetteObjetivo = 0f;
    private float vignetteActual = 0f;
    private bool vignetteFeatureActiva = false;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        cc = GetComponent<CharacterController>();
        Playeranimator = GetComponentInChildren<Animator>();
        Playeranimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        Playeranimator.applyRootMotion = false;

        normalHeight = cc.height;
        normalCenter = cc.center;

        currentStamina = maxStamina;
        currentSpeed = walkSpeed;

        idPropiedadPixel = Shader.PropertyToID("_PixelSize");
        if (materialFatigue != null)
        {
            pixelSizeActual = normalPixelSize;
            materialFatigue.SetFloat(idPropiedadPixel, pixelSizeActual);
        }

        idPropiedadVignette = Shader.PropertyToID("_VignetteIntensity");
        if (materialVignette != null)
        {
            vignetteActual = normalVignette;
            materialVignette.SetFloat(idPropiedadVignette, vignetteActual);
        }

        if (fatigueRenderFeature != null)
        {
            fatigueRenderFeature.SetActive(false);
            fatigueFeatureActiva = false;
        }
    }

    private void Update()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        ReadInput();
        RunPlayer();
        UpdateAnimations();
        HandleGravity();

        if (!inObstacle)
            MovePlayer();
        else
            HandleObstacleMovement();

        ActualizarTransicionFatigue();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Stairs"))
        {
            onStairs = true;
            currentSpeed = walkSpeed;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Stairs"))
            onStairs = false;
    }

    private void OnDisable()
    {
        if (materialFatigue != null)
            materialFatigue.SetFloat(idPropiedadPixel, normalPixelSize);

        if (materialVignette != null)
            materialVignette.SetFloat(idPropiedadVignette, normalVignette);
    }

    #endregion

    #region Input

    private void ReadInput()
    {
        if (Keyboard.current == null) return;

        if (IsLocked || isStunnedByEnemy)
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

        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
        {
            if (!isCrouching)
            {
                StartCrouch();
                Playeranimator?.SetTrigger("doCrouch");
            }
            else
            {
                TryStandUp();
            }
        }

        if (inObstacle && currentSnapPoint != null &&
            Vector3.Distance(transform.position, currentSnapPoint.position) > 1.2f)
        {
            ExitObstacleMode();
        }
    }

    #endregion

    #region Movimiento

    private void HandleGravity()
    {
        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        if (!IsMoving)
        {
            Debug.Log($"PlayerMovement: grounded={grounded} verticalVelocity={verticalVelocity:F4} position={transform.position}");
        }
    }

    private void MovePlayer()
    {
        if (IsLocked) return;

        if (isStunnedByEnemy)
        {
            cc.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
            return;
        }

        moveDirection = orientation.forward * moveInput.y + orientation.right * moveInput.x;

        Vector3 flatMove = moveDirection;
        flatMove.y = 0f;

        if (flatMove.magnitude < 0.1f)
        {
            cc.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
            return;
        }

        Vector3 motion;

        if (onStairs)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit stairsHit,
                playerHeight * 0.6f, whatIsGround))
            {
                Vector3 slopeDir = Vector3.ProjectOnPlane(flatMove, stairsHit.normal).normalized;

                float stairSpeed = walkSpeed;
                if (slopeDir.y > 0.01f)
                    stairSpeed *= 2f;

                motion = slopeDir * stairSpeed;
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

    #endregion

    #region Stamina y Carrera

    private void RunPlayer()
    {
        bool isMoving = moveInput.magnitude > 0.1f;
        bool shiftHeld = Keyboard.current.leftShiftKey.isPressed;

        if (shiftHeld && isRecovering)
        {
            isRecovering = false;
            recoveryTimer = 0f;
        }

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
        IsRunning = isRunning;

        if (isRunning)
        {
            if (!wasRunningLastFrame)
            {
                float oldMax = HabilidadesManager.instance.cooldown;
                float oldTimer = HabilidadesManager.instance.cooldownTimer;
                float startFraction = oldMax > 0f ? (oldTimer / oldMax) : 0f;

                staminaCooldownOffset = startFraction * maxStamina;
            }

            currentStamina -= Time.unscaledDeltaTime;

            float staminaDamage = maxStamina - currentStamina;
            float adjustedTimer = Mathf.Min(maxStamina, staminaCooldownOffset + staminaDamage);

            HabilidadesManager.instance.cooldown = maxStamina;
            HabilidadesManager.instance.cooldownTimer = adjustedTimer;

            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                staminaDepleted = true;
                isRecovering = false;
                recoveryTimer = 0f;
                staminaCooldownOffset = 0f;

                HabilidadesManager.instance.cooldown = staminaRecoveryTime;
                HabilidadesManager.instance.cooldownTimer = staminaRecoveryTime;

                DispararEfectoFatiga();
            }
        }
        else if (!shiftHeld && !staminaDepleted && currentStamina < maxStamina)
        {
            currentStamina += Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }

        currentSpeed = isCrouching ? crouchWalkSpeed : (isRunning ? runSpeed : walkSpeed);

        wasRunningLastFrame = isRunning;

        ReportMovementNoise(isMoving, isRunning);
    }

    #endregion

    #region Agachado

    void StartCrouch()
    {
        isCrouching = true;

        float centerY = normalCenter.y - (normalHeight - crouchHeight) / 2f;

        cc.height = crouchHeight;
        cc.center = new Vector3(normalCenter.x, centerY, normalCenter.z);
    }

    void TryStandUp()
    {
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

    #endregion

    #region Animación

    private void UpdateAnimations()
    {
        if (Playeranimator == null) return;

        bool isMoving = moveInput.magnitude > 0.1f;
        bool shiftHeld = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        bool isRunning = !onStairs && !isCrouching && shiftHeld && isMoving && !staminaDepleted && currentStamina > 0f;
        bool isUsingAbility = HabilidadesManager.instance != null &&
                              HabilidadesManager.instance.IsUsingAbility;

        Playeranimator.SetBool("isMoving", isMoving);
        Playeranimator.SetBool("isRunning", isRunning);
        Playeranimator.SetBool("isCrouching", isCrouching);
        Playeranimator.SetBool("isUsingAbility", isUsingAbility);
        Playeranimator.SetBool("isSlowActive", SlowTime.IsSlowActive);
    }

    #endregion

    #region Obstáculos

    void HandleObstacleMovement()
    {
        if (currentSnapPoint == null) return;

        Vector3 toPlayer = transform.position - currentSnapPoint.position;
        Vector3 wallNormal = currentSnapPoint.right;
        Vector3 parallel = Vector3.ProjectOnPlane(toPlayer, wallNormal);
        Vector3 move = currentSnapPoint.forward * moveInput.y;

        Vector3 targetPos = currentSnapPoint.position + parallel + move;
        Vector3 delta = targetPos - transform.position;

        Vector3 slideMotion = new Vector3(delta.x, verticalVelocity * Time.deltaTime, delta.z);
        cc.Move(slideMotion * snapSpeed * Time.unscaledDeltaTime);
    }

    public void EnterHideMode(Transform snapPoint, float snapSpeed)
    {
        inObstacle = true;
        IsLocked = false;
        currentSnapPoint = snapPoint;
        this.snapSpeed = snapSpeed;
        currentSpeed = crouchWalkSpeed;
        verticalVelocity = 0f;
    }

    public void ExitHideMode()
    {
        inObstacle = false;
        IsLocked = false;
        currentSnapPoint = null;
        currentSpeed = walkSpeed;
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

    #endregion

    #region API Pública

    public void SetMovementLock(bool lockState)
    {
        isStunnedByEnemy = lockState;
        if (lockState)
            moveInput = Vector2.zero;
    }

    #endregion

    #region Efecto Cansancio

    private void DispararEfectoFatiga()
    {
        if (estadoActualFatigue != EstadoFatiga.Inactivo) return;

        pulsosRestantes = cantidadDePulsos;
        estadoActualFatigue = EstadoFatiga.ImpactoInicial;

        pixelSizeObjetivo = cansancioPixelSize;
        vignetteObjetivo = cansancioVignette;

        if (fatigueRenderFeature != null && !fatigueFeatureActiva)
        {
            fatigueRenderFeature.SetActive(true);
            fatigueFeatureActiva = true;
        }

        if (vignetteRenderFeature != null && !vignetteFeatureActiva)
        {
            vignetteRenderFeature.SetActive(true);
            vignetteFeatureActiva = true;
        }
    }

    private void ActualizarTransicionFatigue()
    {
        if (estadoActualFatigue == EstadoFatiga.Inactivo) return;

        float velPixel = (estadoActualFatigue == EstadoFatiga.ImpactoInicial) ? velocidadImpactoInicial : velocidadTransicionFatigue;
        float velVignette = (estadoActualFatigue == EstadoFatiga.ImpactoInicial) ? velocidadVignetteInicial : velocidadTransicionVignette;

        if (materialFatigue != null)
        {
            pixelSizeActual = Mathf.MoveTowards(pixelSizeActual, pixelSizeObjetivo, velPixel * Time.deltaTime);
            materialFatigue.SetFloat(idPropiedadPixel, pixelSizeActual);
        }

        if (materialVignette != null)
        {
            vignetteActual = Mathf.MoveTowards(vignetteActual, vignetteObjetivo, velVignette * Time.deltaTime);
            materialVignette.SetFloat(idPropiedadVignette, vignetteActual);
        }

        float valorControlActual = (materialFatigue != null) ? pixelSizeActual : vignetteActual;
        float valorControlObjetivo = (materialFatigue != null) ? pixelSizeObjetivo : vignetteObjetivo;

        if (Mathf.Approximately(valorControlActual, valorControlObjetivo))
        {
            switch (estadoActualFatigue)
            {
                case EstadoFatiga.ImpactoInicial:
                    estadoActualFatigue = EstadoFatiga.PulsoSubiendo;
                    pixelSizeObjetivo = pulseMaxPixelSize;
                    vignetteObjetivo = pulseMinVignette;
                    break;

                case EstadoFatiga.PulsoSubiendo:
                    estadoActualFatigue = EstadoFatiga.PulsoBajando;
                    pixelSizeObjetivo = cansancioPixelSize;
                    vignetteObjetivo = cansancioVignette;
                    break;

                case EstadoFatiga.PulsoBajando:
                    pulsosRestantes--;
                    if (pulsosRestantes > 0)
                    {
                        estadoActualFatigue = EstadoFatiga.PulsoSubiendo;
                        pixelSizeObjetivo = pulseMaxPixelSize;
                        vignetteObjetivo = pulseMinVignette;
                    }
                    else
                    {
                        estadoActualFatigue = EstadoFatiga.RecuperacionFinal;
                        pixelSizeObjetivo = normalPixelSize;
                        vignetteObjetivo = normalVignette;
                    }
                    break;

                case EstadoFatiga.RecuperacionFinal:
                    estadoActualFatigue = EstadoFatiga.Inactivo;

                    if (fatigueRenderFeature != null && fatigueFeatureActiva)
                    {
                        fatigueRenderFeature.SetActive(false);
                        fatigueFeatureActiva = false;
                    }

                    if (vignetteRenderFeature != null && vignetteFeatureActiva)
                    {
                        vignetteRenderFeature.SetActive(false);
                        vignetteFeatureActiva = false;
                    }
                    break;
            }
        }
    }

    #endregion

    #region Ruido

    private void ReportMovementNoise(bool isMoving, bool isRunning)
    {
        if (GameManager.Instance == null) return;
        if (isCrouching) return;

        if (isRunning)
            GameManager.Instance.ReportNoiseB(transform.position, 0.3f);
        else if (isMoving)
            GameManager.Instance.ReportNoiseA(transform.position, 0.3f);
    }

    #endregion
}