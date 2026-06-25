using UnityEngine;

public class ScreamerNPC : MonoBehaviour
{
    [Header("Cámaras")]
    public Camera screamerCamera;
    public Camera playerCamera;
    public GameObject playerMesh;      // <el GameObject con el mesh del jugador

    [Header("Animator")]
    public Animator npcAnimator;       // el animator del NPC

    [Header("Ajustes")]
    public string screamerTrigger = "doScreamer";  // nombre del trigger en el Animator

    bool screamerActive = false;
    bool playerInside = false;
    bool playerHasExited = false;   // obliga al jugador a salir antes de reactivar

    private void Awake()
    {
        // Aseguramos estado inicial
        screamerCamera.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Solo se activa si el jugador ya salió antes (o es la primera vez)
        if (screamerActive) return;
        if (playerInside) return;
        if (!playerHasExited && playerInside) return;

        playerInside = true;
        playerHasExited = false;

        TriggerScreamer();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        playerHasExited = true;   // ya salió, puede volver a activarse
    }

    void TriggerScreamer()
    {
        screamerActive = true;

        playerCamera.gameObject.SetActive(false);
        playerMesh?.SetActive(false);              
        screamerCamera.gameObject.SetActive(true);

        npcAnimator.SetTrigger(screamerTrigger);
        StartCoroutine(WaitForScreamerEnd());
    }

    System.Collections.IEnumerator WaitForScreamerEnd()
    {
        yield return null;
        yield return null; // dos frames para asegurar que el Animator actualizó

        // Esperar a que entre al estado del screamer
        float timeout = 10f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            AnimatorStateInfo stateInfo = npcAnimator.GetCurrentAnimatorStateInfo(0);

            // Cuando entre al estado screamer y esté por terminar
            if (stateInfo.IsName("Screamer") && stateInfo.normalizedTime >= 0.95f)
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        EndScreamer();
    }

    void EndScreamer()
    {
        screamerActive = false;

        screamerCamera.gameObject.SetActive(false);
        playerCamera.gameObject.SetActive(true);
        playerMesh?.SetActive(true);               
    }
}