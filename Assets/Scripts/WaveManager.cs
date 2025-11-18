using UnityEngine;
using TMPro;
using System.Collections;

public class WaveManager : MonoBehaviour
{


    [HideInInspector]
    public bool killedByProjectile = false;
    public GameObject cubePrefab;
    public Transform playerTarget;
    public TMP_Text waveText;
    public TMP_Text killText; // ✔ Add reference to the kill counter text

    public float spawnDistance = 2.5f;
    public float heightOffset = 0f;
    public float spawnInterval = 3f;

    private int waveNumber = 1;
    private int cubesToSpawn;
    private int aliveCubes;
    public int killCount = 0;

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
        killCount = 0; // reset kills for new wave

        waveText.text = "Wave " + waveNumber;
        UpdateKillText();

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
    Vector3 spawnPos = playerTarget.position + dir * spawnDistance + Vector3.up * heightOffset;

    GameObject cube = Instantiate(cubePrefab, spawnPos, Quaternion.identity);

    // Add and configure CubeMover
    CubeMover mover = cube.GetComponent<CubeMover>();
    if (mover != null)
        mover.target = playerTarget;

    // 🔥 Always add FRESH CubeTracker and assign IMMEDIATELY
    CubeTracker tracker = cube.AddComponent<CubeTracker>();
    tracker.waveManager = this;
    tracker.killedByProjectile = false;

    // Optional: force initialization
    cube.SetActive(false);
    cube.SetActive(true); // ensures Awake/OnEnable runs AFTER assignment

    Debug.Log($"[Spawn] Cube {cube.name} spawned with WaveManager: {(tracker.waveManager != null ? "YES" : "NO")}");
}

    // Only called when a cube is killed by a projectile
public void RegisterKill()
{
    killCount++;
    aliveCubes--;

    Debug.Log($"Cube killed by projectile! Kills: {killCount}/{cubesToSpawn}");
    UpdateKillText();

    if (aliveCubes <= 0)
    {
        Debug.Log($"\n=== WAVE {waveNumber} COMPLETED ===");
        waveNumber++;
        Invoke(nameof(StartWave), 1f);
    }
}


    void UpdateKillText()
    {
        if (killText != null)
        {
            killText.text = "Kills: " + killCount;
        }
    }
}
