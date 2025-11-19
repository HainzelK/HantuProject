using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.InferenceEngine;
using Newtonsoft.Json;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

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
    [SerializeField] private int similarityThreshold = 2;

    [Header("Pengaturan Proyektil")]
    public GameObject projectilePrefab;
    public float shootForce = 12f;
    public float spawnOffset = 0.25f;
    public float projectileLifetime = 8f;

    [Header("Deteksi Suara Kontinu")]
    [SerializeField, Range(0.01f, 1f)] private float volumeThreshold = 0.05f;
    [SerializeField] private float silenceDurationThreshold = 1.5f;
    [SerializeField] private float checkInterval = 0.1f;

    [Header("Model Inference Engine")]
    [SerializeField] private ModelAsset spectrogramModelAsset;
    [SerializeField] private ModelAsset encoderModelAsset;
    [SerializeField] private ModelAsset decoderModelAsset;
    [SerializeField] private TextAsset vocabJsonAsset;

    private Worker spectrogramWorker, encoderWorker, decoderWorker;
    private Dictionary<int, string> tokens;
    private bool isEngineReady = false;
    private bool isCurrentlyTranscribing = false;

    private AudioClip recordingClip;
    private List<float> currentSpeechSamples = new List<float>();
    private bool isListeningForSpeech = false;
    private float silenceTimer = 0;
    private int lastSamplePosition = 0;
    private const int RECORDING_BUFFER_SECONDS = 10;
    private const int SAMPLE_RATE = 16000;
    private const int WHISPER_EXPECTED_SAMPLES = 30 * SAMPLE_RATE;
    private const int VOCAB_SIZE = 51865;

    private void Start()
    {
        StartCoroutine(InitializeAndStart());
    }

    private IEnumerator InitializeAndStart()
    {
#if UNITY_ANDROID
    if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
    {
        Permission.RequestUserPermission(Permission.Microphone);
        
        float timeout = 0f;
        while (!Permission.HasUserAuthorizedPermission(Permission.Microphone) && timeout < 10f)
        {
            yield return null;
            timeout += Time.unscaledDeltaTime;
        }
    }
#endif

        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Debug.Log("Izin mikrofon telah diberikan! Memulai aplikasi...");
            LoadInferenceEngine();
            recordingClip = Microphone.Start(null, true, RECORDING_BUFFER_SECONDS, SAMPLE_RATE);
            if (recordingClip == null)
            {
                Debug.LogError("GAGAL MEMULAI MIKROFON! recordingClip == null");
                yield break; // ✅ DIPERBAIKI DI SINI
            }
            Debug.Log("Mulai mendengarkan mantra secara terus-menerus...");
            StartCoroutine(ContinuousDetectionLoop());
        }
        else
        {
            Debug.LogError("Izin mikrofon tidak diberikan!");
            yield break; // ✅ Opsional: tambahkan ini juga untuk konsistensi
        }
    }

    private void LoadInferenceEngine()
    {
        var spectrogramModel = ModelLoader.Load(spectrogramModelAsset);
        var encoderModel = ModelLoader.Load(encoderModelAsset);
        var decoderModel = ModelLoader.Load(decoderModelAsset);

        spectrogramWorker = new Worker(spectrogramModel, BackendType.GPUCompute);
        encoderWorker = new Worker(encoderModel, BackendType.GPUCompute);
        decoderWorker = new Worker(decoderModel, BackendType.GPUCompute);

        var vocab = JsonConvert.DeserializeObject<Dictionary<string, int>>(vocabJsonAsset.text);
        tokens = vocab.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        isEngineReady = true;
        Debug.Log("Inference Engine berhasil dimuat dan siap.");
    }

    private IEnumerator ContinuousDetectionLoop()
    {
        while (true)
        {
            if (isCurrentlyTranscribing || !isEngineReady) { yield return new WaitForSeconds(checkInterval); continue; }
            int currentPosition = Microphone.GetPosition(null);
            if (currentPosition != lastSamplePosition) { ProcessAudioChunk(currentPosition); lastSamplePosition = currentPosition; }
            if (isListeningForSpeech) { silenceTimer += checkInterval; if (silenceTimer >= silenceDurationThreshold) { FinalizeAndTranscribeSpeech(); } }
            yield return new WaitForSeconds(checkInterval);
        }
    }

    private void ProcessAudioChunk(int currentPosition)
    {
        int length = 0;
        if (currentPosition < lastSamplePosition) length = (RECORDING_BUFFER_SECONDS * SAMPLE_RATE) - lastSamplePosition + currentPosition;
        else length = currentPosition - lastSamplePosition;
        if (length == 0) return;
        float[] chunkData = new float[length];
        recordingClip.GetData(chunkData, lastSamplePosition);
        if (chunkData.Max(Mathf.Abs) > volumeThreshold) { if (!isListeningForSpeech) { isListeningForSpeech = true; Debug.Log("Suara terdeteksi..."); } currentSpeechSamples.AddRange(chunkData); silenceTimer = 0; }
    }

    private void FinalizeAndTranscribeSpeech()
    {
        isListeningForSpeech = false;
        if (currentSpeechSamples.Count == 0) return;
        isCurrentlyTranscribing = true;
        Debug.Log($"Ucapan selesai. Memproses {currentSpeechSamples.Count} sampel...");
        float[] speechData = currentSpeechSamples.ToArray();
        currentSpeechSamples.Clear();
        StartCoroutine(TranscribeAudio(speechData));
    }

    private IEnumerator TranscribeAudio(float[] audioData)
    {
        var paddedAudio = new float[WHISPER_EXPECTED_SAMPLES];
        System.Array.Copy(audioData, paddedAudio, audioData.Length);
        using var inputTensor = new Tensor<float>(new TensorShape(1, WHISPER_EXPECTED_SAMPLES), paddedAudio);

        spectrogramWorker.Schedule(inputTensor);
        var spectrogramTensor = spectrogramWorker.PeekOutput() as Tensor<float>;
        yield return new WaitForEndOfFrame();

        encoderWorker.Schedule(spectrogramTensor);
        var encodedTensor = encoderWorker.PeekOutput() as Tensor<float>;
        yield return new WaitForEndOfFrame();

        var outputTokens = new List<int> { tokens.FirstOrDefault(t => t.Value == "<|startoftranscript|>").Key, tokens.FirstOrDefault(t => t.Value == "<|notimestamps|>").Key };
        string transcribedText = "";

        for (int i = 0; i < 100; i++)
        {
            using var tokensTensor = new Tensor<int>(new TensorShape(1, outputTokens.Count), outputTokens.ToArray());

            decoderWorker.SetInput("encoder_hidden_states", encodedTensor);
            decoderWorker.SetInput("input_ids", tokensTensor);
            decoderWorker.Schedule();

            var outputTensor = decoderWorker.PeekOutput().ReadbackAndClone() as Tensor<float>;

            int lastTokenIndex = outputTokens.Count - 1;
            int nextTokenId = 0;
            float maxProb = float.MinValue;

            for (int j = 0; j < VOCAB_SIZE; j++)
            {
                if (outputTensor[0, lastTokenIndex, j] > maxProb)
                {
                    maxProb = outputTensor[0, lastTokenIndex, j];
                    nextTokenId = j;
                }
            }

            outputTensor.Dispose();

            if (tokens.ContainsKey(nextTokenId) && tokens[nextTokenId] == "<|endoftext|>") break;

            outputTokens.Add(nextTokenId);
            if (tokens.ContainsKey(nextTokenId)) transcribedText += tokens[nextTokenId];

            yield return new WaitForEndOfFrame();
        }

        Debug.Log($"Transkripsi Selesai: {transcribedText}");
        CheckForKeywords(transcribedText.Replace("<|startoftranscript|>", "").Replace("<|notimestamps|>", "").Trim());

        isCurrentlyTranscribing = false;
    }

    private void OnDestroy()
    {
        spectrogramWorker?.Dispose();
        encoderWorker?.Dispose();
        decoderWorker?.Dispose();
    }

    private void CheckForKeywords(string transcribedText) { if (string.IsNullOrWhiteSpace(transcribedText)) return; string lowercasedText = transcribedText.ToLower().Trim(); Spell overallBestSpell = null; int overallMinDistance = int.MaxValue; foreach (Spell spell in spells) { foreach (string variation in spell.variations) { int distance = LevenshteinDistance(lowercasedText, variation.ToLower().Trim()); if (distance < overallMinDistance) { overallMinDistance = distance; overallBestSpell = spell; } } } if (overallBestSpell != null && overallMinDistance <= similarityThreshold) { Debug.Log($"MANTRA TERDETEKSI: {overallBestSpell.spellName} (Hasil asli: {transcribedText})"); OnKeywordDetected(overallBestSpell); } else { Debug.Log($"Tidak ada mantra yang cocok. Hasil asli: {transcribedText}"); } }
    private void OnKeywordDetected(Spell detectedSpell) { Debug.Log($"MANTRA '{detectedSpell.spellName}' TERDETEKSI! Menjalankan aksi..."); switch (detectedSpell.spellName) { case "Lette": case "Api": case "uae": TryShoot(); break; default: Debug.LogWarning($"Mantra '{detectedSpell.spellName}' tidak memiliki aksi yang ditentukan."); break; } }
    void TryShoot() { if (projectilePrefab == null) { Debug.LogError("[SpeechSpellcaster] projectilePrefab BELUM di-assign!"); return; } Transform cam = transform; Vector3 spawnPos = cam.TransformPoint(Vector3.forward * spawnOffset); Quaternion spawnRot = cam.rotation; GameObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot); Rigidbody rb = proj.GetComponent<Rigidbody>() ?? proj.AddComponent<Rigidbody>(); rb.useGravity = false; Collider col = proj.GetComponent<Collider>() ?? proj.AddComponent<SphereCollider>(); rb.linearVelocity = Vector3.zero; rb.AddForce(cam.forward * shootForce, ForceMode.Impulse); Destroy(proj, projectileLifetime); Debug.Log("[SpeechSpellcaster] Menembakkan proyektil."); }
    public static int LevenshteinDistance(string s, string t) { s = s ?? ""; t = t ?? ""; int n = s.Length; int m = t.Length; int[,] d = new int[n + 1, m + 1]; if (n == 0) return m; if (m == 0) return n; for (int i = 0; i <= n; d[i, 0] = i++) ; for (int j = 0; j <= m; d[0, j] = j++) ; for (int i = 1; i <= n; i++) { for (int j = 1; j <= m; j++) { int cost = (t[j - 1] == s[i - 1]) ? 0 : 1; d[i, j] = Mathf.Min(Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost); } } return d[n, m]; }
}