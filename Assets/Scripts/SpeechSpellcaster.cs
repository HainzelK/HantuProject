using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Diperlukan untuk fungsi .Max()

[System.Serializable]
public class Spell
{
    [Tooltip("Nama utama mantra ini (untuk logika game Anda).")]
    public string spellName;
    [Tooltip("Daftar semua kemungkinan cara Whisper akan menulis mantra ini (termasuk bahasa lain).")]
    public List<string> variations;
}

public class SpeechSpellcaster : MonoBehaviour
{
    // --- PENGATURAN YANG ADA ---
    [Header("Pengaturan Mantra")]
    [SerializeField] private Spell[] spells;
    [Tooltip("Seberapa toleran pencocokan kata? 1 atau 2 direkomendasikan.")]
    [SerializeField] private int similarityThreshold = 2;

    [Header("Pengaturan Proyektil")]
    public GameObject projectilePrefab;
    public float shootForce = 12f;
    public float spawnOffset = 0.25f;
    public float projectileLifetime = 8f;

    // --- PENGATURAN BARU UNTUK DETEKSI KONTINU ---
    [Header("Deteksi Suara Kontinu")]
    [Tooltip("Ambang batas volume (0-1) untuk dianggap sebagai 'suara'. Sesuaikan berdasarkan kebisingan latar belakang.")]
    [Range(0.01f, 1f)]
    public float volumeThreshold = 0.05f;

    [Tooltip("Berapa lama keheningan (dalam detik) setelah berbicara untuk memicu pengiriman API.")]
    public float silenceDurationThreshold = 1.5f;

    [Tooltip("Seberapa sering (dalam detik) kita memeriksa audio baru dari mikrofon.")]
    public float checkInterval = 0.1f;

    // --- VARIABEL INTERNAL ---
    private AudioClip recordingClip;
    private bool isListeningForSpeech = false;
    private bool isCurrentlySending = false;
    private List<float> currentSpeechSamples = new List<float>();
    private float silenceTimer = 0;
    private int lastSamplePosition = 0;
    private const int RECORDING_BUFFER_SECONDS = 10;
    private const int SAMPLE_RATE = 16000;

    private const string API_URL = "https://router.huggingface.co/hf-inference/models/openai/whisper-large-v3-turbo";
    private string hfToken;

    private void Awake()
    {
        DotEnv.Load();
        hfToken = DotEnv.Get("HF_TOKEN");

        if (string.IsNullOrEmpty(hfToken))
        {
            Debug.LogError("Hugging Face token (HF_TOKEN) tidak ditemukan di file .env!");
        }
    }

    private void Start()
    {
        // true = looping, RECORDING_BUFFER_SECONDS = durasi buffer, SAMPLE_RATE = kualitas
        recordingClip = Microphone.Start(null, true, RECORDING_BUFFER_SECONDS, SAMPLE_RATE);
        Debug.Log("Mulai mendengarkan mantra secara terus-menerus...");
        StartCoroutine(ContinuousDetectionLoop());
    }

    private IEnumerator ContinuousDetectionLoop()
    {
        while (true)
        {
            // Jangan proses audio baru jika sedang menunggu respons dari API
            if (isCurrentlySending)
            {
                yield return new WaitForSeconds(checkInterval);
                continue;
            }

            // Dapatkan sampel audio baru sejak pemeriksaan terakhir
            int currentPosition = Microphone.GetPosition(null);
            if (currentPosition != lastSamplePosition)
            {
                ProcessAudioChunk(currentPosition);
                lastSamplePosition = currentPosition;
            }

            // Jika kita sedang dalam proses mendengarkan ucapan...
            if (isListeningForSpeech)
            {
                // ...dan sekarang hening, mulai timer keheningan.
                silenceTimer += checkInterval;
                if (silenceTimer >= silenceDurationThreshold)
                {
                    // Keheningan sudah cukup lama, artinya pengguna selesai berbicara.
                    // Proses audio yang telah dikumpulkan.
                    FinalizeAndSendSpeech();
                }
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }

    private void ProcessAudioChunk(int currentPosition)
    {
        int length = 0;
        // Tangani kasus di mana buffer mikrofon berputar kembali ke awal
        if (currentPosition < lastSamplePosition)
        {
            length = (RECORDING_BUFFER_SECONDS * SAMPLE_RATE) - lastSamplePosition + currentPosition;
        }
        else
        {
            length = currentPosition - lastSamplePosition;
        }

        if (length == 0) return;

        float[] chunkData = new float[length];
        recordingClip.GetData(chunkData, lastSamplePosition);

        // Periksa apakah ada suara di potongan audio ini
        float maxVolume = chunkData.Max(Mathf.Abs);
        if (maxVolume > volumeThreshold)
        {
            // Ada suara!
            // Jika kita belum mulai mendengarkan, sekaranglah saatnya.
            if (!isListeningForSpeech)
            {
                isListeningForSpeech = true;
                Debug.Log("Suara terdeteksi, mulai merekam ucapan...");
            }

            // Tambahkan data audio ke koleksi kita dan reset timer keheningan
            currentSpeechSamples.AddRange(chunkData);
            silenceTimer = 0;
        }
    }

    private void FinalizeAndSendSpeech()
    {
        isListeningForSpeech = false;

        if (currentSpeechSamples.Count == 0)
        {
            Debug.Log("Ucapan selesai tetapi tidak ada data audio, mengabaikan.");
            return;
        }

        Debug.Log($"Ucapan selesai. Mengirim {currentSpeechSamples.Count} sampel audio ke API.");

        // Salin data ke array baru
        float[] speechData = currentSpeechSamples.ToArray();
        // Kosongkan list untuk ucapan berikutnya
        currentSpeechSamples.Clear();

        // Encode sebagai WAV dan kirim
        byte[] wavData = EncodeAsWAV(speechData, SAMPLE_RATE, recordingClip.channels);
        StartCoroutine(SendRecording(wavData));
    }

    private IEnumerator SendRecording(byte[] audioBytes)
    {
        isCurrentlySending = true;
        Debug.Log("Mengirim rekaman ke server...");

        using (var request = new UnityWebRequest(API_URL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(audioBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "audio/wav");
            request.SetRequestHeader("Authorization", $"Bearer {hfToken}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log($"Respons JSON Mentah dari Server: {jsonResponse}");
                SpeechRecognitionResponse response = JsonUtility.FromJson<SpeechRecognitionResponse>(jsonResponse);
                CheckForKeywords(response.text);
            }
            else
            {
                Debug.LogError($"Error: {request.responseCode} - {request.downloadHandler.text}");
            }
        }

        // Setelah selesai (baik berhasil maupun gagal), reset flag
        isCurrentlySending = false;
        Debug.Log("Siap mendengarkan mantra berikutnya.");
    }

    // --- Sisa skrip (CheckForKeywords, OnKeywordDetected, TryShoot, dll) tetap sama ---
    #region Unchanged Methods

    private void CheckForKeywords(string transcribedText)
    {
        if (string.IsNullOrWhiteSpace(transcribedText))
        {
            Debug.Log("(Tidak ada ucapan terdeteksi oleh API)");
            return;
        }

        string lowercasedText = transcribedText.ToLower().Trim();
        Spell overallBestSpell = null;
        int overallMinDistance = int.MaxValue;

        foreach (Spell spell in spells)
        {
            int currentSpellMinDistance = int.MaxValue;
            foreach (string variation in spell.variations)
            {
                int distance = LevenshteinDistance(lowercasedText, variation.ToLower().Trim());
                if (distance < currentSpellMinDistance)
                {
                    currentSpellMinDistance = distance;
                }
            }
            if (currentSpellMinDistance < overallMinDistance)
            {
                overallMinDistance = currentSpellMinDistance;
                overallBestSpell = spell;
            }
        }

        Debug.Log($"Pencocokan terbaik: spell '{overallBestSpell?.spellName}' dengan jarak keseluruhan: {overallMinDistance}");

        if (overallBestSpell != null && overallMinDistance <= similarityThreshold)
        {
            Debug.Log($"MANTRA TERDETEKSI: {overallBestSpell.spellName} (Hasil asli: {transcribedText})");
            OnKeywordDetected(overallBestSpell);
        }
        else
        {
            Debug.Log($"Tidak ada mantra yang cocok. Hasil asli: {transcribedText}");
        }
    }

    private void OnKeywordDetected(Spell detectedSpell)
    {
        Debug.Log($"MANTRA '{detectedSpell.spellName}' TERDETEKSI! Menjalankan aksi...");

        switch (detectedSpell.spellName)
        {
            case "Lette":
            case "Api":
            case "uae":
                TryShoot();
                break;
            default:
                Debug.LogWarning($"Mantra '{detectedSpell.spellName}' tidak memiliki aksi yang ditentukan.");
                break;
        }
    }

    void TryShoot()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("[SpeechSpellcaster] projectilePrefab BELUM di-assign di Inspector!");
            return;
        }

        Transform cam = transform;
        Vector3 spawnPos = cam.TransformPoint(Vector3.forward * spawnOffset);
        Quaternion spawnRot = cam.rotation;

        GameObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot);
        if (proj == null)
        {
            Debug.LogError("[SpeechSpellcaster] Instantiate mengembalikan null!");
            return;
        }

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = proj.AddComponent<Rigidbody>();
            rb.useGravity = false;
        }

        Collider col = proj.GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sc = proj.AddComponent<SphereCollider>();
            sc.radius = 0.1f;
        }

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(cam.forward * shootForce, ForceMode.Impulse);

        Destroy(proj, projectileLifetime);

        Debug.Log("[SpeechSpellcaster] Menembakkan proyektil dari " + spawnPos);
    }

    [System.Serializable]
    private class SpeechRecognitionResponse
    {
        public string text;
    }

    public static int LevenshteinDistance(string s, string t)
    {
        s = s ?? "";
        t = t ?? "";
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
                d[i, j] = Mathf.Min(
                    Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }

    private byte[] EncodeAsWAV(float[] samples, int frequency, int channels)
    {
        using (var memoryStream = new MemoryStream(44 + samples.Length * 2))
        {
            using (var writer = new BinaryWriter(memoryStream))
            {
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + samples.Length * 2);
                writer.Write("WAVE".ToCharArray());
                writer.Write("fmt ".ToCharArray());
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2);
                writer.Write((ushort)(channels * 2));
                writer.Write((ushort)16);
                writer.Write("data".ToCharArray());
                writer.Write(samples.Length * 2);

                foreach (var sample in samples)
                {
                    writer.Write((short)(sample * short.MaxValue));
                }
            }
            return memoryStream.ToArray();
        }
    }

    #endregion
}