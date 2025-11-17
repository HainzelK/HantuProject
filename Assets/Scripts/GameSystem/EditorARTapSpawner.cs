using UnityEngine;

public class EditorTapSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject cubePrefab;          // Cube prefab to spawn
    public Transform spawnPlane;           // The plane object in the scene
    public float spawnHeightOffset = 0f;   // Optional height offset

    void Update()
    {
        // Only left mouse button clicks
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if we hit the plane
            if (hit.collider.transform == spawnPlane)
            {
                Vector3 spawnPos = hit.point + Vector3.up * spawnHeightOffset;

                if (cubePrefab != null)
                {
                    GameObject cube = Instantiate(cubePrefab, spawnPos, Quaternion.identity);

                    // Auto-assign VirtualPlayer target for chasing
                    CubeMover mover = cube.GetComponent<CubeMover>();
                    if (mover != null)
                    {
                        GameObject vp = GameObject.Find("VirtualPlayer");
                        if (vp == null)
                        {
                            vp = new GameObject("VirtualPlayer");
                            vp.transform.position = Vector3.zero;
                        }
                        mover.target = vp.transform;
                    }
                }
            }
        }
    }
}
