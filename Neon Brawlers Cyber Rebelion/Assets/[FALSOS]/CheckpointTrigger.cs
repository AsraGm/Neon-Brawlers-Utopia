using UnityEngine;

/// <summary>
/// Sistema de Checkpoint activado por Trigger
/// Guarda automáticamente cuando el jugador entra en la zona
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class CheckpointTrigger : MonoBehaviour
{
    [Header("Configuración del Checkpoint")]
    [Tooltip("Tag del jugador (debe ser 'Player')")]
    [SerializeField] private string jugadorTag = "Player";
    
    [Tooltip("Se puede activar varias veces o solo una vez")]
    [SerializeField] private bool activarSoloUnaVez = true;
    
    [Tooltip("Mostrar mensaje en consola al activar")]
    [SerializeField] private bool mostrarDebugInfo = true;

    [Header("Efectos Visuales")]
    [Tooltip("Partículas que se activan al guardar")]
    [SerializeField] private ParticleSystem particulasActivacion;
    
    [Tooltip("Audio que se reproduce al guardar")]
    [SerializeField] private AudioSource audioActivacion;

    [Header("Respawn Point")]
    [Tooltip("Punto específico de respawn (si está vacío, usa la posición del trigger)")]
    [SerializeField] private Transform puntoRespawn;

    // Estado interno
    private bool yaActivado = false;
    private BoxCollider triggerCollider;

    private void Awake()
    {
        // Configurar el collider como trigger
        triggerCollider = GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verificar que sea el jugador usando el tag
        if (!other.CompareTag(jugadorTag))
            return;

        // Si ya fue activado y solo se activa una vez, salir
        if (yaActivado && activarSoloUnaVez)
            return;

        // Activar checkpoint
        ActivarCheckpoint(other.transform);
    }

    /// Activa el checkpoint y guarda el juego
    private void ActivarCheckpoint(Transform jugador)
    {
        // Verificar que el GameManager exista
        if (GameManager.Instance == null)
        {
            Debug.LogError("[CheckpointTrigger] GameManager no encontrado en la escena");
            return;
        }

        // Si hay un punto de respawn específico, mover al jugador primero
        if (puntoRespawn != null)
        {
            jugador.position = puntoRespawn.position;
            jugador.rotation = puntoRespawn.rotation;
        }

        // Guardar el checkpoint
        GameManager.Instance.GuardarCheckpoint();

        // Marcar como activado
        yaActivado = true;

        // Efectos visuales
        ReproducirEfectos();

        // Debug
        if (mostrarDebugInfo)
        {
            Debug.Log($"[CheckpointTrigger] Checkpoint '{gameObject.name}' activado correctamente");
        }
    }

    /// Reproduce efectos visuales y de audio
    private void ReproducirEfectos()
    {
        // Partículas
        if (particulasActivacion != null)
        {
            particulasActivacion.Play();
        }

        // Audio
        if (audioActivacion != null)
        {
            audioActivacion.Play();
        }
    }

    /// Resetear el checkpoint para que pueda volver a activarse
    public void ResetearCheckpoint()
    {
        yaActivado = false;
        if (mostrarDebugInfo)
        {
            Debug.Log($"[CheckpointTrigger] Checkpoint '{gameObject.name}' reseteado");
        }
    }

    // VISUALIZACIÓN EN EDITOR (GIZMOS)
    private void OnDrawGizmos()
    {
        // Dibujar el área del trigger
        Gizmos.color = yaActivado ? Color.green : Color.yellow;
        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
        
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            
            // Borde del trigger
            Gizmos.color = yaActivado ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(box.center, box.size);
        }

        // Dibujar punto de respawn si existe
        if (puntoRespawn != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(puntoRespawn.position, 0.5f);
            Gizmos.DrawLine(puntoRespawn.position, puntoRespawn.position + puntoRespawn.forward * 1.5f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Mostrar el área de influencia cuando está seleccionado
        Gizmos.color = Color.yellow;
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }

        // Etiqueta del checkpoint
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
            $"CHECKPOINT\n{gameObject.name}\n{(yaActivado ? "✓ Activado" : "○ Inactivo")}");
        #endif
    }
}
