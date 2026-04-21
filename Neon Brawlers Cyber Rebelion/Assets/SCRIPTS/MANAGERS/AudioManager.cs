using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    public Sounds[] sounds;

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
        { //agrega el audiosource y le pasa los valores que nosotros pusimos en el arreglo
            s.source = gameObject.AddComponent<AudioSource>();

            s.source.clip = s.clip;
            s.source.loop = s.loop;
            s.source.volume = s.volume;
        }
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
            {
                s.source.Pause();
            }
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
