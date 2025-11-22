using UnityEngine;

public class ARRandomSpawner : MonoBehaviour
{
    public Camera arCamera;
    public GameObject cubePrefab;
    public Transform virtualPlayer;

    public float spawnDistance = 2.5f;
    public float spawnInterval = 5f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnCube();
        }
    }

    void SpawnCube()
    {
        float angle = Random.Range(0f, 360f);
        Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
        Vector3 pos = arCamera.transform.position + dir.normalized * spawnDistance;

        GameObject cube = Instantiate(cubePrefab, pos, Quaternion.identity);

        CubeMover mover = cube.GetComponent<CubeMover>();
        mover.target = virtualPlayer;
    }
}
