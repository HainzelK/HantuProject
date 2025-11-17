using UnityEngine;

public class ProjectileCollision : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Cube"))
        {
            WaveManager.Instance.CubeKilled();
            Destroy(collision.gameObject);  // destroy cube
            Destroy(gameObject);           // destroy projectile
        }
    }

}
