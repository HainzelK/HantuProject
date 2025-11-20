using UnityEngine;
using System.Linq;
using Eitan.SherpaOnnxUnity.Runtime;
using Eitan.SherpaOnnxUnity.Samples;
using System.Collections.Generic;
using System.Threading.Tasks;

// Kelas Spell tetap sama
[System.Serializable]
public class Spell
{
    public string spellName;
    public List<string> variations;
}

public class SpeechSpellcaster : MonoBehaviour
{
    [Header("Pengaturan Mantra")]
    [SerializeField] private Spell[] spells;
    [Tooltip("Maksimal 'kesalahan' yang diizinkan saat mencocokkan kata. 0 = persis, 1 = satu huruf salah, dst.")]
    [SerializeField] private int similarityThreshold = 1;

    [Header("Pengaturan Proyektil")]
    public GameObject projectilePrefab;
    public float shootForce = 12f;
    public float spawnOffset = 0.25f;
    public float projectileLifetime = 8f;

    [Header("Konfigurasi Model")]
    [SerializeField]
    private string koreanAsrModelID = "sherpa-onnx-zipformer-ctc-ko-int8-2024-05-02";
    [SerializeField]
    private string vadModelID = "silero-vad";

    private SpeechRecognition speechRecognizer;
    private VoiceActivityDetection vad;
    private Mic.Device microphoneDevice;
    private const int SAMPLE_RATE = 16000;
    private bool isTranscribing = false;

    // BARU: Variabel untuk komunikasi antar thread
    private Spell _spellToCast = null;
    private volatile bool _isSpellActionPending = false; // 'volatile' untuk keamanan thread

    private void Start()
    {
        InitializeSystems();
    }

    // BARU: Tambahkan fungsi Update() yang berjalan di Main Thread
    private void Update()
    {
        // Periksa setiap frame apakah ada aksi mantra yang menunggu untuk dieksekusi
        if (_isSpellActionPending)
        {
            // Karena ini di dalam Update(), kita berada di Main Thread
            OnSpellAction(_spellToCast);

            // Reset flag setelah aksi selesai
            _spellToCast = null;
            _isSpellActionPending = false;
        }
    }

    private void InitializeSystems()
    {
        speechRecognizer = new SpeechRecognition(koreanAsrModelID, SAMPLE_RATE);
        Debug.Log($"Model Speech Recognition Korea '{koreanAsrModelID}' berhasil dimuat.");

        vad = new VoiceActivityDetection(vadModelID, SAMPLE_RATE);
        vad.OnSpeechSegmentDetected += HandleSpeechSegmentDetected;
        Debug.Log("VAD berhasil diinisialisasi dan siap mendeteksi ucapan.");

        StartRecording();
        Debug.Log("Sistem siap. Silakan ucapkan mantra dalam Bahasa Korea...");
    }

    private void StartRecording()
    {
        if (!Mic.Initialized) Mic.Init();
        var devices = Mic.AvailableDevices;
        if (devices.Count > 0)
        {
            microphoneDevice = devices[0];
            microphoneDevice.OnFrameCollected += HandleAudioFrameCollected;
            microphoneDevice.StartRecording(SAMPLE_RATE, 10);
            Debug.Log($"Mulai merekam dengan device: {microphoneDevice.Name}");
        }
        else
        {
            Debug.LogError("Tidak ada mikrofon yang ditemukan!");
        }
    }

    private void HandleAudioFrameCollected(int sampleRate, int channelCount, float[] pcm)
    {
        vad?.StreamDetect(pcm);
    }

    private void HandleSpeechSegmentDetected(float[] speechSegment)
    {
        if (isTranscribing) return;
        if (speechSegment == null || speechSegment.Length < SAMPLE_RATE * 0.2f) return;

        Debug.Log($"[VAD]: Ucapan terdeteksi ({speechSegment.Length} sampel). Memulai transkripsi...");
        _ = TranscribeAndCheckSpellsAsync(speechSegment);
    }

    private async Task TranscribeAndCheckSpellsAsync(float[] audioData)
    {
        isTranscribing = true;
        try
        {
            string transcribedText = await speechRecognizer.SpeechTranscriptionAsync(audioData, SAMPLE_RATE);
            Debug.Log($"[Hasil Transkripsi]: '{transcribedText}'");
            if (!string.IsNullOrWhiteSpace(transcribedText))
            {
                FindBestMatchInTranscription(transcribedText);
            }
        }
        catch (System.Exception ex)
        {
            // DIUBAH: Kita tidak bisa memanggil Debug.LogError dari background thread, jadi kita akan menanganinya dengan cara yang aman
            Debug.LogWarning($"[ERROR DI BACKGROUND THREAD]: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            isTranscribing = false;
        }
    }

    private void FindBestMatchInTranscription(string transcribedText)
    {
        // 1. Pecah kalimat menjadi kata-kata
        string[] transcribedWords = transcribedText.Trim().Split(' ');

        Spell bestOverallSpell = null;
        int bestOverallDistance = int.MaxValue;

        // 2. Iterasi melalui setiap mantra yang kita miliki (Lette, Uwae, dll.)
        foreach (Spell spell in spells)
        {
            int bestDistanceForThisSpell = int.MaxValue;

            // 3. Cari jarak terdekat untuk MANTRA INI dari semua kata yang ditranskripsi
            foreach (string variation in spell.variations)
            {
                string cleanVariation = variation.Trim();
                foreach (string word in transcribedWords)
                {
                    int currentDistance = LevenshteinDistance(word, cleanVariation);
                    if (currentDistance < bestDistanceForThisSpell)
                    {
                        bestDistanceForThisSpell = currentDistance;
                    }
                }
            }

            // 4. Setelah memeriksa semua variasi untuk mantra ini,
            // kita sekarang punya jarak terbaiknya. Bandingkan dengan juara bertahan.
            // Jika jarak untuk mantra ini lebih baik, ia menjadi juara baru.
            if (bestDistanceForThisSpell < bestOverallDistance)
            {
                bestOverallDistance = bestDistanceForThisSpell;
                bestOverallSpell = spell;
            }
        }

        // 5. Setelah semua mantra "bersaing", kita lihat siapa pemenangnya
        // dan apakah skornya cukup bagus (di bawah threshold).
        if (bestOverallSpell != null && bestOverallDistance <= similarityThreshold)
        {
            Debug.Log($"Pemenang Kompetisi Mantra: '{bestOverallSpell.spellName}', Jarak: {bestOverallDistance} (Threshold: {similarityThreshold})");

            // PENTING: Set flag untuk dieksekusi oleh Main Thread
            _spellToCast = bestOverallSpell;
            _isSpellActionPending = true;
        }
        else
        {
            Debug.Log($"Tidak ada mantra yang cocok ditemukan. Pemenang: '{bestOverallSpell?.spellName}', Jarak: {bestOverallDistance} (Threshold: {similarityThreshold})");
        }
    }

    private void OnSpellAction(Spell detectedSpell)
    {
        Debug.Log($"MANTRA '{detectedSpell.spellName}' TERDETEKSI! Menjalankan aksi...");
        switch (detectedSpell.spellName)
        {
            case "Lette":
                TryShoot();
                break;
            default:
                Debug.LogWarning($"Mantra '{detectedSpell.spellName}' tidak memiliki aksi yang ditentukan.");
                break;
        }
    }

    void TryShoot()
    {
        // Fungsi ini sekarang 100% aman karena dipanggil dari Update()
        if (projectilePrefab == null) return;
        Transform cam = Camera.main.transform;
        Vector3 spawnPos = cam.TransformPoint(Vector3.forward * spawnOffset);
        Quaternion spawnRot = cam.rotation;
        GameObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot);
        Rigidbody rb = proj.GetComponent<Rigidbody>() ?? proj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.AddForce(cam.forward * shootForce, ForceMode.Impulse);
        Destroy(proj, projectileLifetime);
        Debug.Log($"[SpeechSpellcaster] Menembakkan proyektil.");
    }

    private void OnDestroy()
    {
        if (microphoneDevice != null)
        {
            microphoneDevice.StopRecording();
            microphoneDevice.OnFrameCollected -= HandleAudioFrameCollected;
        }
        if (speechRecognizer != null) speechRecognizer.Dispose();
        if (vad != null)
        {
            vad.OnSpeechSegmentDetected -= HandleSpeechSegmentDetected;
            vad.Dispose();
        }
    }

    public static int LevenshteinDistance(string s, string t)
    {
        s = s.ToLower();
        t = t.ToLower();
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];
        if (n == 0) return m;
        if (m == 0) return n;
        for (int i = 0; i <= n; d[i, 0] = i++) ;
        for (int j = 0; j <= m; d[0, j] = j++) ;
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Mathf.Min(Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }

}