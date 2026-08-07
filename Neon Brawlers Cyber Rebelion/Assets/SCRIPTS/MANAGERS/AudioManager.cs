using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    public Sounds[] sounds;

    [Header("Música dinámica (nivel / persecución)")]
    [SerializeField] private string normalMusicName;
    [SerializeField] private string chaseMusicName;
    [SerializeField] private float musicFadeDuration = 1.5f;

    private int chasingCount = 0;
    private Coroutine musicFadeCoroutine;
    private Sounds normalMusic;
    private Sounds chaseMusic;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
        foreach (Sounds s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.loop = s.loop;
            s.source.volume = s.volume;
        }
    }

    private void Start()
    {
        normalMusic = GetSound(normalMusicName);
        chaseMusic = GetSound(chaseMusicName);

        if (normalMusic == null || chaseMusic == null) return;

        chaseMusic.source.volume = 0f; // arranca en silencio pero sincronizado
        normalMusic.source.Play();
        chaseMusic.source.Play();
    }

    private Sounds GetSound(string nombre)
    {
        foreach (Sounds s in sounds)
        {
            if (s.name == nombre) return s;
        }
        Debug.Log("La cancion " + nombre + " no se encontro");
        return null;
    }

    public void EnemyStartedChasing()
    {
        chasingCount++;
        if (chasingCount == 1)
            StartMusicFade(toChase: true);
    }

    public void EnemyStoppedChasing()
    {
        if (chasingCount == 0) return;
        chasingCount--;
        if (chasingCount == 0)
            StartMusicFade(toChase: false);
    }

    private void StartMusicFade(bool toChase)
    {
        if (normalMusic == null || chaseMusic == null) return;
        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(MusicFadeRoutine(toChase));
    }

    private IEnumerator MusicFadeRoutine(bool toChase)
    {
        float t = 0f;
        float startNormal = normalMusic.source.volume;
        float startChase = chaseMusic.source.volume;
        float targetNormal = toChase ? 0f : normalMusic.volume;
        float targetChase = toChase ? chaseMusic.volume : 0f;

        while (t < musicFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / musicFadeDuration;
            normalMusic.source.volume = Mathf.Lerp(startNormal, targetNormal, p);
            chaseMusic.source.volume = Mathf.Lerp(startChase, targetChase, p);
            yield return null;
        }

        normalMusic.source.volume = targetNormal;
        chaseMusic.source.volume = targetChase;
    }

    public void Play(string nombre)
    {
        foreach (Sounds s in sounds)
        {
            if (s.name == nombre)
            {
                s.source.Play();
                return;
            }
        }
        Debug.Log("La cancion " + nombre + " no se encontro");
    }

    public void Stop(string nombre)
    {
        foreach (Sounds s in sounds)
        {
            if (s.name == nombre)
            {
                s.source.Stop();
                return;
            }
        }
        Debug.Log("La cancion " + nombre + " no se encontro");
    }

    public void PauseAll()
    {
        foreach (Sounds s in sounds)
        {
            if (s.source.isPlaying)
                s.source.Pause();
        }
    }

    public void UnPauseAll()
    {
        foreach (Sounds s in sounds)
        {
            s.source.UnPause();
        }
    }

    public void StopAll()
    {
        foreach (Sounds s in sounds)
        {
            s.source.Stop();
        }
    }
}