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
        Debug.Log($"ShowHideButton — hideButtonUI: {hideButtonUI?.name ?? "NULL"}, exitHideButtonUI: {exitHideButtonUI?.name ?? "NULL"}");
        hideButtonUI?.SetActive(true);
        exitHideButtonUI?.SetActive(false);
    }

    public void ShowExitHideButton()
    {
        Debug.Log($"ShowExitHideButton — hideButtonUI: {hideButtonUI?.name ?? "NULL"}, exitHideButtonUI: {exitHideButtonUI?.name ?? "NULL"}");
        hideButtonUI?.SetActive(false);
        exitHideButtonUI?.SetActive(true);
    }

    public void HideAllHideUI()
    {
        Debug.Log($"HideAllHideUI llamado");
        hideButtonUI?.SetActive(false);
        exitHideButtonUI?.SetActive(false);
    }
}