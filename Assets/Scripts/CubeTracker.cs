using UnityEngine;

public class CubeTracker : MonoBehaviour
{
    public WaveManager waveManager;
    public bool killedByProjectile = false;

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
