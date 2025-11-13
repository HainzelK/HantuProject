using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using System.Collections.Generic;

public class ARRandomSpawner : MonoBehaviour
{
    [Header("References")]
    public XROrigin xrOrigin;
    public Camera arCamera;
    public GameObject cubePrefab;
    public GameObject linePrefab; // optional prefab for line, or create dynamically

    [Header("Spawn Settings")]
    public float spawnDistance = 2.5f;
    public float minAngle = 0f;
    public float maxAngle = 360f;
    public float spawnInterval = 10f;
    public int maxLines = 5;

    [Header("Dynamic Origin Settings")]
    public bool useAccelerometer = true;
    public float accelFactor = 0.5f;

    private Transform trackables;
    private List<LineRenderer> activeLines = new List<LineRenderer>();
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
        // Always keep camera as new (0,0,0)
        Vector3 offset = -arCamera.transform.position;
        trackables.position += offset;

        // Optional accelerometer-based world shift
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

            // Limit to max lines
            if (activeLines.Count < maxLines)
            {
                CreateRandomLine();
            }
        }
    }

    void CreateRandomLine()
    {
        // Create new GameObject for the line
        GameObject lineObj = new GameObject("SpawnLine");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.startWidth = 0.01f;
        lr.endWidth = 0.01f;

        // Random direction
        float angle = Random.Range(minAngle, maxAngle);
        Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
        Vector3 start = Vector3.zero;
        Vector3 end = dir.normalized * spawnDistance;

        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        activeLines.Add(lr);

        // Spawn cube at the *end* of line after short delay
        StartCoroutine(SpawnCubeAndRemoveLine(lr, end));
    }

    System.Collections.IEnumerator SpawnCubeAndRemoveLine(LineRenderer lr, Vector3 endPoint)
    {
        yield return new WaitForSeconds(0.1f);

        // Spawn cube at the end of the line, facing the player
        GameObject cube = Instantiate(cubePrefab, endPoint, Quaternion.identity);
        CubeMover mover = cube.GetComponent<CubeMover>();
        if (mover == null) mover = cube.AddComponent<CubeMover>();
        mover.target = arCamera.transform;

        // Remove line right after spawning
        activeLines.Remove(lr);
        Destroy(lr.gameObject, 0.1f);
    }
}
