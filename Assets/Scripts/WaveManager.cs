using UnityEngine;
using TMPro;
using System.Collections;

public class WaveManager : MonoBehaviour
{

        [HideInInspector]
    public WaveManager waveManager;

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
        if (waveManager == null)
        Debug.LogError("WaveManager reference missing on CubeTracker!");
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

    Vector3 spawnPos = playerTarget.position +
                       dir.normalized * spawnDistance +
                       new Vector3(0, heightOffset, 0);

    // Spawn cube
    GameObject cube = Instantiate(cubePrefab, spawnPos, Quaternion.identity);

    // Assign CubeMover target
    CubeMover mover = cube.GetComponent<CubeMover>();
    if (mover != null)
        mover.target = playerTarget;

    // ✅ Ensure CubeTracker exists and assign WaveManager
    CubeTracker tracker = cube.GetComponent<CubeTracker>();
    if (tracker == null)
        tracker = cube.AddComponent<CubeTracker>();

    tracker.waveManager = this;

    // Debug check
    Debug.Log($"Spawned cube: {cube.name}, CubeTracker exists? {tracker != null}, WaveManager assigned? {tracker.waveManager != null}");
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
