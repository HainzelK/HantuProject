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

    private bool isSpawning = false; 
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
    // Safety: if this object is disabled or destroyed, don't spawn
    if (!this.isActiveAndEnabled)
    {
        Debug.LogError("WaveManager is disabled or destroyed! Cannot start wave.");
        return;
    }

    if (isSpawning)
    {
        Debug.LogWarning("StartWave called while already spawning! Skipping.");
        return;
    }

    isSpawning = true;

    Debug.Log($"\n=== STARTING WAVE {waveNumber} ===");
    cubesToSpawn = 2 + (waveNumber - 1);
    aliveCubes = cubesToSpawn;
    killCount = 0;

    waveText.text = "Wave " + waveNumber;
    UpdateKillText();

    StartCoroutine(SpawnWaveRoutine());
}

IEnumerator SpawnWaveRoutine()
{
    for (int i = 0; i < cubesToSpawn; i++)
    {
        SpawnCube360();
        yield return new WaitForSeconds(spawnInterval);
    }

    // Keep isSpawning = true until wave is cleared by kills
    // We'll clear it in RegisterKill when wave ends
}
private int _spawnCounter = 0;

void SpawnCube360()
{
    if (playerTarget == null) return;

    _spawnCounter++;
    int myId = _spawnCounter;

    float angle = Random.Range(0f, 360f);
    Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
    Vector3 spawnPos = playerTarget.position + dir * spawnDistance + Vector3.up * heightOffset;

    GameObject cube = Instantiate(cubePrefab, spawnPos, Quaternion.identity);
    cube.name = $"Enemy_{myId}_Wave{waveNumber}";

    CubeMover mover = cube.GetComponent<CubeMover>();
    if (mover != null) mover.target = playerTarget;

    CubeTracker tracker = cube.GetComponent<CubeTracker>();
    if (tracker != null)
    {
        tracker.Initialize(this);
        Debug.Log($"[SPAWN OK] {cube.name} | WaveManager ID: {this.GetInstanceID()}");
    }
    else
    {
        Debug.LogError($"[SPAWN FAIL] {cube.name} — CubeTracker MISSING on prefab!");
    }
}

    // Only called when a cube is killed by a projectile
public void RegisterKill()
{
    killCount++;
    aliveCubes--;
    UpdateKillText();

    if (aliveCubes <= 0)
    {
        Debug.Log($"\n=== WAVE {waveNumber} COMPLETED ===");
        waveNumber++;
        isSpawning = false;
        StartCoroutine(DelayedStartWave(1f));
    }
}

IEnumerator DelayedStartWave(float delay)
{
    yield return new WaitForSeconds(delay);
    if (this.isActiveAndEnabled)
    {
        StartWave();
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
