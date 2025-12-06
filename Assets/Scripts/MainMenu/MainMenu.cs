using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class MainMenu : MonoBehaviour
{
    readonly string[] modelFiles = new string[]
    {
        "sherpa-onnx/models/speech-recognition/sherpa-onnx-zipformer-korean-2024-06-24/bpe.model",
        "sherpa-onnx/models/speech-recognition/sherpa-onnx-zipformer-korean-2024-06-24/decoder-epoch-99-avg-1.int8.onnx",
        "sherpa-onnx/models/speech-recognition/sherpa-onnx-zipformer-korean-2024-06-24/decoder-epoch-99-avg-1.onnx",
        "sherpa-onnx/models/speech-recognition/sherpa-onnx-zipformer-korean-2024-06-24/encoder-epoch-99-avg-1.int8.onnx",
        "sherpa-onnx/models/speech-recognition/sherpa-onnx-zipformer-korean-2024-06-24/encoder-epoch-99-avg-1.onnx",
        "sherpa-onnx/models/speech-recognition/sherpa-onnx-zipformer-korean-2024-06-24/joiner-epoch-99-avg-1.int8.onnx",
        "sherpa-onnx/models/speech-recognition/sherpa-onnx-zipformer-korean-2024-06-24/joiner-epoch-99-avg-1.onnx",
        "sherpa-onnx/models/speech-recognition/sherpa-onnx-zipformer-korean-2024-06-24/tokens.txt",
        "sherpa-onnx/models/speech-recognition/sherpa-onnx-zipformer-korean-2024-06-24/test_wavs/0.wav",
        "sherpa-onnx/models/speech-recognition/sherpa-onnx-zipformer-korean-2024-06-24/test_wavs/1.wav",
        "sherpa-onnx/models/speech-recognition/sherpa-onnx-zipformer-korean-2024-06-24/test_wavs/3.wav",
        "sherpa-onnx/models/speech-recognition/sherpa-onnx-zipformer-korean-2024-06-24/test_wavs/4.wav",
        "sherpa-onnx/models/speech-recognition/sherpa-onnx-zipformer-korean-2024-06-24/test_wavs/trans.txt",
        "sherpa-onnx/models/voice-activity-detection/ten-vad/ten-vad.onnx",
    };

    bool copyDone = false;

    IEnumerator Start()
    {
        yield return StartCoroutine(CopyAllModels());
        copyDone = true;
        Debug.Log("SEMUA FILE MODEL SIAP!");
    }

    public void StartGame()
    {
        if (!copyDone)
        {
            Debug.LogWarning("Copy file model belum selesai! Tunggu sebentar...");
            return;
        }

        SceneManager.LoadScene("tesSFX");
    }

    IEnumerator CopyAllModels()
    {
        foreach (var relativePath in modelFiles)
        {
            yield return CopySingleFile(relativePath);
        }
    }

    IEnumerator CopySingleFile(string relativePath)
    {
        string dst = Path.Combine(Application.persistentDataPath, relativePath);

        // PERBAIKAN 3: Cek jika file sudah ada, skip copy (biar loading cepat)
        if (File.Exists(dst))
        {
            // Debug.Log("File sudah ada, skip: " + relativePath);
            yield break;
        }

        // Buat direktori tujuan jika belum ada
        string dir = Path.GetDirectoryName(dst);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string src = Path.Combine(Application.streamingAssetsPath, relativePath);

#if UNITY_ANDROID && !UNITY_EDITOR
        // Pada Android, src akan berformat jar:file://...
        using (var www = new UnityWebRequest(src, UnityWebRequest.kHttpVerbGET))
        {
            // PERBAIKAN 2: Gunakan DownloadHandlerFile untuk hemat RAM
            // Ini langsung menulis ke harddisk/storage tanpa memuat ke RAM
            www.downloadHandler = new DownloadHandlerFile(dst);
            
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"GAGAL COPY: {relativePath}\nError: {www.error}");
                // Hapus file corrupt/setengah jalan jika gagal
                if(File.Exists(dst)) File.Delete(dst); 
            }
            else
            {
                Debug.Log("COPIED (Android) -> " + relativePath);
            }
        }
#else
        // Untuk Editor / PC / iOS
        if (!File.Exists(src))
        {
            Debug.LogError("FILE SUMBER TIDAK DITEMUKAN: " + src);
            yield break;
        }

        File.Copy(src, dst, true);
        Debug.Log("COPIED (PC/Editor) -> " + relativePath);
#endif
    }
}