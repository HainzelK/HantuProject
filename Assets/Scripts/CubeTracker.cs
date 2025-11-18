using UnityEngine;

public class CubeTracker : MonoBehaviour
{
    public WaveManager waveManager;

    // Called when projectile hits the cube
    public void MarkKilledByProjectile()
    {
        if (waveManager == null)
        {
            Debug.LogError("CubeTracker: waveManager is NULL!");
            return;
        }

        Debug.Log("CubeTracker: Cube killed by projectile");
        waveManager.OnCubeKilledByPlayer();
        Destroy(gameObject);
    }
}
