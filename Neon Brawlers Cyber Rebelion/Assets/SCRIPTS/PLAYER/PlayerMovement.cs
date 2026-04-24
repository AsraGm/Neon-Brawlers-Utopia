using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float walkSpeed = 6f;
    public float runSpeed = 10f;
    float currentSpeed;

    public float groundDrag;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    public Transform orientation;

    Vector2 moveInput;
    Vector3 moveDirection;

    Rigidbody rb;

    // variables para la interaccion con obstaculos
    Transform currentSnapPoint;
    float snapSpeed;
    bool inObstacle;

    // para las escaleras
    private bool onStairs;
    private RaycastHit stairsHit;

    // referencia al efecto de velocidad
    public UniversalRendererData rendererData;
    ScriptableRendererFeature fullScreenFeature;

    Animator Playeranimator;


    // referencia al script de la cámara
    public ThirdPersonCam camScript;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        Playeranimator = GetComponentInChildren<Animator>();

        // establecemos que la velocidad actual es la walk desde el inicio
        currentSpeed = walkSpeed;

        //foreach para el efecto de velocidad
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
        // checar el suelo lanzando raycast
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        ReadInput();

        RunPlayer();

        UpdateAnimations();

        if (!inObstacle)
        {
            //SpeedControl();
            rb.linearDamping = grounded ? groundDrag : 0f;
        }
        else
        {
            rb.linearDamping = 0f;
        }

    }

    private void FixedUpdate()
    {
        if (inObstacle)
            HandleObstacleMovement();
        else
            MovePlayer();
    }


    private void ReadInput()
    {
        if (Keyboard.current == null) return;

        float horizontal =
            (Keyboard.current.dKey.isPressed ? 1 : 0) -
            (Keyboard.current.aKey.isPressed ? 1 : 0);

        float vertical =
            (Keyboard.current.wKey.isPressed ? 1 : 0) -
            (Keyboard.current.sKey.isPressed ? 1 : 0);

        moveInput = new Vector2(horizontal, vertical);

        // pero si esta en un asset
        if (inObstacle && Vector3.Distance(rb.position, currentSnapPoint.position) > 1.2f)
        {
            ExitObstacleMode();
        }


    }

    private void MovePlayer()
    {
        moveDirection = orientation.forward * moveInput.y + orientation.right * moveInput.x;

        if (moveDirection.magnitude < 0.1f)
            return;

        if (onStairs)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out stairsHit, playerHeight * 0.6f, whatIsGround))
            {
                Vector3 slopeDir = Vector3.ProjectOnPlane(moveDirection, stairsHit.normal).normalized;

                float stairSpeed = walkSpeed;

                // Si va cuesta arriba, aplicar boost
                if (slopeDir.y > 0.01f)
                {
                    stairSpeed *= 2f; // se puede ajustar la velocidad al subir escaleras 
                }

                Vector3 targetVelocity = slopeDir * stairSpeed;

                //mantener la Y actual excepto si es positiva
                float yVel = rb.linearVelocity.y;

                if (yVel > 0f)
                    yVel = 0f;

                rb.linearVelocity = new Vector3(
                    targetVelocity.x,
                    yVel,
                    targetVelocity.z
                );
            }
        }
        else
        {
            // Movimiento normal fuera de escaleras
            Vector3 targetVelocity = moveDirection.normalized * currentSpeed;

            rb.linearVelocity = new Vector3(
                targetVelocity.x,
                rb.linearVelocity.y,
                targetVelocity.z
            );
        }

        if (moveDirection.magnitude > 0.1f)
        {
            Vector3 flatDirection = new Vector3(moveDirection.x, 0f, moveDirection.z);

            Quaternion targetRotation = Quaternion.LookRotation(flatDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * 10f
            );
        }
    }

    private void RunPlayer()
    {
        bool isMoving = moveInput.magnitude > 0.1f;

        // Si está en escaleras, no puede correr
        bool isRunning = !onStairs &&
                         Keyboard.current.leftShiftKey.isPressed &&
                         isMoving;

        currentSpeed = isRunning ? runSpeed : walkSpeed;

        // avisamos a la camara
        camScript.SetRunning(isRunning);

        // Avisamos para prender el efecto de velocidad
        SetEffect(isRunning);
    }
    void SetEffect(bool active)
    {
        if (fullScreenFeature != null)
        {
            fullScreenFeature.SetActive(active);
        }
    }

    private void UpdateAnimations()
    {
        if (Playeranimator == null) return;

        bool isMoving = moveInput.magnitude > 0.1f;
        bool isUsingAbility = HabilidadesManager.instance != null &&
                              HabilidadesManager.instance.IsUsingAbility;

        Playeranimator.SetBool("isMoving", isMoving);

        // animacion especial de poder opcional
        Playeranimator.SetBool("isUsingAbility", isUsingAbility);
    }

    void HandleObstacleMovement()
    {
        if (currentSnapPoint == null) return;

        // Vector desde el snapPoint al player
        Vector3 toPlayer = rb.position - currentSnapPoint.position;

        // Normal de la pared (hacia afuera)
        Vector3 wallNormal = currentSnapPoint.right;

        // Componente paralela a la pared (eje Z)
        Vector3 parallel = Vector3.ProjectOnPlane(toPlayer, wallNormal);

        // Movimiento input solo sobre el eje paralelo
        Vector3 move = currentSnapPoint.forward * moveInput.y;

        Vector3 targetPos = currentSnapPoint.position + parallel + move;

        rb.MovePosition(Vector3.Lerp(
            rb.position,
            targetPos,
            Time.unscaledDeltaTime * snapSpeed
        ));
    }


    //private void SpeedControl()
    //{
    //    Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

    //    if (flatVel.magnitude > currentSpeed)
    //    {
    //        Vector3 limitedVel = flatVel.normalized * currentSpeed;
    //        rb.linearVelocity =
    //            new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
    //    }
    //}

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Stairs"))
        {
            onStairs = true;

            // Forzar volver a caminar
            currentSpeed = walkSpeed;
            camScript.SetRunning(false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Stairs"))
        {
            onStairs = false;
        }
    }

    public void EnterObstacleMode(Transform snapPoint, float snapSpeed)
    {
        inObstacle = true;
        currentSnapPoint = snapPoint;
        this.snapSpeed = snapSpeed;

        currentSpeed = walkSpeed;
        rb.linearVelocity = Vector3.zero;
    }

    public void ExitObstacleMode()
    {
        inObstacle = false;
        currentSnapPoint = null;
    }

}
