using UnityEngine;
using UnityEngine.InputSystem;

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

    Animator anim;


    // referencia al script de la cámara
    public ThirdPersonCam camScript;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        anim = GetComponent<Animator>();

        // establecemos que la velocidad actual es la walk desde el inicio
        currentSpeed = walkSpeed;
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
            SpeedControl();
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


        rb.AddForce
        (
            moveDirection.normalized * currentSpeed * 10f,
            ForceMode.Force

        );
    }

    private void RunPlayer()
    {
        bool isMoving = moveInput.magnitude > 0.1f;
        bool isRunning = Keyboard.current.leftShiftKey.isPressed && isMoving;

        currentSpeed = isRunning ? runSpeed : walkSpeed;

        // avisamos a la cámara
        camScript.SetRunning(isRunning);
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;

        bool isMoving = moveInput.magnitude > 0.1f;
        bool isRunning = Keyboard.current.leftShiftKey.isPressed && isMoving;

        // Actualizar parámetros de animación usando hash para mejor rendimiento
        anim.SetBool("Walk", isMoving);
        anim.SetBool("Running", isRunning);
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


    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (flatVel.magnitude > currentSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * currentSpeed;
            rb.linearVelocity =
                new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
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
