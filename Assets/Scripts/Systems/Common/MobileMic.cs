using UnityEngine;

public class MobileMic : MonoBehaviour
{
    public static MobileMic Instance;

    public AudioClip clip;
    public bool micReady;
    public int sampleRate = 16000;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartMic();
    }

    public void StartMic()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone found!");
            return;
        }

        string dev = Microphone.devices[0];

        clip = Microphone.Start(dev, true, 1, sampleRate);

        if (clip == null)
        {
            Debug.LogError("Microphone failed to start");
            return;
        }

        micReady = true;

        Debug.Log($"Mic started. Requested {sampleRate}, Actual {clip.frequency}");
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (!micReady) return;

        VoiceManager.Instance.FeedAudio(data);
    }
}
