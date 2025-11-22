using UnityEngine;
using System.Linq;
using Eitan.SherpaOnnxUnity.Runtime;
using Eitan.SherpaOnnxUnity.Samples;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

[System.Serializable]
public class Spell
{
    public string spellName;
    public List<string> variations;
}

public class SpeechSpellcaster : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private Spell[] spells;

    [Header("Model Config")]
    [SerializeField] private string koreanAsrModelID = "sherpa-onnx-zipformer-korean-2024-06-24";
    [SerializeField] private string vadModelID = "silero_vad_v5";

    private SpeechRecognition speechRecognizer;
    private VoiceActivityDetection vad;
    private ProjectileShooter projectileShooter;

    private AudioClip micClip;
    private const int SAMPLE_RATE = 16000;

    private bool isTranscribing = false;
    private Spell _spellToCast = null;
    private bool _isSpellActionPending = false;

    void Start()
    {
        projectileShooter = GetComponent<ProjectileShooter>();
        if (projectileShooter == null)
            Debug.LogError("ProjectileShooter tidak ditemukan! Tambahkan script ProjectileShooter ke kamera!");

        StartCoroutine(Init());
    }

    System.Collections.IEnumerator Init()
    {
        // Request microphone permission (Android)
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        }

        speechRecognizer = new SpeechRecognition(koreanAsrModelID, SAMPLE_RATE);
        vad = new VoiceActivityDetection(vadModelID, SAMPLE_RATE);

        vad.OnSpeechSegmentDetected += HandleSpeechDetected;

        StartMic();

        Debug.Log("System Ready.");
    }

    void Update()
    {
        if (_isSpellActionPending)
        {
            OnSpellAction(_spellToCast);
            _spellToCast = null;
            _isSpellActionPending = false;
        }
    }

    // ===============================================
    //   Microphone Recorder (Universal)
    // ===============================================
    void StartMic()
    {
        if (micClip != null)
            Microphone.End(null);

        micClip = Microphone.Start(null, true, 1, SAMPLE_RATE);

        Debug.Log("[Mic] Start at 16kHz");
        InvokeRepeating(nameof(PullMicFrames), 0.1f, 0.1f);
    }

    void PullMicFrames()
    {
        if (micClip == null) return;

        int pos = Microphone.GetPosition(null);
        if (pos < SAMPLE_RATE / 10) return; // not enough samples

        float[] buffer = new float[SAMPLE_RATE / 10];
        micClip.GetData(buffer, pos - buffer.Length);

        vad.StreamDetect(buffer);
    }

    // ===============================================
    //   VAD → ASR
    // ===============================================
    private void HandleSpeechDetected(float[] segment)
    {
        if (isTranscribing) return;
        if (segment == null || segment.Length < SAMPLE_RATE * 0.2f) return;

        Debug.Log($"[VAD] speech ({segment.Length} samples)");
        _ = Transcribe(segment);
    }

    private async Task Transcribe(float[] pcm)
    {
        isTranscribing = true;
        try
        {
            string txt = await speechRecognizer.SpeechTranscriptionAsync(pcm, SAMPLE_RATE);
            Debug.Log("[ASR] => " + txt);
            if (!string.IsNullOrWhiteSpace(txt))
                FindBestMatch(txt);
        }
        finally
        {
            isTranscribing = false;
        }
    }

    // ===============================================
    //   SPELL MATCHING
    // ===============================================
    private void FindBestMatch(string text)
    {
        string[] words = text.Trim().Split(' ');

        Spell bestSpell = null;
        float bestScore = 0;

        foreach (Spell s in spells)
        {
            foreach (string v in s.variations)
            {
                foreach (string w in words)
                {
                    float sc = HangulSimilarity(w, v);
                    if (sc > bestScore)
                    {
                        bestScore = sc;
                        bestSpell = s;
                    }
                }
            }
        }

        if (bestSpell != null && bestScore >= 0.55f)
        {
            Debug.Log($"[SPELL] Match: {bestSpell.spellName}");
            _spellToCast = bestSpell;
            _isSpellActionPending = true;
        }
        else
        {
            Debug.Log($"[Spell] No match (best={bestScore:0.00})");
        }
    }

    // ===============================================
    //   DO SPELL
    // ===============================================
    void OnSpellAction(Spell s)
    {
        projectileShooter.TryShoot(s.spellName);
    }

    // ===============================================
    //   HANGUL MATCH CORE
    // ===============================================
    public static float HangulSimilarity(string a, string b)
    {
        var A = HangulJamo.Decompose(a);
        var B = HangulJamo.Decompose(b);

        int len = Mathf.Min(A.Count, B.Count);
        if (len == 0) return 0f;

        float t = 0f;
        for (int i = 0; i < len; i++)
        {
            if (A[i].initial == B[i].initial) t += 0.5f;
            if (A[i].vowel == B[i].vowel) t += 0.3f;
            if (A[i].finalJ == B[i].finalJ) t += 0.2f;
        }

        return t / len;
    }

    void OnDestroy()
    {
        Microphone.End(null);
        vad?.Dispose();
        speechRecognizer?.Dispose();
    }
}