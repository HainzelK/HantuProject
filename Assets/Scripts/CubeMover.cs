using UnityEngine;

public class CubeMover : MonoBehaviour
{
    [Header("Movement Settings")]
    public Transform target;            // The Virtual Player (NOT AR camera)
    public float baseSpeed = 0.8f;
    public float acceleration = 0.5f;
    public float rotateSpeed = 5f;

    [Header("Stopping Settings")]
    public float stopDistance = 0.45f;
    public bool destroyOnReach = true;
    public float destroyDelay = 0.5f;

    [Header("Idle Behavior")]
    public bool rotateInPlaceWhenStopped = true;
    public float idleRotateSpeed = 25f;

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

        if (distance > stopDistance)
        {
            currentSpeed += acceleration * Time.deltaTime;

            Vector3 dir = (targetFlat - myPos).normalized;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, rotateSpeed * Time.deltaTime);
            }

            transform.position += transform.forward * currentSpeed * Time.deltaTime;
        }
        else
        {
            if (destroyOnReach)
            {
                Destroy(gameObject, destroyDelay);
            }
            else if (rotateInPlaceWhenStopped)
            {
                transform.Rotate(Vector3.up, idleRotateSpeed * Time.deltaTime, Space.World);
            }
        }
    }
}
