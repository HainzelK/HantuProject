using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

public class ARRandomSpawner : MonoBehaviour
{
    [Header("References")]
    public XROrigin xrOrigin;
    public Camera arCamera;
    public GameObject cubePrefab;

    [Header("Spawn Settings")]
    public float spawnDistance = 2.5f;
    public float minAngle = 0f;
    public float maxAngle = 360f;
    public float spawnInterval = 10f;

    [Header("Dynamic Origin Settings")]
    public bool useAccelerometer = true;
    public float accelFactor = 0.5f;

    private Transform trackables;
    private float spawnTimer = 0f;
    private bool initialized = false;

    void Start()
    {
        if (xrOrigin == null || arCamera == null || cubePrefab == null)
        {
            Debug.LogError("Assign XR Origin, AR Camera, and Cube Prefab.");
            return;
        }

        trackables = xrOrigin.TrackablesParent;
        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;

        UpdateWorldOrigin();
        UpdateSpawnTimer();
    }

    void UpdateWorldOrigin()
    {
        // Keep AR camera as (0,0,0)
        Vector3 offset = -arCamera.transform.position;
        trackables.position += offset;

        // Optional accelerometer-based drift
        if (useAccelerometer)
        {
            Vector3 accel = Input.acceleration;
            Vector3 accelOffset = new Vector3(accel.x, 0f, accel.y) * accelFactor;
            trackables.position += accelOffset * Time.deltaTime;
        }
    }

    void UpdateSpawnTimer()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnRandomCube();
        }
    }

    void SpawnRandomCube()
    {
        // Random direction around player
        float angle = Random.Range(minAngle, maxAngle);
        Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

        // Compute spawn position relative to camera
        Vector3 spawnPos = dir.normalized * spawnDistance;

        // Spawn cube
        GameObject cube = Instantiate(cubePrefab, spawnPos, Quaternion.identity);
        CubeMover mover = cube.GetComponent<CubeMover>();
        if (mover == null) mover = cube.AddComponent<CubeMover>();
        mover.target = arCamera.transform;
    }
}
