using UnityEngine;

public class CubeTracker : MonoBehaviour
{
    public WaveManager waveManager;
    public bool killedByProjectile;

    public void Initialize(WaveManager wm)
    {
        waveManager = wm;
        killedByProjectile = false;
    }

    // Optional: if you ever need reset
    public void Reset(WaveManager wm)
    {
        waveManager = wm;
        killedByProjectile = false;
    }

    // ❌ REMOVE KILL LOGIC FROM OnDestroy
    // Cubes should ONLY be counted as kills when hit by projectile
    // Let ProjectileCollision handle everything
}