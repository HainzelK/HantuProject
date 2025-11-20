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

    public SpellManager spellManager;

    public float spawnDistance = 2.5f;
    public float heightOffset = 0f;
    public float spawnInterval = 3f;

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

        CubeMover mover = cube.GetComponent<CubeMover>();
        if (mover != null) mover.target = playerTarget;

        CubeTracker tracker = cube.GetComponent<CubeTracker>();
        if (tracker != null)
        {
            tracker.Initialize(this);
        }
        else
        {
            Debug.LogError($"{cube.name} — CubeTracker MISSING on prefab!");
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
        Debug.Log("Wave 1 complete — attempting to unlock Spell 3");
        if (spellManager != null)
        {
            Debug.Log("SpellManager found — calling UnlockSpell");
            spellManager.UnlockSpell("Spell 3");
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