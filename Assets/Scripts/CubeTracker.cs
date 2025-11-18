using UnityEngine;

public class CubeTracker : MonoBehaviour
{
    public WaveManager waveManager;
    public bool killedByProjectile;

    // Reset when spawned (if using pooling, call this manually)
    public void Reset(WaveManager wm)
    {
        waveManager = wm;
        killedByProjectile = false;
    }

    void OnDestroy()
    {
        if (waveManager == null) return;

        if (killedByProjectile)
        {
            waveManager.RegisterKill();
        }
        else
        {
            Debug.Log("Cube died NOT from projectile — ignoring");
        }
    }
}
