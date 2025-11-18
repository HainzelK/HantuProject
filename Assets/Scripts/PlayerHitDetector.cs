using UnityEngine;

public class PlayerHitDetector : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Cube"))
        {
            Debug.Log("Cube hit player — NOT a kill");
            Destroy(collision.gameObject);
        }
    }
}
