using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Enemies/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    public string enemyName;
    public GameObject prefab;
    public float baseHP = 100f;
    public float speed = 0.8f;
    public float acceleration = 0.5f;
    public float spawnInterval = 3f; // optional: per-enemy spawn rate
}