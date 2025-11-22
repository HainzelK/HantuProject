using System;
using System.Collections.Generic;   // ← untuk List<>
using UnityEngine;                   // ← untuk MonoBehaviour

public class VoiceManager : MonoBehaviour
{
    public static VoiceManager Instance;

    private readonly object lockObj = new object();
    private readonly List<float> audioBuffer = new List<float>();

    private void Awake()
    {
        Instance = this;
    }

    public void FeedAudio(float[] samples)
    {
        lock (lockObj)
        {
            audioBuffer.AddRange(samples);
        }
    }

    private void Update()
    {
        ProcessAudio();
    }

    private void ProcessAudio()
    {
        float[] chunk;

        lock (lockObj)
        {
            if (audioBuffer.Count < 1600)
                return;

            chunk = audioBuffer.GetRange(0, 1600).ToArray();
            audioBuffer.RemoveRange(0, 1600);
        }

        // Kirim ke VAD di script kamu
        // contoh:
        // SpeechSpellcaster.Instance.ProcessVADChunk(chunk);
    }
}
