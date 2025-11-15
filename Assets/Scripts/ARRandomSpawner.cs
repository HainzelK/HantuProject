using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

public class ARRandomSpawner : MonoBehaviour
{
    [Header("References")]
    public XROrigin xrOrigin;
    public Camera arCamera;
    public GameObject cubePrefab;
    public Transform virtualPlayer;   // NEW

    [Header("Spawn Settings")]
    public float spawnDistance = 2.5f;
    public float minAngle = 0f;
    public float maxAngle = 360f;
    public float spawnInterval = 10f;

    private float spawnTimer = 0f;
    private bool initialized = false;

    void Start()
    {
        if (xrOrigin == null || arCamera == null || cubePrefab == null)
        {
            Debug.LogError("Assign XR Origin, AR Camera, Cube Prefab, VirtualPlayer.");
            return;
        }

        if (virtualPlayer == null)
        {
            Debug.LogError("Assign the VirtualPlayer transform.");
            return;
        }

        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnRandomCube();
        }
    }

    void SpawnRandomCube()
    {
        float angle = Random.Range(minAngle, maxAngle);
        Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

        Vector3 spawnPos = arCamera.transform.position + dir.normalized * spawnDistance;

        GameObject cube = Instantiate(cubePrefab, spawnPos, Quaternion.identity);

        CubeMover mover = cube.GetComponent<CubeMover>();
        if (mover == null) mover = cube.AddComponent<CubeMover>();
        mover.target = virtualPlayer;   // CHANGED
    }
}
