using UnityEngine;

public class ProjectileCollision : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Cube"))
        {
            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
    }
}
