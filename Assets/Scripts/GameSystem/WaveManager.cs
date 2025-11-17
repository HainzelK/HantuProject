using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    [Header("Spawning")]
    public GameObject cubePrefab;
    public Camera arCamera;                 // The AR camera / main player camera
    public float spawnDistance = 3f;         // Radius around the player
    public float spawnInterval = 4f;

    [Header("Wave Settings")]
    public int startingSpawnCount = 5;
    public int spawnIncreasePerWave = 5;

    [Header("UI")]
    public TMPro.TextMeshProUGUI waveText;
    public TMPro.TextMeshProUGUI killText;

    private int currentWave = 0;
    private int cubesToSpawnThisWave;
    private int cubesKilledThisWave;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (arCamera == null)
            arCamera = Camera.main;

        StartCoroutine(WaveLoop());
    }

    public void ReportCubeKilled()
    {
        cubesKilledThisWave++;
        killText.text = "Kills: " + cubesKilledThisWave + "/" + cubesToSpawnThisWave;
    }

    private IEnumerator WaveLoop()
    {
        while (true)
        {
            currentWave++;
            waveText.text = "Wave: " + currentWave;

            cubesToSpawnThisWave = startingSpawnCount + (spawnIncreasePerWave * (currentWave - 1));
            cubesKilledThisWave = 0;

            killText.text = "Kills: 0/" + cubesToSpawnThisWave;

            // Spawn enemies for this wave
            yield return StartCoroutine(SpawnWave());

            // Wait until all cubes are killed
            while (cubesKilledThisWave < cubesToSpawnThisWave)
                yield return null;

            // Short delay before next wave
            yield return new WaitForSeconds(2f);
        }
    }

    private IEnumerator SpawnWave()
    {
        for (int i = 0; i < cubesToSpawnThisWave; i++)
        {
            SpawnCubeAroundPlayer();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnCubeAroundPlayer()
    {
        if (arCamera == null) return;

        // Pick a random angle around the player (360°)
        float angle = Random.Range(0f, 360f);

        // Direction on XZ plane
        Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

        // Spawn position in a circle around the camera
        Vector3 spawnPos = arCamera.transform.position + dir.normalized * spawnDistance;
        spawnPos.y = arCamera.transform.position.y;   // same height as player

        GameObject cube = Instantiate(cubePrefab, spawnPos, Quaternion.identity);

        // Assign the player as the target
        CubeMover mover = cube.GetComponent<CubeMover>();
        if (mover != null)
            mover.SetTarget(arCamera.transform);
    }
}
