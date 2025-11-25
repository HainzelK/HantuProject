using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Enemies/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    public string enemyName;
    public GameObject enemyPrefab;      // 👈 ADD THIS
    public float moveSpeed = 0.8f;      // Normal = 0.8, Slow = 0.4
    public float acceleration = 0.5f;
    public float maxHealth = 100f;
}