using UnityEngine;

public class CubeMover : MonoBehaviour
{
    public Transform target;
    public float baseSpeed = 0.8f;
    public float acceleration = 0.5f;
    public float rotateSpeed = 5f;
    public float stopDistance = 1.5f; // Increased for AR
    public bool destroyOnReach = true; // ← WAS MISSING!
    public float destroyDelay = 0.5f;

    private float currentSpeed;
    private bool reachedPlayer = false;
    private EdgeFlash edgeFlash;
    private Animator animator;

    void Start()
    {
        currentSpeed = baseSpeed;
        animator = GetComponent<Animator>();
        
        if (edgeFlash == null)
        {
            edgeFlash = FindObjectOfType<EdgeFlash>();
        }

        // 🔥 TRIGGER SPAWN ANIMATION
        if (animator != null)
        {
            animator.SetTrigger("Spawn");
        }
    }

    void Update()
    {
        if (target == null) return;

        Vector3 myPos = transform.position;
        // 🔥 FIX: Use target's Y for ground-level movement
        Vector3 targetFlat = new Vector3(target.position.x, 0f, target.position.z);
        float distance = Vector3.Distance(myPos, targetFlat);

        bool isMoving = distance > stopDistance && !reachedPlayer; // ← PREVENT MOVEMENT AFTER REACH

        // 🔥 CONTROL RUN ANIMATION
        if (animator != null)
        {
            animator.SetBool("isMoving", isMoving);
        }

        if (isMoving)
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
        else if (!reachedPlayer) // ← ONLY TRIGGER ONCE
        {
            reachedPlayer = true;
            Debug.Log("Cube reached player — NOT counting as kill");
            
            // 🔥 TRIGGER HIT ANIMATION
            if (animator != null)
            {
                animator.SetTrigger("Hit");
            }
            
            edgeFlash?.Trigger(Color.red, 0.4f);
            PlayerHealth.Instance?.TakeDamage(20f);

            // 🔥 HANDLE DEATH BASED ON destroyOnReach
            if (destroyOnReach)
            {
                if (animator != null)
                {
                    animator.SetTrigger("Death");
                    Destroy(gameObject, 2.0f);
                }
                else
                {
                    Destroy(gameObject, destroyDelay);
                }
            }
        }
    }

    public bool HasReachedPlayer()
    {
        return reachedPlayer;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            Debug.Log("Cube hit player via trigger!");
            
            if (animator != null && !reachedPlayer) // ← PREVENT DOUBLE-TRIGGER
            {
                animator.SetTrigger("Hit");
                reachedPlayer = true;
            }
            
            PlayerHealth.Instance?.TakeDamage(20f);
            
            if (destroyOnReach)
            {
                Destroy(gameObject);
            }
        }
    }

    public void TriggerDeath()
    {
        if (reachedPlayer) return; // ← PREVENT DEATH IF ALREADY REACHED PLAYER
        
        if (animator != null)
        {
            animator.SetTrigger("Death");
            Destroy(gameObject, 2.0f);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}