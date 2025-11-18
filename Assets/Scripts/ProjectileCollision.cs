using UnityEngine;

public class ProjectileCollision : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Cube"))
        {
            Debug.Log("Projectile hit cube — KILL!");

            CubeTracker tracker = collision.gameObject.GetComponent<CubeTracker>();
            if (tracker != null)
                tracker.killedByProjectile = true;

            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
    }
}
