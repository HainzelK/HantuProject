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

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float shootForce = 12f;
    public float spawnOffset = 0.25f;
    public float projectileLifetime = 8f;

    [Header("Model Config")]
    [SerializeField] private string koreanAsrModelID = "sherpa-onnx-zipformer-korean-2024-06-24";
    [SerializeField] private string vadModelID = "silero_vad_v5";

    private SpeechRecognition speechRecognizer;
    private VoiceActivityDetection vad;

    private AudioClip micClip;
    private const int SAMPLE_RATE = 16000;

    private bool isTranscribing = false;
    private Spell _spellToCast = null;
    private bool _isSpellActionPending = false;
    
    void Start()
    {
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
        if (s.spellName == "Lette")
            TryShoot();
    }

    void TryShoot()
    {
        Transform cam = Camera.main.transform;

        Vector3 pos = cam.TransformPoint(Vector3.forward * spawnOffset);
        Quaternion rot = cam.rotation;

        GameObject proj = Instantiate(projectilePrefab, pos, rot);

        Rigidbody rb = proj.GetComponent<Rigidbody>() ?? proj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.AddForce(cam.forward * shootForce, ForceMode.Impulse);

        Destroy(proj, projectileLifetime);
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

// ===============================================
//   HANGUL DECOMPOSER
// ===============================================
public static class HangulJamo
{
    private const int Base = 0xAC00;

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
        public char initial, vowel, finalJ;
    }

    public static List<JamoTriple> Decompose(string text)
    {
        List<JamoTriple> list = new List<JamoTriple>();

        foreach (char c in text)
        {
            if (c < Base || c > 0xD7A3) continue;

            int code = c - Base;
            list.Add(new JamoTriple
            {
                initial = initials[code / (21 * 28)],
                vowel = vowels[(code % (21 * 28)) / 28],
                finalJ = finals[code % 28]
            });
        }

        return list;
    }
}
