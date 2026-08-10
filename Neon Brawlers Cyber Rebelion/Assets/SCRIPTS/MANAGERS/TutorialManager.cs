using UnityEngine;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI tutorialText;
    [SerializeField] private Animator animator;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    private void Start()
    {
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    public void ShowMessage(string mensaje)
    {
        tutorialText.text = mensaje;
        animator.SetTrigger("trigger");
    }
}