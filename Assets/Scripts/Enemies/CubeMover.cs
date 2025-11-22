using UnityEngine;

public class CubeMover : MonoBehaviour
{
    public Transform target;
    public float baseSpeed = 0.8f;
    public float acceleration = 0.5f;
    public float rotateSpeed = 5f;

    public float stopDistance = 0.45f;
    public bool destroyOnReach = true;
    public float destroyDelay = 0.5f;

    private float currentSpeed;
    private bool reachedPlayer = false;

    void Start()
    {
        currentSpeed = baseSpeed;
    }

    void Update()
    {
        if (target == null) return;

        Vector3 myPos = transform.position;
        Vector3 targetFlat = new Vector3(target.position.x, myPos.y, target.position.z);

        float distance = Vector3.Distance(myPos, targetFlat);

        if (distance > stopDistance)
        {
            currentSpeed += acceleration * Time.deltaTime;

            Vector3 dir = (targetFlat - myPos).normalized;

            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion look = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, rotateSpeed * Time.deltaTime);
            }

            transform.position += transform.forward * currentSpeed * Time.deltaTime;
        }
        else
        {
            if (!reachedPlayer)
            {
                reachedPlayer = true;
                Debug.Log("Cube reached player — NOT counting as kill");
                
                // ✅ Apply damage when reaching player
                PlayerHealth.Instance?.TakeDamage(20f);
            }

            if (destroyOnReach)
            {
                Destroy(gameObject, destroyDelay);
            }
        }
    }

    public bool HasReachedPlayer()
    {
        return reachedPlayer;
    }

    void OnTriggerEnter(Collider other) 
    {
        // If cube reaches player (AR Camera)
        if (other.CompareTag("MainCamera"))
        {
            Debug.Log("Cube hit player via trigger!");
            
            // ✅ Damage player (fallback if Update() didn't trigger)
            PlayerHealth.Instance?.TakeDamage(20f);
            
            // Optional: destroy cube on hit
            Destroy(gameObject);
        }
    }
}