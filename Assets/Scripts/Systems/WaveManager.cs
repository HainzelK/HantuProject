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
    public TMP_Text killText;

    public EnemyIndicatorManager enemyIndicatorManager; // Assign in Inspector
    public SpellManager spellManager;

    public float spawnDistance = 2.5f;
    public float heightOffset = 0f;
    public float spawnInterval = 3f;

    [Header("Enemy HP")]
    public float baseEnemyHp = 100f;
    public float hpPerWave = 30f;

    // ✅ NEW: How many kills needed to complete a wave?
    public int killsPerWave = 5;

    private bool isWaveActive = false;
    private int waveNumber = 1;
    private int currentWaveKills = 0;

    private Coroutine spawnCoroutine;

    void Start()
    {
        Debug.Log("WaveManager STARTED");
        StartWave();
    }

    void StartWave()
    {
        if (!this.isActiveAndEnabled) return;

        // Stop any previous wave
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        Debug.Log($"\n=== STARTING WAVE {waveNumber} (Target: {killsPerWave} kills) ===");
        currentWaveKills = 0;
        isWaveActive = true;

        waveText.text = "Wave " + waveNumber;
        UpdateKillText();

        // Start infinite spawning
        spawnCoroutine = StartCoroutine(SpawnWaveRoutine());
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

        float angle = Random.Range(0f, 360f);
        Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
        Vector3 spawnPos = playerTarget.position + dir * spawnDistance + Vector3.up * heightOffset;

        GameObject cube = Instantiate(cubePrefab, spawnPos, Quaternion.identity);
        cube.name = $"Enemy_W{waveNumber}_T{Time.frameCount}";

        // Setup CubeMover
        CubeMover mover = cube.GetComponent<CubeMover>();
        if (mover != null)
        {
            mover.target = playerTarget;
        }
        else
        {
            Debug.LogWarning($"[Spawn] {cube.name} missing CubeMover!", cube);
        }

        // Setup CubeTracker
        CubeTracker tracker = cube.GetComponent<CubeTracker>();
        if (tracker != null)
        {
            tracker.Initialize(this);
        }
        else
        {
            Debug.LogError($"[Spawn] {cube.name} missing CubeTracker! Add to prefab.", cube);
        }

        // ✅ Setup EnemyHealth with wave-based HP

        EnemyHealth enemyHealth = cube.GetComponent<EnemyHealth>();

        enemyHealth.maxHealth = baseEnemyHp + (waveNumber - 1) * hpPerWave;
        if (enemyHealth != null)
        {
            // 🔥 Set HP based on wave number (example: +30 HP per wave)
            enemyHealth.maxHealth = 100f + (waveNumber - 1) * 30f;
            // HP will be initialized in EnemyHealth.Start()
        }
        else
        {
            Debug.LogWarning($"[Spawn] {cube.name} missing EnemyHealth component!", cube);
        }

        // ✅ Register with EnemyIndicatorManager (if available)
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
        if (!isWaveActive) return; // Ignore kills between waves

        currentWaveKills++;
        UpdateKillText();

        Debug.Log($"Kill registered! Current wave kills: {currentWaveKills}/{killsPerWave}");

        // ✅ Check if wave prerequisite is met: enough kills?
        if (currentWaveKills >= killsPerWave)
        {
            CompleteWave();
        }
    }

    void CompleteWave()
    {
        isWaveActive = false;
        Debug.Log($"\n=== WAVE {waveNumber} COMPLETED ===");

        // ✅ Unlock Spell 3 ONLY after Wave 1
        if (waveNumber == 1)
        {
            Debug.Log("Wave 1 complete — attempting to unlock Api");
            if (spellManager != null)
            {
                Debug.Log("SpellManager found — calling UnlockSpell");
                spellManager.UnlockSpell("Api");
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