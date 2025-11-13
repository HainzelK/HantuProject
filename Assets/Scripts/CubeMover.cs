using UnityEngine;

public class CubeMover : MonoBehaviour
{
    [Header("Movement Settings")]
    public Transform target;            // The AR camera (player)
    public float baseSpeed = 0.8f;      // Movement speed (m/s)
    public float acceleration = 0.5f;   // Speed increase over time
    public float rotateSpeed = 5f;      // Rotation smoothing factor

    [Header("Stopping Settings")]
    public float stopDistance = 0.45f;  // Distance to stop before reaching player
    public bool destroyOnReach = true;  // Destroy when close enough
    public float destroyDelay = 0.5f;   // Wait before destroy

    [Header("Idle Behavior")]
    public bool rotateInPlaceWhenStopped = true;
    public float idleRotateSpeed = 25f; // degrees per second

    private float currentSpeed;

    void Start()
    {
        currentSpeed = baseSpeed;
    }

    void Update()
    {
        if (target == null) return;

        Vector3 myPos = transform.position;
        Vector3 targetPos = target.position;
        Vector3 targetFlat = new Vector3(targetPos.x, myPos.y, targetPos.z);

        float distance = Vector3.Distance(myPos, targetFlat);

        // --- Move until reaching stop distance ---
        if (distance > stopDistance)
        {
            // Accelerate slightly over time
            currentSpeed += acceleration * Time.deltaTime;

            // Face the player smoothly
            Vector3 dir = (targetFlat - myPos).normalized;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, rotateSpeed * Time.deltaTime);
            }

            // Move forward toward player
            transform.position += transform.forward * currentSpeed * Time.deltaTime;
        }
        else
        {
            // --- When close to player ---
            if (destroyOnReach)
            {
                Destroy(gameObject, destroyDelay);
            }
            else if (rotateInPlaceWhenStopped)
            {
                // Optional idle spin for visual feedback
                transform.Rotate(Vector3.up, idleRotateSpeed * Time.deltaTime, Space.World);
            }
        }
    }
}
