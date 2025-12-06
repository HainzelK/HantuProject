using UnityEngine;
using UnityEngine.Networking; // [WAJIB] Untuk membaca file di Android
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TensorFlowLite;
using System.IO;
using System.Text.RegularExpressions;

public class SpeechSpellcaster : MonoBehaviour
{
    [Header("TFLite Settings")]
    public string modelFileName = "model_unquant.tflite";
    public string labelFileName = "labels.txt";

    [Header("Detection Config")]
    [Range(0f, 1f)]
    public float threshold = 0.85f;
    public float detectionInterval = 0.1f;

    [Header("Timeout Settings")]
    public float recordingTimeout = 5.0f;

    private SpellManager spellManager;
    private Interpreter interpreter;

    private float[] inputBuffer;
    private float[] outputBuffer;
    private List<string> labels = new List<string>();

    private AudioClip micClip;
    private const int SAMPLE_RATE = 44100;
    private string _microphoneDevice;
    private bool _isListening = false;
    private string _pendingSpellName = null;

    private float _timer;
    private float _listeningDuration;

    // Ubah void Start menjadi IEnumerator Start
    IEnumerator Start()
    {
        spellManager = FindObjectOfType<SpellManager>();

        // 1. Dapatkan Path yang benar
        string modelPath = Path.Combine(Application.streamingAssetsPath, modelFileName);
        string labelPath = Path.Combine(Application.streamingAssetsPath, labelFileName);

        byte[] modelData = null;
        string labelContent = "";

        // 2. Logika Loading (Beda Android vs PC)
        if (modelPath.Contains("://") || modelPath.Contains("jar:file"))
        {
            // --- ANDROID (Load via UnityWebRequest) ---
            Debug.Log("[TFLite] Loading from Android JAR...");

            // Load Model
            using (UnityWebRequest www = UnityWebRequest.Get(modelPath))
            {
                yield return www.SendWebRequest();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error loading model: {www.error}");
                    yield break;
                }
                modelData = www.downloadHandler.data;
            }

            // Load Labels
            using (UnityWebRequest www = UnityWebRequest.Get(labelPath))
            {
                yield return www.SendWebRequest();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error loading labels: {www.error}");
                    yield break;
                }
                labelContent = www.downloadHandler.text;
            }
        }
        else
        {
            // --- PC / EDITOR (Load via System.IO) ---
            Debug.Log("[TFLite] Loading from File System...");

            if (File.Exists(modelPath))
                modelData = File.ReadAllBytes(modelPath);

            if (File.Exists(labelPath))
                labelContent = File.ReadAllText(labelPath);
        }

        // 3. Validasi Data
        if (modelData == null || string.IsNullOrEmpty(labelContent))
        {
            Debug.LogError("[SpeechSpellcaster] Gagal memuat Model atau Label!");
            yield break;
        }

        // 4. Inisialisasi Interpreter TFLite
        var options = new InterpreterOptions() { threads = 2 };
        interpreter = new Interpreter(modelData, options);
        interpreter.AllocateTensors();

        int inputShape = interpreter.GetInputTensorInfo(0).shape[1];
        inputBuffer = new float[inputShape];
        int outputShape = interpreter.GetOutputTensorInfo(0).shape[1];
        outputBuffer = new float[outputShape];

        // 5. Proses Labels (Regex & Split)
        labels.Clear();
        // Split berdasarkan baris baru (support Windows \r\n dan Unix \n)
        string[] rawLabels = labelContent.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in rawLabels)
        {
            // Hapus angka di depan
            string cleanLabel = Regex.Replace(line, @"^\d+\s*", "").Trim();
            labels.Add(cleanLabel);
        }

        Debug.Log($"[TFLite] Initialization Complete. Loaded {labels.Count} labels.");
    }

    public void SetPendingSpell(string spellName)
    {
        _pendingSpellName = spellName;
        _timer = 0f;
        _listeningDuration = 0f;
        StartListening();
    }

    void StartListening()
    {
        if (_isListening) return;
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("[Mic] No Microphone found!");
            return;
        }

        _microphoneDevice = Microphone.devices[0];
        micClip = Microphone.Start(_microphoneDevice, true, 10, SAMPLE_RATE);
        _isListening = true;
        Debug.Log($"[Mic] Menunggu ucapan: '{_pendingSpellName}'...");
    }

    public void StopListening()
    {
        if (!_isListening) return;
        Microphone.End(_microphoneDevice);
        _isListening = false;
        _pendingSpellName = null;
        Debug.Log("[Mic] Stopped.");
    }

    void Update()
    {
        if (!_isListening || interpreter == null || string.IsNullOrEmpty(_pendingSpellName)) return;

        // Timeout Logic
        _listeningDuration += Time.deltaTime;
        if (_listeningDuration > recordingTimeout)
        {
            Debug.Log("[Time Out] Tidak ada suara terdeteksi.");
            if (spellManager != null) spellManager.ResetSelection();
            StopListening();
            return;
        }

        // Detection Interval
        _timer += Time.deltaTime;
        if (_timer < detectionInterval) return;
        _timer = 0f;

        ProcessAudio();
    }

    void ProcessAudio()
    {
        int pos = Microphone.GetPosition(_microphoneDevice);
        if (pos < inputBuffer.Length) return;

        micClip.GetData(inputBuffer, pos - inputBuffer.Length);

        interpreter.SetInputTensorData(0, inputBuffer);
        interpreter.Invoke();
        interpreter.GetOutputTensorData(0, outputBuffer);

        int maxIndex = 0;
        float maxScore = 0f;
        for (int i = 0; i < outputBuffer.Length; i++)
        {
            if (outputBuffer[i] > maxScore)
            {
                maxScore = outputBuffer[i];
                maxIndex = i;
            }
        }

        if (maxScore > threshold)
        {
            string detectedClass = labels[maxIndex];

            // Cek Noise
            bool isNoise = detectedClass.Equals("Background Noise", System.StringComparison.OrdinalIgnoreCase) ||
                           detectedClass.Equals("_background_noise_", System.StringComparison.OrdinalIgnoreCase);

            if (isNoise) return;

            HandlePrediction(detectedClass);
        }
    }

    void HandlePrediction(string detectedClass)
    {
        string detected = detectedClass.ToLower();
        string target = _pendingSpellName.ToLower();

        if (detected == target)
        {
            Debug.Log($"[SUCCESS] Mantra Benar: {detectedClass}");
            if (spellManager != null) spellManager.CastSpellWithAksara(_pendingSpellName);
            StopListening();
        }
        else
        {
            Debug.Log($"[FAIL] Salah Mantra! Target: {target}, Terdeteksi: {detected}");
            if (spellManager != null) spellManager.ResetSelection();
            StopListening();
        }
    }

    void OnDestroy()
    {
        StopListening();
        interpreter?.Dispose();
    }
}