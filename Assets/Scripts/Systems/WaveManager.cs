using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    // 🔥 Enemy prefabs and their speeds
    public List<GameObject> enemyPrefabs;      // Assign in Inspector: [Enemy1, Enemy2, ...]
    public List<float> enemyBaseSpeeds;        // Match index: [0.8f, 0.4f, ...]
    public List<float> enemyAccelerations;     // Optional: [0.5f, 0.2f, ...]

    public Transform playerTarget;
    public TMP_Text waveText;
    public TMP_Text killText;

    public EnemyIndicatorManager enemyIndicatorManager;
    public SpellManager spellManager;

    public float spawnDistance = 2.5f;
    public float heightOffset = 0f;
    public float spawnInterval = 3f;

    [Header("Enemy HP")]
    public float baseEnemyHp = 100f;
    public float hpPerWave = 30f;
    public int killsPerWave = 5;

    private bool isWaveActive = false;
    public int waveNumber = 1;
    private int currentWaveKills = 0;
    private Coroutine spawnCoroutine;

    void Start()
    {
        StartWave();
    }

    void StartWave()
    {
        if (!this.isActiveAndEnabled) return;
        
        // Validate lists
        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            Debug.LogError("No enemy prefabs assigned!");
            return;
        }
        
        // Auto-fill speed/acceleration lists if missing
        EnsureListsMatch();

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        Debug.Log($"\n=== STARTING WAVE {waveNumber} (Target: {killsPerWave} kills) ===");
        currentWaveKills = 0;
        isWaveActive = true;

        waveText.text = "Wave " + waveNumber;
        UpdateKillText();

        spawnCoroutine = StartCoroutine(SpawnWaveRoutine());
    }

    // 🔥 Ensure speed/acceleration lists match prefab count
    void EnsureListsMatch()
    {
        while (enemyBaseSpeeds.Count < enemyPrefabs.Count)
            enemyBaseSpeeds.Add(0.8f); // Default speed
        
        while (enemyAccelerations.Count < enemyPrefabs.Count)
            enemyAccelerations.Add(0.5f); // Default acceleration
    }

    IEnumerator SpawnWaveRoutine()
    {
        while (isWaveActive)
        {
            SpawnCube360();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

void SpawnCube360()
{
    if (playerTarget == null) return;

    int prefabIndex = (waveNumber - 1) % enemyPrefabs.Count;
    GameObject prefabToSpawn = enemyPrefabs[prefabIndex];

    float angle = Random.Range(0f, 360f);
    Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
    Vector3 spawnPos = playerTarget.position + dir * spawnDistance + Vector3.up * heightOffset;

    GameObject cube = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
    cube.name = $"Enemy_W{waveNumber}_T{Time.frameCount}";

    // 🔥 Apply custom speed & acceleration WITH PROPER RESET
    CubeMover mover = cube.GetComponent<CubeMover>();
    if (mover != null)
    {
        mover.target = playerTarget;
        
        // ✅ SET SPEEDS AND RESET CURRENT SPEED
        mover.baseSpeed = enemyBaseSpeeds[prefabIndex];
        mover.acceleration = enemyAccelerations[prefabIndex];
        mover.currentSpeed = mover.baseSpeed; // ← CRITICAL: Reset current speed
        
        // Optional: Reset movement state
        mover.canMove = true;
        mover.reachedPlayer = false;
        mover.attackAnimationPlayed = false;
    }
    else
    {
        Debug.LogWarning($"[Spawn] {cube.name} missing CubeMover!", cube);
    }

    CubeTracker tracker = cube.GetComponent<CubeTracker>();
    if (tracker != null)
    {
        tracker.Initialize(this);
    }
    else
    {
        Debug.LogError($"[Spawn] {cube.name} missing CubeTracker! Add to prefab.", cube);
    }

    EnemyHealth enemyHealth = cube.GetComponent<EnemyHealth>();
    if (enemyHealth != null)
    {
        float waveHP = baseEnemyHp + (waveNumber - 1) * hpPerWave;
        enemyHealth.maxHealth = waveHP;
    }
    else
    {
        Debug.LogWarning($"[Spawn] {cube.name} missing EnemyHealth component!", cube);
    }

    if (enemyIndicatorManager != null)
    {
        enemyIndicatorManager.RegisterEnemy(cube);
    }
    else if (enemyIndicatorManager == null && waveNumber == 1)
    {
        Debug.LogWarning("[Spawn] EnemyIndicatorManager not assigned! Indicators disabled.");
    }
}

    public void RegisterKill()
    {
        if (!isWaveActive) return;
        currentWaveKills++;
        UpdateKillText();

        if (currentWaveKills >= killsPerWave)
        {
            CompleteWave();
        }
    }

    void CompleteWave()
    {
        isWaveActive = false;
        Debug.Log($"\n=== WAVE {waveNumber} COMPLETED ===");

        if (waveNumber == 1)
        {
            Debug.Log("Wave 1 complete — attempting to unlock Api");
            if (spellManager != null)
            {
                Debug.Log("SpellManager found — calling UnlockSpell");
                spellManager.UnlockSpell("api");
            }
            else
            {
                Debug.LogError("SpellManager is NULL! Did you assign it in Inspector?");
            }
        }

        waveNumber++;
        StartCoroutine(DelayedStartWave(1f));
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
            killText.text = $"Kills: {currentWaveKills}/{killsPerWave}";
        }
    }
}