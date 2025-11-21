using UnityEngine;
using System.Linq;
using Eitan.SherpaOnnxUnity.Runtime;
using Eitan.SherpaOnnxUnity.Samples;
using System.Collections.Generic;
using System.Threading.Tasks;

//
// ===========================
//   CLASS SPELL
// ===========================
[System.Serializable]
public class Spell
{
    public string spellName;
    public List<string> variations;  // e.g. "레떼", "레테", "렛테"
}

//
// ===========================
//   MAIN SCRIPT
// ===========================
public class SpeechSpellcaster : MonoBehaviour
{
    [Header("Pengaturan Mantra")]
    [SerializeField] private Spell[] spells;

    [Header("Pengaturan Proyektil")]
    public GameObject projectilePrefab;
    public float shootForce = 12f;
    public float spawnOffset = 0.25f;
    public float projectileLifetime = 8f;

    [Header("Konfigurasi Model")]
    [SerializeField] private string koreanAsrModelID = "sherpa-onnx-zipformer-ctc-ko-int8-2024-05-02";
    [SerializeField] private string vadModelID = "silero_vad_v5";

    private SpeechRecognition speechRecognizer;
    private VoiceActivityDetection vad;
    private Mic.Device microphoneDevice;

    private const int SAMPLE_RATE = 16000;
    private bool isTranscribing = false;

    private Spell _spellToCast = null;
    private volatile bool _isSpellActionPending = false;

    void Start()
    {
        InitializeSystems();
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

    private void InitializeSystems()
    {
        speechRecognizer = new SpeechRecognition(koreanAsrModelID, SAMPLE_RATE);
        Debug.Log($"ASR Korea loaded: {koreanAsrModelID}");

        vad = new VoiceActivityDetection(vadModelID, SAMPLE_RATE);
        vad.OnSpeechSegmentDetected += HandleSpeechSegmentDetected;
        Debug.Log("VAD Ready (Silero v5)");

        StartRecording();
    }

    private void StartRecording()
    {
        if (!Mic.Initialized) Mic.Init();

        var devices = Mic.AvailableDevices;
        if (devices.Count > 0)
        {
            microphoneDevice = devices[0];
            microphoneDevice.OnFrameCollected += HandleAudioFrameCollected;
            microphoneDevice.StartRecording(SAMPLE_RATE, 160);
            Debug.Log($"Recording with mic: {microphoneDevice.Name}");
        }
        else
        {
            Debug.LogError("No microphone found!");
        }
    }

    private void HandleAudioFrameCollected(int sampleRate, int channelCount, float[] pcm)
    {
        vad?.StreamDetect(pcm);
    }

    private void HandleSpeechSegmentDetected(float[] segment)
    {
        if (isTranscribing) return;
        if (segment == null || segment.Length < SAMPLE_RATE * 0.2f) return;

        Debug.Log($"[VAD] Speech detected ({segment.Length} samples).");
        _ = TranscribeAndCheckSpellsAsync(segment);
    }

    private async Task TranscribeAndCheckSpellsAsync(float[] audioData)
    {
        isTranscribing = true;

        try
        {
            string text = await speechRecognizer.SpeechTranscriptionAsync(audioData, SAMPLE_RATE);
            Debug.Log($"[ASR] => '{text}'");

            if (!string.IsNullOrWhiteSpace(text))
                FindBestMatchInTranscription(text);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[ASR ERROR]: {ex.Message}");
        }
        finally
        {
            isTranscribing = false;
        }
    }

    //
    // ===========================
    //   HANGUL JAMO FUZZY MATCH
    // ===========================
    //
    private void FindBestMatchInTranscription(string transcribedText)
    {
        string[] words = transcribedText.Trim().Split(' ');

        Spell bestSpell = null;
        float bestScore = 0f;

        foreach (Spell spell in spells)
        {
            foreach (string variation in spell.variations)
            {
                string cleanVar = variation.Trim();

                foreach (string w in words)
                {
                    float score = HangulSimilarity(w, cleanVar);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestSpell = spell;
                    }
                }
            }
        }

        const float THRESHOLD = 0.55f;

        if (bestSpell != null && bestScore >= THRESHOLD)
        {
            Debug.Log($"[SPELL MATCH] {bestSpell.spellName} (Similarity={bestScore:F2})");

            _spellToCast = bestSpell;
            _isSpellActionPending = true;
        }
        else
        {
            Debug.Log($"[NO MATCH] Highest similarity={bestScore:F2}");
        }
    }

    //
    // ===========================
    //   HANGUL DECOMPOSER
    // ===========================
    //
    public static float HangulSimilarity(string a, string b)
    {
        var A = HangulJamo.Decompose(a);
        var B = HangulJamo.Decompose(b);

        int len = Mathf.Min(A.Count, B.Count);
        if (len == 0) return 0f;

        float total = 0f;

        for (int i = 0; i < len; i++)
        {
            if (A[i].initial == B[i].initial) total += 0.5f;
            if (A[i].vowel == B[i].vowel) total += 0.3f;
            if (A[i].finalJ == B[i].finalJ) total += 0.2f;
        }

        return total / len;
    }


    //
    // ===========================
    //   SPELL ACTION
    // ===========================
    //
    private void OnSpellAction(Spell detectedSpell)
    {
        Debug.Log($"Spell Detected → {detectedSpell.spellName}");

        switch (detectedSpell.spellName)
        {
            case "Lette":
                TryShoot();
                break;

            default:
                Debug.LogWarning($"No action defined for spell '{detectedSpell.spellName}'");
                break;
        }
    }

    private void TryShoot()
    {
        if (projectilePrefab == null) return;

        Transform cam = Camera.main.transform;
        Vector3 spawnPos = cam.TransformPoint(Vector3.forward * spawnOffset);
        Quaternion spawnRot = cam.rotation;

        GameObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot);
        Rigidbody rb = proj.GetComponent<Rigidbody>() ?? proj.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.AddForce(cam.forward * shootForce, ForceMode.Impulse);

        Destroy(proj, projectileLifetime);
        Debug.Log("Projectile fired!");
    }

    private void OnDestroy()
    {
        if (microphoneDevice != null)
        {
            microphoneDevice.StopRecording();
            microphoneDevice.OnFrameCollected -= HandleAudioFrameCollected;
        }

        vad?.Dispose();
        speechRecognizer?.Dispose();
    }
}

//
// ===========================
//   HANGUL JAMO DECOMPOSER
// ===========================
public static class HangulJamo
{
    private const int BaseCode = 0xAC00;

    private static readonly char[] initials =
    {
        'ㄱ','ㄲ','ㄴ','ㄷ','ㄸ','ㄹ','ㅁ','ㅂ','ㅃ','ㅅ','ㅆ','ㅇ','ㅈ','ㅉ','ㅊ','ㅋ','ㅌ','ㅍ','ㅎ'
    };

    private static readonly char[] vowels =
    {
        'ㅏ','ㅐ','ㅑ','ㅒ','ㅓ','ㅔ','ㅕ','ㅖ','ㅗ','ㅘ','ㅙ','ㅚ','ㅛ','ㅜ','ㅝ','ㅞ','ㅟ','ㅠ','ㅡ','ㅢ','ㅣ'
    };

    private static readonly char[] finals =
    {
        '\0','ㄱ','ㄲ','ㄳ','ㄴ','ㄵ','ㄶ','ㄷ','ㄹ','ㄺ','ㄻ','ㄼ','ㄽ','ㄾ','ㄿ','ㅀ','ㅁ','ㅂ','ㅄ','ㅅ','ㅆ','ㅇ','ㅈ','ㅊ','ㅋ','ㅌ','ㅍ','ㅎ'
    };

    public struct JamoTriple
    {
        public char initial;
        public char vowel;
        public char finalJ;
    }

    public static List<JamoTriple> Decompose(string text)
    {
        List<JamoTriple> list = new List<JamoTriple>();

        foreach (char ch in text)
        {
            if (ch < BaseCode || ch > 0xD7A3)
                continue;

            int code = ch - BaseCode;
            int i = code / (21 * 28);
            int v = (code % (21 * 28)) / 28;
            int f = code % 28;

            list.Add(new JamoTriple
            {
                initial = initials[i],
                vowel = vowels[v],
                finalJ = finals[f]
            });
        }

        return list;
    }
}
