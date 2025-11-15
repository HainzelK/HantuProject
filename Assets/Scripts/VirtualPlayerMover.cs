using UnityEngine;

public class VirtualPlayerMover : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;    // Sensitivity of accelerometer
    public float smoothFactor = 5f; // Smoothing for movement

    private Vector3 velocity = Vector3.zero;

    void Update()
    {
        Vector3 tilt = Input.acceleration;

        // Convert tilt to movement on X/Z plane
        Vector3 targetMove = new Vector3(tilt.x, 0f, tilt.y) * moveSpeed;

        // Smooth movement
        velocity = Vector3.Lerp(velocity, targetMove, smoothFactor * Time.deltaTime);

        transform.position += velocity * Time.deltaTime;
    }
}
