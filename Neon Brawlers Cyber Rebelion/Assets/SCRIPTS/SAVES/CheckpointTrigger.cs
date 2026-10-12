using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CheckpointTrigger : MonoBehaviour
{
    [SerializeField] private string jugadorTag = "Player";
    [SerializeField] private bool soloUnaVez = true;
    [SerializeField] private bool mostrarDebug = true;

    private bool usado = false;

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(jugadorTag)) return;
        if (soloUnaVez && usado) return;
        if (GameManager.Instance == null) return;

        usado = true;
        GameManager.Instance.GuardarCheckpoint();
        PuzzleCheckpointGroup.GuardarTodos();

        if (mostrarDebug)
            Debug.Log($"[CheckpointTrigger] '{name}' guardó checkpoint");
    }

    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.25f);
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}