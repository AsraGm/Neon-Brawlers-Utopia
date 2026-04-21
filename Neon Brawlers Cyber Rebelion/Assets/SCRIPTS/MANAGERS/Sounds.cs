using UnityEngine;

[System.Serializable]
public class Sounds 
{
    public string name;

    public AudioClip clip;

    public bool loop;

    [Range(0, 1)]
    public float volume;

    [HideInInspector]
    public AudioSource source;
}
