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
        {
            if (tracker.waveManager != null)
            {
                tracker.waveManager.RegisterKill();
                Debug.Log("RegisterKill successfully called!");
            }
            else
            {
                Debug.LogError("WaveManager reference missing on CubeTracker!"); 
                Debug.LogError($"Cube name: {collision.gameObject.name}");
            }
        }
        else
        {
            Debug.LogError("CubeTracker component missing on cube!");
        }

        Destroy(collision.gameObject);
        Destroy(gameObject);
    }
}


}
