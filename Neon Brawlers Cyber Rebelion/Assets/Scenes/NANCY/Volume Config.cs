using UnityEngine;
using UnityEngine.UI;

public class VolumeConfig : MonoBehaviour
{
    [SerializeField] private Slider slider;
    private float sliderValue;

    private void Start()
    {
        slider.value = PlayerPrefs.GetFloat("audioVolume", 0.5f);
        AudioListener.volume = slider.value;
    }

    public void AudioChange(float volume)
    {
        sliderValue = volume;
        PlayerPrefs.SetFloat("audioVolume", sliderValue);
        AudioListener.volume = slider.value;
    }
}
