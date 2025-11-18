using UnityEngine;

public class PlayerHitDetector : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Cube"))
        {
            Debug.Log("Cube reached player — NOT counting as kill");

            // DO NOT set tracker.wasKilledByPlayer
            Destroy(collision.gameObject);  
        }
    }
}
