using System.Collections;
using UnityEngine;

public class Telekinesis : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask objectLayer;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float throwForce = 20f;
    [SerializeField] private float cooldownTime = 2f;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float maxHoldTime = 4f;

    [Header("Hold Point")]
    [SerializeField] private Transform holdPosition;
    [SerializeField] private float holdDistance = 3f;
    [SerializeField] private float holdHeight = 1.5f;

    [SerializeField] private Renderer _rend;

    [Header("Effects")]
    [SerializeField] ParticleSystem throwParticles;

    private Rigidbody currentRb;
    private EnemyPatrol currentEnemy;

    private MaterialPropertyBlock _mpb;

    private void Start()
    {
        _mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        UpdateHoldPosition();

        if (currentRb) MoveObject();

        // por si el objeto desaparece o lo que sea, se detiene el loop.
        if (!currentRb)
        {
            AudioManager.instance.Stop("telekinesis");
        }

        UpdateCooldownShader();
    }

    private void UpdateCooldownShader()
    {
        float progress = (HabilidadesManager.instance.cooldownTimer / HabilidadesManager.instance.cooldown) * 5f;
        _mpb.SetFloat("_Remove_Segments", progress);
        _rend.SetPropertyBlock(_mpb);
    }

    void UpdateHoldPosition()
    {
        if (holdPosition != null && playerCamera != null)
        {
            Vector3 targetDirection = playerCamera.transform.forward;

            targetDirection.y = 0;
            targetDirection.Normalize();

            Vector3 targetPosition = transform.position + targetDirection * holdDistance + Vector3.up * holdHeight;
            holdPosition.position = targetPosition;
        }
    }

    public void StartTelekinesis()
    {
        if (HabilidadesManager.instance.cooldownTimer <= 0 && !currentRb)
        {
            GrabNearestObject();
            StartCoroutine(ForceDronRelease());
        }
    }

    public void StopTelekinesis()
    {
        if (currentRb)
        {
            ThrowObject();
        }
    }

    void GrabNearestObject()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, objectLayer);
        if (hits.Length == 0) return;

        Collider nearest = hits[0];
        float minDist = Vector3.Distance(playerCamera.transform.position, nearest.transform.position); // Calcular la distancia al objeto más cercano

        for (int i = 1; i < hits.Length; i++)
        {
            float dist = Vector3.Distance(playerCamera.transform.position, hits[i].transform.position); // Calcular la distancia al objeto
            if (dist < minDist)
            {
                minDist = dist; // Actualizar la distancia mínima
                nearest = hits[i]; // Actualizar el objeto más cercano
            }
        }

        Debug.Log("Objetos detectados: " + hits.Length);
        currentRb = nearest.GetComponent<Rigidbody>(); // Obtener el Rigidbody del objeto más cercano
        currentEnemy = nearest.GetComponent<EnemyPatrol>();

        if (currentRb)
        {
            if (currentEnemy != null)
            {
                currentEnemy.OnTelekinesisGrab();
            }
            else
            {
                currentRb.useGravity = false;
            }

            currentRb.linearVelocity = Vector3.zero;
            currentRb.angularVelocity = Vector3.zero;

            AudioManager.instance.Play("telekinesis");
        }


        Debug.Log("Agarrando: " + nearest.name);
    }

    void MoveObject() // Mueve el objeto hacia la posición de agarre
    {
        Vector3 dir = holdPosition.position - currentRb.position;
        currentRb.linearVelocity = dir * moveSpeed;
    }

    void ThrowObject() // Lanza el objeto en la dirección de la cámara
    {
        throwParticles.Play();

        Vector3 releasePosition = currentRb.position;

        if (currentEnemy != null)
        {
            currentEnemy.OnTelekinesisRelease();
            currentRb.AddForce(playerCamera.transform.forward * throwForce * 10, ForceMode.Impulse);
            currentEnemy = null;

            AudioManager.instance.Play("throwDron");
        }
        else
        {
            currentRb.useGravity = true;
            AudioManager.instance.Play("throwObject");
        }

        GameManager.Instance.ReportNoiseA(releasePosition, 1.5f); 

        AudioManager.instance.Stop("telekinesis");

        currentRb = null;
        HabilidadesManager.instance.Cooldown(cooldownTime);
    }

    private IEnumerator ForceDronRelease()
    {
        yield return new WaitForSeconds(maxHoldTime);
        if (currentRb)
        {
            ThrowObject();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}