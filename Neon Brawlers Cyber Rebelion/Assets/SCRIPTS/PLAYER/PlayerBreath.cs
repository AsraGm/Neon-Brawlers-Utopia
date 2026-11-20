using UnityEngine;

public class PlayerBreath : MonoBehaviour
{
    [Header("Breath")]
    [SerializeField] private AudioClip breathClip;
    [Range(0f, 1f)]
    [SerializeField] private float breathVolume = 1f;

    [SerializeField] private AudioSource breathSource;

    private void Awake()
    {
        if (breathSource == null)
        {
            breathSource = gameObject.AddComponent<AudioSource>();
            breathSource.playOnAwake = false;
            breathSource.spatialBlend = 0f; // 2D, se escucha igual sin importar la posición
        }

        breathSource.clip = breathClip;
        breathSource.loop = true;
        breathSource.volume = breathVolume;
    }

    public void StartBreath()
    {
        if (breathSource == null || breathClip == null) return;
        if (breathSource.isPlaying) return;

        breathSource.Play();
    }

    public void StopBreath()
    {
        if (breathSource == null) return;

        breathSource.Stop();
    }
}