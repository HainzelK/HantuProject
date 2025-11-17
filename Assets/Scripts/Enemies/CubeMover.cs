using UnityEngine;

public class CubeMover : MonoBehaviour
{
    public Transform target;
    public float moveSpeed = 1.5f;
    public float stopDistance = 0.3f;

    public void SetTarget(Transform t)
    {
        target = t;
    }

    void Update()
    {
        if (target == null)
            return;

        Vector3 dir = (target.position - transform.position).normalized;

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist > stopDistance)
        {
            transform.position += dir * moveSpeed * Time.deltaTime;
        }

        transform.LookAt(target);
    }

    private void OnDestroy()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.ReportCubeKilled();
    }
}
