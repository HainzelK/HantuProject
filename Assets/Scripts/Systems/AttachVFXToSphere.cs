using UnityEngine;

public class AttachVFXToSphere : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Place your VFX prefab in Assets/Resources/VFX/")]
    public GameObject vfxPrefab; // ← Optional: Assign in Inspector (recommended)

    [Header("Settings")]
    public string vfxPrefabPath = "VFX/FireAura"; // Path relative to Resources folder
    public Vector3 localOffset = Vector3.zero;

    void Start()
    {
        GameObject vfx = null;

        // Method 1: Use Inspector-assigned prefab (BEST)
        if (vfxPrefab != null)
        {
            vfx = Instantiate(vfxPrefab, transform);
        }
        // Method 2: Load from Resources (if not assigned in Inspector)
        else
        {
            vfx = Resources.Load<GameObject>(vfxPrefabPath);
            if (vfx != null)
            {
                vfx = Instantiate(vfx, transform);
            }
            else
            {
                Debug.LogError($"[AttachVFX] VFX prefab not found at 'Resources/{vfxPrefabPath}.prefab'");
                return;
            }
        }

        // Optional: Set local offset
        vfx.transform.localPosition = localOffset;
    }
}