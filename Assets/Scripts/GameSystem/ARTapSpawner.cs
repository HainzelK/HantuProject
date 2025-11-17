// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.XR.ARFoundation;
// using UnityEngine.XR.ARSubsystems;

// public class ARTapSpawner : MonoBehaviour
// {
//     [Header("Spawn Settings")]
//     public GameObject cubePrefab;                // Cube prefab to spawn
//     public float spawnHeightOffset = 0f;        // Optional height offset above the plane

//     private ARRaycastManager raycastManager;
//     private List<ARRaycastHit> hits = new List<ARRaycastHit>();

//     void Awake()
//     {
//         // Automatically find ARRaycastManager in the scene
//         raycastManager = FindObjectOfType<ARRaycastManager>();
//         if (raycastManager == null)
//         {
//             Debug.LogError("[ARTapSpawner] ARRaycastManager not found in the scene!");
//         }
//     }

//     void Update()
//     {
//         if (Input.touchCount == 0 || raycastManager == null) return;

//         Touch touch = Input.GetTouch(0);
//         if (touch.phase != TouchPhase.Began) return;

//         SpawnAtTouch(touch.position);
//     }

//     private void SpawnAtTouch(Vector2 touchPosition)
//     {
//         // Raycast against detected planes
//         if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
//         {
//             Pose hitPose = hits[0].pose;
//             Vector3 spawnPos = hitPose.position + Vector3.up * spawnHeightOffset;

//             if (cubePrefab != null)
//             {
//                 GameObject cube = Instantiate(cubePrefab, spawnPos, Quaternion.identity);

//                 // Auto-assign the target for chasing behavior
//                 CubeMover mover = cube.GetComponent<CubeMover>();
//                 if (mover != null)
//                 {
//                     GameObject vp = GameObject.Find("VirtualPlayer");
//                     if (vp == null)
//                     {
//                         vp = new GameObject("VirtualPlayer");
//                         vp.transform.position = Vector3.zero;
//                     }
//                     mover.target = vp.transform;
//                 }
//             }
//             else
//             {
//                 Debug.LogError("[ARTapSpawner] Cube prefab is not assigned!");
//             }
//         }
//         else
//         {
//             Debug.Log("[ARTapSpawner] No plane detected at touch position.");
//         }
//     }
// }
