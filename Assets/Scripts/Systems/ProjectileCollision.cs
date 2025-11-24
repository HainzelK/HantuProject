using UnityEngine;

public class ProjectileCollision : MonoBehaviour
{
    public float damage = 50f;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("MainCamera"))
        {
            PlayerHealth.Instance?.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (collision.gameObject.CompareTag("Cube"))
        {
            Debug.Log("Projectile hit cube!");

            EnemyHealth enemyHealth = collision.gameObject.GetComponent<EnemyHealth>();
            CubeMover cubeMover = collision.gameObject.GetComponent<CubeMover>();

            // 🔥 NEW: Always notify enemy of hit (play takeDamage anim)
            cubeMover?.TakeDamageFromPlayer();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);

                if (enemyHealth.CurrentHealth <= 0)
                {
                    cubeMover?.TriggerDeath();
                    HandleEnemyDeath(collision.gameObject);
                }
            }
            else
            {
                // No health → kill instantly
                cubeMover?.TriggerDeath();
                HandleEnemyDeath(collision.gameObject);
            }

            Destroy(gameObject);
        }
    }

void HandleEnemyDeath(GameObject enemy)
{
    if (enemy == null) return;

    CubeTracker tracker = enemy.GetComponent<CubeTracker>();
    if (tracker != null)
    {
        tracker.killedByProjectile = true;

        if (tracker.waveManager != null)
        {
            if (tracker.waveManager.enemyIndicatorManager != null)
            {
                tracker.waveManager.enemyIndicatorManager.UnregisterEnemy(enemy);
            }
            tracker.waveManager.RegisterKill();
            Debug.Log("Enemy killed by projectile! RegisterKill called.");
        }
        else
        {
            Debug.LogError("WaveManager reference missing on CubeTracker!");
            Debug.LogError($"Cube name: {enemy.name}");
            
            var indicatorMgr = FindObjectOfType<EnemyIndicatorManager>();
            if (indicatorMgr != null)
            {
                indicatorMgr.UnregisterEnemy(enemy);
            }
        }
    }
    else
    {
        Debug.LogError("CubeTracker component missing on cube!");
        var indicatorMgr = FindObjectOfType<EnemyIndicatorManager>();
        if (indicatorMgr != null)
        {
            indicatorMgr.UnregisterEnemy(enemy);
        }
    }

    // 🔥 REMOVED: Do NOT destroy here at all!
    // Destruction is handled by CubeMover.TriggerDeath()
}

}   