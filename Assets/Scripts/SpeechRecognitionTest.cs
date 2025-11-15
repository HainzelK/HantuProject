using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SpeechRecognitionTest : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private TextMeshProUGUI text;

    private AudioClip clip;
    private byte[] bytes;
    private bool recording;

    // URL API tetap bisa di-hardcode karena tidak sensitif
    private const string API_URL = "https://router.huggingface.co/hf-inference/models/openai/whisper-large-v3";

    // Variabel untuk menyimpan token dari .env
    private string hfToken;

    // Gunakan Awake() untuk memastikan .env dimuat sebelum Start()
    private void Awake()
    {
        // Muat semua variabel dari file .env
        DotEnv.Load();
        // Ambil token dan simpan di variabel
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
            // Gunakan variabel hfToken yang sudah dimuat dari .env
            request.SetRequestHeader("Authorization", $"Bearer {hfToken}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                SpeechRecognitionResponse response = JsonUtility.FromJson<SpeechRecognitionResponse>(jsonResponse);

                text.color = Color.white;
                text.text = response.text;
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