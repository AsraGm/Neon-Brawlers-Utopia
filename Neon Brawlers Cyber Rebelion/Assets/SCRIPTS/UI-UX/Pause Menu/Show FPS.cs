using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShowFPS : MonoBehaviour
{
    [Header("Visual Config")]
    [SerializeField] private TextMeshProUGUI fpsText;

    [Header("Options Config")]
    [SerializeField] private Toggle fpsButton;
    [SerializeField] private Toggle screenButton;
    [SerializeField] private Toggle vsyncButton;
    [SerializeField] private TMP_Dropdown resolutionsDropdown;
    Resolution[] resolutions;

    float deltaTime = 0;
    private bool fpsState;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
    }
    private void Start()
    {
        CheckResolution();
    }

    private void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1 / deltaTime;
        fpsText.text = Mathf.Ceil(fps).ToString() + " FPS";
    }

    public void FPSToggleChange()
    {
        fpsState = fpsButton.isOn;
        fpsText.gameObject.SetActive(fpsState);
    }

    public void ScreenToggleChange()
    {
        if (screenButton.isOn)
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
    }

    public void VSyncToggleChange()
    {
        if (vsyncButton.isOn)
        {
            QualitySettings.vSyncCount = 1;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }

    public void CheckResolution()
    {
        resolutions = Screen.resolutions;
        resolutionsDropdown.ClearOptions();

        List<string> options = new List<string>();
        List<Resolution> uniqueResolutions = new List<Resolution>();
        HashSet<string> seen = new HashSet<string>();
        int actualResolution = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string key = resolutions[i].width + "x" + resolutions[i].height;

            if (seen.Contains(key)) continue;
            seen.Add(key);

            uniqueResolutions.Add(resolutions[i]);

            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (Screen.fullScreen && resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                actualResolution = uniqueResolutions.Count - 1;
            }
        }
        resolutions = uniqueResolutions.ToArray();

        resolutionsDropdown.AddOptions(options);
        resolutionsDropdown.value = actualResolution;
        resolutionsDropdown.RefreshShownValue();
        resolutionsDropdown.value = PlayerPrefs.GetInt("resolution", 0);
    }

    public void ChangeResolution(int resolutionIndex)
    {
        PlayerPrefs.SetInt("resolution", resolutionsDropdown.value);

        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }
}
