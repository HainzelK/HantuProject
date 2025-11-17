using UnityEngine;

public class CubeMover : MonoBehaviour
{
    [Header("Movement Settings")]
    public Transform target;
    public float baseSpeed = 0.8f;
    public float acceleration = 0.5f;
    public float rotateSpeed = 5f;

    [Header("Stopping Settings")]
    public float stopDistance = 0.45f;
    public float destroyDelay = 0.3f;

    private float currentSpeed = 0f;
    private bool isDying = false;

    void Start()
    {
        currentSpeed = baseSpeed;

        if (target == null)
        {
            GameObject vp = GameObject.Find("Virtual Player");
            if (vp != null)
                target = vp.transform;
            else
                Debug.LogError("CubeMover: VirtualPlayer not found!");
        }
    }


    void Update()
    {
        if (isDying || target == null) return;

        Vector3 myPos = transform.position;
        Vector3 targetPos = target.position;
        Vector3 flatTarget = new Vector3(targetPos.x, myPos.y, targetPos.z);

        float distance = Vector3.Distance(myPos, flatTarget);

        if (distance > stopDistance)
        {
            currentSpeed += acceleration * Time.deltaTime;

            Vector3 dir = (flatTarget - myPos).normalized;
            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, rotateSpeed * Time.deltaTime);

            transform.position += transform.forward * currentSpeed * Time.deltaTime;
        }
        else
        {
            // Cube reached player – DO NOT count as kill
            DieNoScore();
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (isDying) return;

        if (col.collider.CompareTag("Projectile"))
        {
            // Count kill
            isDying = true;
            WaveManager.Instance.CubeKilled();

            Destroy(col.gameObject);
            Destroy(gameObject, destroyDelay);
        }
    }

    void DieNoScore()
    {
        isDying = true;
        Destroy(gameObject, destroyDelay);
    }
}
