using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("---------Audio Source---------")]
    [SerializeField] AudioSource BGMSource;
    [Header("---------Audio Clip---------")]
    public AudioClip background;

    public void Start()
    {
        BGMSource.clip = background;
        BGMSource.Play();
    }

}
