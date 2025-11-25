using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class AttachParticleSystem : MonoBehaviour
{
    [Header("Target to Follow")]
    public Transform target; // The sphere (or object) to attach to

    [Header("Offset Settings")]
    public Vector3 localOffset = Vector3.zero; // Offset relative to target
    public bool rotateWithTarget = true;       // Match target's rotation

    private Transform _target;

    void Start()
    {
        // If no target assigned, use parent as target
        _target = target != null ? target : transform.parent;

        if (_target == null)
        {
            Debug.LogError("[AttachParticleSystem] No target assigned and no parent found!", this);
            enabled = false;
            return;
        }

        // Ensure ParticleSystem plays on start
        var ps = GetComponent<ParticleSystem>();
        if (ps != null && !ps.isPlaying)
        {
            ps.Play();
        }
    }

    void LateUpdate()
    {
        if (_target == null) return;

        // Update position
        transform.position = _target.TransformPoint(localOffset);

        // Update rotation (optional)
        if (rotateWithTarget)
        {
            transform.rotation = _target.rotation;
        }
    }
}