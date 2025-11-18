using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

[System.Serializable]
public class Spell
{
    [Tooltip("Nama utama mantra ini (untuk logika game Anda).")]
    public string spellName;
    [Tooltip("Daftar semua kemungkinan cara Whisper akan menulis mantra ini (termasuk bahasa lain).")]
    public List<string> variations;
}

public class SpeechRecognitionTest : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private TextMeshProUGUI text;

    [Header("Pengaturan Mantra")]
    [SerializeField] private Spell[] spells;
    [Tooltip("Seberapa toleran pencocokan kata? 1 atau 2 direkomendasikan.")]
    [SerializeField] private int similarityThreshold = 2;

    private AudioClip clip;
    private byte[] bytes;
    private bool recording;

    private const string API_URL = "https://router.huggingface.co/hf-inference/models/openai/whisper-large-v3";
    private string hfToken;

    private IEnumerator SendRecordingWithCustomHeader()
    {
        text.color = Color.yellow;
        text.text = "Sending...";
        stopButton.interactable = false;

        using (var request = new UnityWebRequest(API_URL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bytes);
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
                text.color = Color.red;
                text.text = $"Error: {request.responseCode} - {request.downloadHandler.text}";
                Debug.LogError($"Error sending request: {request.error} | Response: {request.downloadHandler.text}");
            }

            startButton.interactable = true;
        }
    }

    private void CheckForKeywords(string transcribedText)
    {
        if (string.IsNullOrWhiteSpace(transcribedText))
        {
            text.color = Color.white;
            text.text = "(Tidak ada ucapan terdeteksi)";
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
            text.text = $"Terdeteksi: {overallBestSpell.spellName} (Hasil asli: {transcribedText})";
            OnKeywordDetected(overallBestSpell);
        }
        else
        {
            text.color = Color.white;
            text.text = transcribedText;
        }
    }
    private void Awake()
    {
        DotEnv.Load();
        hfToken = DotEnv.Get("HF_TOKEN");

        if (string.IsNullOrEmpty(hfToken))
        {
            Debug.LogError("Hugging Face token (HF_TOKEN) not found in .env file.");
        }
    }

    private void Start()
    {
        startButton.onClick.AddListener(StartRecording);
        stopButton.onClick.AddListener(StopRecording);
        stopButton.interactable = false;
    }

    private void Update()
    {
        if (recording && Microphone.GetPosition(null) >= clip.samples)
        {
            StopRecording();
        }
    }

    private void StartRecording()
    {
        text.color = Color.white;
        text.text = "Recording...";
        startButton.interactable = false;
        stopButton.interactable = true;
        clip = Microphone.Start(null, false, 10, 44100);
        recording = true;
    }

    private void StopRecording()
    {
        var position = Microphone.GetPosition(null);
        Microphone.End(null);
        var samples = new float[position * clip.channels];
        clip.GetData(samples, 0);
        bytes = EncodeAsWAV(samples, clip.frequency, clip.channels);
        recording = false;

        StartCoroutine(SendRecordingWithCustomHeader());
    }

    [System.Serializable]
    private class SpeechRecognitionResponse
    {
        public string text;
    }

    private void OnKeywordDetected(Spell detectedSpell)
    {
        Debug.Log($"MANTRA '{detectedSpell.spellName}' TERDETEKSI! Menjalankan aksi...");

        switch (detectedSpell.spellName)
        {
            case "Lette":
                // Lempar listrik
                break;
            case "Api":
                // Lempar api
                break;
            case "uae":
                // Lempar air
                break;
            default:
                Debug.LogWarning("Mantra tidak dikenali untuk aksi.");
                break;
        }

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
}