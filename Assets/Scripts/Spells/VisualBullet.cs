using UnityEngine;

public class VisualBullet : MonoBehaviour
{
    public float speed = 50f;
    private Vector3 direction;

    public void Init(Vector3 dir)
    {
        direction = dir.normalized;
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }
}
