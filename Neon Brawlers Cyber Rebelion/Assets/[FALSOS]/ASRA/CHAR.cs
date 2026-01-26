using UnityEngine;

public class MovimientoRapido : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [Tooltip("miran si funciona")]
    [SerializeField] private float velocidad = 5f;
    [SerializeField] private float velocidadCarrera = 10f;
    [SerializeField] private float suavizado = 10f;

    [Header("Salto")]
    [SerializeField] private float fuerzaSalto = 5f;
    [SerializeField] private LayerMask capaSuelo;
    [SerializeField] private Transform checkSuelo;
    [SerializeField] private float radioCheckSuelo = 0.2f;

    [Header("Teclas (Old Input System)")]
    [SerializeField] private KeyCode teclaCorrer = KeyCode.LeftShift;
    [SerializeField] private KeyCode teclaSaltar = KeyCode.Space;

    private Rigidbody rb;
    private Vector3 movimiento;
    private bool enSuelo;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Si no hay Rigidbody, agregar uno
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Configurar Rigidbody
        rb.freezeRotation = true;
    }

    void Update()
    {
        // Obtener input manualmente con GetKey
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.W)) vertical = 1f;
        if (Input.GetKey(KeyCode.S)) vertical = -1f;
        if (Input.GetKey(KeyCode.D)) horizontal = 1f;
        if (Input.GetKey(KeyCode.A)) horizontal = -1f;

        // Calcular dirección del movimiento
        Vector3 direccion = new Vector3(horizontal, 0f, vertical).normalized;

        // Determinar velocidad (normal o carrera)
        float velocidadActual = Input.GetKey(teclaCorrer) ? velocidadCarrera : velocidad;

        // Aplicar movimiento suavizado
        movimiento = Vector3.Lerp(movimiento, direccion * velocidadActual, Time.deltaTime * suavizado);

        // Verificar si está en el suelo
        if (checkSuelo != null)
        {
            enSuelo = Physics.CheckSphere(checkSuelo.position, radioCheckSuelo, capaSuelo);
        }
        else
        {
            // Raycast simple si no hay checkSuelo
            enSuelo = Physics.Raycast(transform.position, Vector3.down, 1.1f);
        }

        // Saltar
        if (Input.GetKeyDown(teclaSaltar) && enSuelo)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * fuerzaSalto, ForceMode.Impulse);
        }
    }

    void FixedUpdate()
    {
        // Aplicar movimiento al Rigidbody
        Vector3 velocidadObjetivo = new Vector3(movimiento.x, rb.linearVelocity.y, movimiento.z);
        rb.linearVelocity = velocidadObjetivo;
    }

    // Visualizar el área de detección de suelo en el editor
    private void OnDrawGizmosSelected()
    {
        if (checkSuelo != null)
        {
            Gizmos.color = enSuelo ? Color.green : Color.red;
            Gizmos.DrawWireSphere(checkSuelo.position, radioCheckSuelo);
        }
    }
}