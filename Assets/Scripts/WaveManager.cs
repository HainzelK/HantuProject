using UnityEngine;
using TMPro;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    public GameObject cubePrefab;
    public Transform playerTarget;
    public TMP_Text waveText;

    public float spawnDistance = 2.5f;
    public float heightOffset = 0f;
    public float spawnInterval = 3f;

    private int waveNumber = 1;
    private int cubesToSpawn;
    private int aliveCubes;
    public int killCount = 0; // ✔ track real kills

    void Start()
    {
        Debug.Log("WaveManager STARTED");
        StartWave();
    }

    void StartWave()
    {
        Debug.Log($"\n=== STARTING WAVE {waveNumber} ===");

        cubesToSpawn = 2 + (waveNumber - 1);
        aliveCubes = cubesToSpawn;
        killCount = 0; // ✔ reset kills for new wave

        waveText.text = "Wave " + waveNumber;
        Debug.Log("Cubes to spawn: " + cubesToSpawn);

        StartCoroutine(SpawnWaveRoutine());
    }

    IEnumerator SpawnWaveRoutine()
    {
        for (int i = 0; i < cubesToSpawn; i++)
        {
            SpawnCube360();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnCube360()
    {
        float angle = Random.Range(0f, 360f);
        Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

        Vector3 spawnPos = playerTarget.position +
                           dir.normalized * spawnDistance +
                           new Vector3(0, heightOffset, 0);

        Debug.Log($"Spawning cube at angle {angle}°, position {spawnPos}");

        GameObject cube = Instantiate(cubePrefab, spawnPos, Quaternion.identity);

        CubeMover mover = cube.GetComponent<CubeMover>();
        mover.target = playerTarget;

        CubeTracker tracker = cube.AddComponent<CubeTracker>();
        tracker.waveManager = this;
    }

    // ❌ NO LONGER CALLED ON CUBE DESTROY
    // We only use it if it’s a real kill
    public void RegisterKill()
    {
        killCount++;
        aliveCubes--;

        Debug.Log($"Cube killed by projectile! Kills: {killCount}/{cubesToSpawn}");

        if (aliveCubes <= 0)
        {
            Debug.Log($"\n=== WAVE {waveNumber} COMPLETED ===");
            waveNumber++;
            Invoke(nameof(StartWave), 1f);
        }
    }
}
