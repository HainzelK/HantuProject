// SpeechSpellcaster.cs
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
    [SerializeField] private string vadModelID = "ten-vad";

    private SpeechRecognition speechRecognizer;
    private VoiceActivityDetection vad;
    private ProjectileShooter projectileShooter;

    private SpellManager spellManager;

    private AudioClip micClip;
    private const int SAMPLE_RATE = 16000;

    private bool isTranscribing = false;
    private Spell _spellToCast = null;
    private bool _isSpellActionPending = false;
    private bool _isListening = false;
    private string _pendingSpellName = null;

    void Start()
    {
        spellManager = FindObjectOfType<SpellManager>();
        if (spellManager == null) Debug.LogWarning("SpellManager not found!");

        projectileShooter = GetComponent<ProjectileShooter>();
        if (projectileShooter == null)
            Debug.LogError("ProjectileShooter tidak ditemukan!");

        StartCoroutine(Init());
    }

    System.Collections.IEnumerator Init()
    {
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        }

        speechRecognizer = new SpeechRecognition(koreanAsrModelID, SAMPLE_RATE);
        vad = new VoiceActivityDetection(vadModelID, SAMPLE_RATE);
        vad.OnSpeechSegmentDetected += HandleSpeechDetected;

        Debug.Log("Speech system initialized.");
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

    void StartListening()
    {
        if (_isListening) return;

        if (micClip != null) Microphone.End(null);
        micClip = Microphone.Start(null, true, 1, SAMPLE_RATE);
        _isListening = true;

        Debug.Log("[Mic] Mulai mendengarkan (16kHz)");
        InvokeRepeating(nameof(PullMicFrames), 0.1f, 0.1f);
    }

    void StopListening()
    {
        if (!_isListening) return;
        CancelInvoke(nameof(PullMicFrames));
        if (micClip != null) { Microphone.End(null); micClip = null; }
        _isListening = false;
        Debug.Log("[Mic] Berhenti mendengarkan.");
    }

    void PullMicFrames()
    {
        if (micClip == null) return;
        int pos = Microphone.GetPosition(null);
        if (pos < SAMPLE_RATE / 10) return;
        float[] buffer = new float[SAMPLE_RATE / 10];
        micClip.GetData(buffer, pos - buffer.Length);
        vad.StreamDetect(buffer);
    }

    private void HandleSpeechDetected(float[] segment)
    {
        if (isTranscribing) return;
        if (segment == null || segment.Length < SAMPLE_RATE * 0.2f) return;
        _ = Transcribe(segment);
    }

    private async Task Transcribe(float[] pcm)
    {
        isTranscribing = true;
        try
        {
            string txt = await speechRecognizer.SpeechTranscriptionAsync(pcm, SAMPLE_RATE);
            Debug.Log("[ASR] => " + txt);
            if (!string.IsNullOrWhiteSpace(txt)) FindBestMatch(txt);
        }
        finally { isTranscribing = false; }
    }

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
                    if (sc > bestScore) { bestScore = sc; bestSpell = s; }
                }
            }
        }

        if (bestSpell != null && bestScore >= 0.55f)
        {
            Debug.Log($"[SPELL] ASR mengenali: {bestSpell.spellName}");
            _spellToCast = bestSpell;
            _isSpellActionPending = true;
        }
        else
        {
            Debug.Log($"[Spell] No match (best={bestScore:0.00})");
        }
    }

    void OnSpellAction(Spell recognizedSpell)
    {
        if (_pendingSpellName != null)
        {
            if (recognizedSpell.spellName == _pendingSpellName)
            {
                Debug.Log($"[SUCCESS] Ucapan cocok: {_pendingSpellName}");
                if (spellManager != null)
                {
                    spellManager.CastSpellWithAksara(_pendingSpellName);
                }
                else
                {
                    projectileShooter?.TryShoot(_pendingSpellName);
                }
                // Catatan: Jika sukses, SpellManager akan menangani dissolve/refresh UI
            }
            else
            {
                Debug.Log($"[FAIL] Ucapan '{recognizedSpell.spellName}' ≠ '{_pendingSpellName}'");

                // [BARU] Panggil fungsi reset seleksi agar kartu kembali normal (Unselected)
                if (spellManager != null)
                {
                    spellManager.ResetSelection();
                }
            }

            _pendingSpellName = null;
            StopListening();
        }
        else
        {
            Debug.Log($"[FREE CAST] Mengeluarkan spell: {recognizedSpell.spellName}");
            if (spellManager != null)
                spellManager.CastSpellWithAksara(recognizedSpell.spellName);
        }
    }

    public static float HangulSimilarity(string a, string b)
    {
        var A = HangulJamo.Decompose(a);
        var B = HangulJamo.Decompose(b);

        int maxLen = Mathf.Max(A.Count, B.Count);
        if (maxLen == 0) return 0f;

        int minLen = Mathf.Min(A.Count, B.Count);

        float t = 0f;
        for (int i = 0; i < minLen; i++)
        {
            if (A[i].initial == B[i].initial) t += 0.5f;
            if (A[i].vowel == B[i].vowel) t += 0.3f;
            if (A[i].finalJ == B[i].finalJ) t += 0.2f;
        }

        return t / maxLen;
    }

    public void SetPendingSpell(string spellName)
    {
        _pendingSpellName = spellName;
        Debug.Log($"[SpeechSpellcaster] Menunggu ucapan untuk spell: '{spellName}'");
        StartListening();
    }

    void OnDestroy()
    {
        Microphone.End(null);
        vad?.Dispose();
        speechRecognizer?.Dispose();
    }
}