using UnityEngine;

/// <summary>
/// Sistema de Daño activado por Trigger
/// Aplica daño al jugador cuando entra/permanece en la zona
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class DamageTrigger : MonoBehaviour
{
    [Header("Configuración del Daño")]
    [Tooltip("Tag del jugador (debe ser 'Player')")]
    [SerializeField] private string jugadorTag = "Player";

    [Tooltip("Tipo de daño que aplica esta zona")]
    [SerializeField] private TipoDanio tipoDanio = TipoDanio.InstantaneoAlEntrar;

    [Tooltip("Cantidad de daño que aplica")]
    [SerializeField] private float cantidadDanio = 10f;

    [Tooltip("Intervalo entre daños continuos (en segundos)")]
    [SerializeField] private float intervaloDanioContinuo = 1f;

    [Header("Comportamiento")]
    [Tooltip("Destruir el trigger después de aplicar daño una vez")]
    [SerializeField] private bool destruirDespuesDeUsar = false;

    [Tooltip("Mostrar mensajes de debug")]
    [SerializeField] private bool mostrarDebugInfo = true;

    [Header("Efectos Visuales/Audio")]
    [Tooltip("Partículas al aplicar daño")]
    [SerializeField] private ParticleSystem particulasDanio;
    
    [Tooltip("Audio al aplicar daño")]
    [SerializeField] private AudioSource audioDanio;
    
    [Tooltip("Material para visualizar la zona peligrosa")]
    [SerializeField] private Material materialZonaPeligro;

    // Enumeración de tipos de daño
    public enum TipoDanio
    {
        InstantaneoAlEntrar,    // Daño una sola vez al entrar
        ContinuoMientrasEsta,   // Daño constante mientras está dentro
        InstantaneoAlSalir      // Daño al salir de la zona
    }

    // Variables internas
    private BoxCollider triggerCollider;
    private PlayerHealth jugadorDentro = null;
    private float tiempoUltimoDanio = 0f;

    private void Awake()
    {
        // Configurar el collider como trigger
        triggerCollider = GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;

        // Aplicar material visual si existe
        AplicarMaterialPeligro();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verificar que sea el jugador usando el tag
        if (!other.CompareTag(jugadorTag))
            return;

        // Obtener el componente PlayerHealth
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogWarning("[DamageTrigger] El jugador no tiene componente PlayerHealth");
            return;
        }

        // Guardar referencia para daño continuo
        jugadorDentro = playerHealth;

        // Aplicar daño según el tipo
        switch (tipoDanio)
        {
            case TipoDanio.InstantaneoAlEntrar:
                AplicarDanio(playerHealth);
                
                // Destruir si está configurado
                if (destruirDespuesDeUsar)
                {
                    Destroy(gameObject, 0.1f);
                }
                break;

            case TipoDanio.ContinuoMientrasEsta:
                // El daño se aplica en Update()
                tiempoUltimoDanio = Time.time;
                break;

            case TipoDanio.InstantaneoAlSalir:
                // El daño se aplica en OnTriggerExit()
                break;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Solo procesar si es daño continuo
        if (tipoDanio != TipoDanio.ContinuoMientrasEsta)
            return;

        // Verificar que sea el jugador
        if (!other.CompareTag(jugadorTag))
            return;

        // Aplicar daño si ha pasado el intervalo
        if (Time.time >= tiempoUltimoDanio + intervaloDanioContinuo)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                AplicarDanio(playerHealth);
                tiempoUltimoDanio = Time.time;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Verificar que sea el jugador
        if (!other.CompareTag(jugadorTag))
            return;

        // Limpiar referencia
        jugadorDentro = null;

        // Aplicar daño si es del tipo "al salir"
        if (tipoDanio == TipoDanio.InstantaneoAlSalir)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                AplicarDanio(playerHealth);
            }
        }
    }

    /// Aplica el daño al jugador
    private void AplicarDanio(PlayerHealth playerHealth)
    {
        if (playerHealth == null)
            return;

        // Aplicar el daño
        playerHealth.RecibirDanio(cantidadDanio);

        // Reproducir efectos
        ReproducirEfectos();

        // Debug
        if (mostrarDebugInfo)
        {
            Debug.Log($"[DamageTrigger] '{gameObject.name}' aplicó {cantidadDanio} de daño");
        }
    }

    /// Reproduce efectos visuales y de audio
    private void ReproducirEfectos()
    {
        // Partículas
        if (particulasDanio != null)
        {
            particulasDanio.Play();
        }

        // Audio
        if (audioDanio != null)
        {
            audioDanio.Play();
        }
    }

    /// Aplica un material visual a la zona de daño
    private void AplicarMaterialPeligro()
    {
        if (materialZonaPeligro == null)
            return;

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = materialZonaPeligro;
        }
    }

    /// Cambiar la cantidad de daño en tiempo de ejecución
    public void EstablecerDanio(float nuevoDanio)
    {
        cantidadDanio = nuevoDanio;
    }

    /// <summary>
    /// Activar/desactivar la zona de daño
    /// </summary>
    public void ActivarZona(bool activar)
    {
        enabled = activar;
        triggerCollider.enabled = activar;
    }

    // VISUALIZACIÓN EN EDITOR (GIZMOS)
    private void OnDrawGizmos()
    {
        // Color según el tipo de daño
        Color colorGizmo = Color.red;
        switch (tipoDanio)
        {
            case TipoDanio.InstantaneoAlEntrar:
                colorGizmo = Color.red;
                break;
            case TipoDanio.ContinuoMientrasEsta:
                colorGizmo = new Color(1f, 0.5f, 0f); // Naranja
                break;
            case TipoDanio.InstantaneoAlSalir:
                colorGizmo = Color.yellow;
                break;
        }

        // Área del trigger (transparente)
        Gizmos.color = new Color(colorGizmo.r, colorGizmo.g, colorGizmo.b, 0.3f);
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            
            // Borde del trigger
            Gizmos.color = colorGizmo;
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Mostrar información detallada cuando está seleccionado
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.color = Color.red;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }

        // Etiqueta con información
        #if UNITY_EDITOR
        string info = $"ZONA DE DAÑO\n" +
                      $"{gameObject.name}\n" +
                      $"Daño: {cantidadDanio}\n" +
                      $"Tipo: {tipoDanio}";
        
        if (tipoDanio == TipoDanio.ContinuoMientrasEsta)
        {
            info += $"\nIntervalo: {intervaloDanioContinuo}s";
        }

        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, info);
        #endif
    }
}
