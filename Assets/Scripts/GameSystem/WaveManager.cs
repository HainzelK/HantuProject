using UnityEngine;
using TMPro;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    public TextMeshProUGUI waveText;   // TMP text
    public TextMeshProUGUI killsText;  // TMP text

    private int wave = 0;
    private int totalCubes = 0;
    private int killed = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartNextWave();
    }

    public void StartNextWave()
    {
        wave++;
        totalCubes = wave + 1;
        killed = 0;

        waveText.text = $"Wave {wave}";
        killsText.text = $"0 / {totalCubes}";
    }

    public void CubeKilled()
    {
        killed++;
        killsText.text = $"{killed} / {totalCubes}";

        if (killed >= totalCubes)
            StartNextWave();
    }
}
