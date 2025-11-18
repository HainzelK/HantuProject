using UnityEngine;
using TMPro;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text waveText;
    public TMP_Text killText;

    [Header("Spawning")]
    public GameObject cubePrefab;
    public Transform playerTarget;
    public float spawnDistance = 2.5f;
    public float spawnInterval = 3f;

    private int waveNumber = 1;
    private int cubesToSpawn;
    private int killsThisWave;

    void Start()
    {
        StartWave();
    }

    void StartWave()
    {
        killsThisWave = 0;
        cubesToSpawn = 2 + (waveNumber - 1); // Wave 1 = 2 cubes, +1 each wave

        waveText.text = $"Wave {waveNumber}";
        killText.text = $"Kills: {killsThisWave} / {cubesToSpawn}";

        Debug.Log($"=== STARTING WAVE {waveNumber} ===");
        Debug.Log($"Cubes to spawn this wave: {cubesToSpawn}");

        StartCoroutine(SpawnWaveRoutine());
    }

    IEnumerator SpawnWaveRoutine()
    {
        for (int i = 0; i < cubesToSpawn; i++)
        {
            SpawnCube();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

void SpawnCube()
{
    float angle = Random.Range(0f, 360f);
    Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
    Vector3 spawnPos = playerTarget.position + dir * spawnDistance;

    GameObject cube = Instantiate(cubePrefab, spawnPos, Quaternion.identity);

    // Ensure CubeTracker exists
    CubeTracker tracker = cube.GetComponent<CubeTracker>();
    if (tracker == null)
    {
        tracker = cube.AddComponent<CubeTracker>();
        Debug.LogWarning("CubeTracker added dynamically to prefab!");
    }

    // ✅ Assign the WaveManager to the spawned cube
    tracker.waveManager = this;

    // Assign the target for movement
    CubeMover mover = cube.GetComponent<CubeMover>();
    if (mover != null) mover.target = playerTarget;

    Debug.Log($"Spawned cube at {spawnPos}");
}

    // Called only when a projectile kills a cube
    public void OnCubeKilledByPlayer()
    {
        killsThisWave++;
        killText.text = $"Kills: {killsThisWave} / {cubesToSpawn}";
        Debug.Log($"KILL REGISTERED ({killsThisWave}/{cubesToSpawn})");

        if (killsThisWave >= cubesToSpawn)
        {
            Debug.Log($"=== WAVE {waveNumber} COMPLETE ===");
            waveNumber++;
            Invoke(nameof(StartWave), 1.5f);
        }
    }
}
