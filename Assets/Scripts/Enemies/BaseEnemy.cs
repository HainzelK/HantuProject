// BaseEnemy.cs (optional)
using UnityEngine;

public class BaseEnemy : MonoBehaviour
{
    public Animator animator;
    public virtual void OnSpawn() { }
    public virtual void OnHit() { }
    public virtual void OnDeath() { }
}