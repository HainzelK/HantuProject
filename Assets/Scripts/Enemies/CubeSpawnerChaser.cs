using System.Collections;
using UnityEngine;

public class CubeSpawnerChaser : MonoBehaviour
{
    public GameObject cubePrefab;
    public float spawnDistance = 2f;    // Distance around player
    public float speed = 1f;            // Cube movement speed
    public float reachDistance = 0.1f;  // Distance considered "reached"

    private Vector3 playerPosition = Vector3.zero; // Player always at (0,0,0)

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnCube();
            yield return new WaitForSeconds(5f);
        }
    }

    void SpawnCube()
    {
        // Generate random angle for full 360° around player
        float angle = Random.Range(0f, 360f);
        float rad = angle * Mathf.Deg2Rad;

        // Position around the player
        Vector3 spawnPos = new Vector3(
            playerPosition.x + Mathf.Cos(rad) * spawnDistance,
            playerPosition.y,
            playerPosition.z + Mathf.Sin(rad) * spawnDistance
        );

        // Spawn cube
        GameObject cube = Instantiate(cubePrefab, spawnPos, Quaternion.identity);

        // Start chasing behavior
        StartCoroutine(CubeChase(cube));
    }

    IEnumerator CubeChase(GameObject cube)
    {
        while (cube != null)
        {
            cube.transform.position = Vector3.MoveTowards(
                cube.transform.position,
                playerPosition,
                speed * Time.deltaTime
            );

            if (Vector3.Distance(cube.transform.position, playerPosition) < reachDistance)
            {
                Destroy(cube);
                yield break;
            }

            yield return null;
        }
    }
}
