using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip; // Voltou a ser um único clip para SFX e Ambiente
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

[System.Serializable]
public class Playlist
{
    public string name;
    public AudioClip[] clips; // Array exclusivo para as músicas (BGM)
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}