using UnityEngine;

public class HudManager : MonoBehaviour
{
    public static HudManager instance { get; private set; }

    [Header("Escondite")]
    public GameObject hideButtonUI;
    public GameObject exitHideButtonUI;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        hideButtonUI?.SetActive(false);
        exitHideButtonUI?.SetActive(false);
    }

    public void ShowHideButton()
    {
        hideButtonUI?.SetActive(true);
        exitHideButtonUI?.SetActive(false);
    }

    public void ShowExitHideButton()
    {
        hideButtonUI?.SetActive(false);
        exitHideButtonUI?.SetActive(true);
    }

    public void HideAllHideUI()
    {
        hideButtonUI?.SetActive(false);
        exitHideButtonUI?.SetActive(false);
    }
}