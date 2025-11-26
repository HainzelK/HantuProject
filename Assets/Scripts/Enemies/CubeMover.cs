using UnityEngine;

public class CubeMover : MonoBehaviour
{
    public Transform target;
    public float baseSpeed = 0.5f;
    public float acceleration = 0.2f;
    public float rotateSpeed = 5f;
    public float stopDistance = 0.45f;
    public bool destroyOnReach = true;
    public float destroyDelay = 0.5f;

    // 🔥 Hit animation timing
    public float hitAnimationDuration = 0.3f;
    private float hitTime = -1000f;

    // 🔥 NEW: Attack animation timing (start X seconds before arrival)
    public float attackAnimationLeadTime = 2.0f;
    public bool attackAnimationPlayed = false;

    public bool canMove = false;
    public  bool reachedPlayer = false;
    public float currentSpeed;
    private EdgeFlash edgeFlash;
    private Animator animator;

    void Start()
    {
        currentSpeed = baseSpeed;
        animator = GetComponent<Animator>();
        edgeFlash = FindObjectOfType<EdgeFlash>();

        if (animator != null)
        {
            animator.SetTrigger("spawn");
            Invoke(nameof(EnableMovement), 1.0f);
        }
        else
        {
            canMove = true;
        }
    }

    void EnableMovement() => canMove = true;

    void Update()
    {
        if (target == null || !canMove || reachedPlayer) return;

        Vector3 targetFlat = new Vector3(target.position.x, transform.position.y, target.position.z);
        float distance = Vector3.Distance(transform.position, targetFlat);

        // 🔥 Play attack animation early if within lead time (but not yet played)
        if (!attackAnimationPlayed && distance > stopDistance)
        {
            float timeToImpact = distance / Mathf.Max(currentSpeed, 0.01f); // avoid divide by zero
            if (timeToImpact <= attackAnimationLeadTime)
            {
                PlayAttackAnimation();
                attackAnimationPlayed = true;
            }
        }

        // 🔥 Pause movement during hit reaction
        if (Time.time - hitTime < hitAnimationDuration)
        {
            // Keep rotating toward player during hit
            Vector3 dir = (targetFlat - transform.position).normalized;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotateSpeed * Time.deltaTime);
            }
            return;
        }

        bool isMoving = distance > stopDistance;
        if (animator != null)
        {
            try { animator.SetBool("isMoving", isMoving); } catch { }
        }

        if (isMoving)
        {
            currentSpeed += acceleration * Time.deltaTime;
            Vector3 dir = (targetFlat - transform.position).normalized;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotateSpeed * Time.deltaTime);
            }
            transform.position += transform.forward * currentSpeed * Time.deltaTime;
        }
        else
        {
            EnemyReachesPlayer();
        }
    }

    void PlayAttackAnimation()
    {
        Debug.Log("👹 Starting REACH/ATTACK animation (2s before arrival)");
        if (animator != null)
        {
            animator.SetTrigger("reachPlayer");
        }
    }

    // Add this to CubeMover
public void SetSpeed(float newBaseSpeed, float newAcceleration)
{
    baseSpeed = newBaseSpeed;
    acceleration = newAcceleration;
    currentSpeed = baseSpeed; // 🔥 Critical: Reset current speed
}

    void EnemyReachesPlayer()
    {
        if (reachedPlayer) return;
        reachedPlayer = true;

        Debug.Log("💥 Enemy physically reached player — applying damage.");
        edgeFlash?.Trigger(Color.red, 0.4f);
        PlayerHealth.Instance?.TakeDamage(20f);

        if (destroyOnReach)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    public void TakeDamageFromPlayer()
    {
        if (reachedPlayer) return;

        Debug.Log("💥 Enemy hit by player attack — playing TAKE DAMAGE animation.");
        hitTime = Time.time;

        if (animator != null)
        {
            animator.SetTrigger("takeDamage");
        }
    }

    public void TriggerDeath()
    {
        if (reachedPlayer) return;
        reachedPlayer = true;

        Debug.Log("💀 Enemy killed by player — playing DEATH animation.");
        if (animator != null)
        {
            animator.SetTrigger("death");
            Destroy(gameObject, 1f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera") && !reachedPlayer)
        {
            EnemyReachesPlayer();
        }
    }
}