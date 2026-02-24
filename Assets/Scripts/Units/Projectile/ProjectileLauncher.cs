using UnityEngine;

/// <summary>투사체 발사.</summary>
public class ProjectileLauncher : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float defaultSpeed = 8f;
    [SerializeField] private float hitRadius = 0.3f;

    public void Fire(Vector3 origin, IEnemy target, float damage)
    {
        if (projectilePrefab == null || target == null) return;

        var mb = target as MonoBehaviour;
        if (mb == null) return;

        var go = Instantiate(projectilePrefab, origin, Quaternion.identity);
        if (go.TryGetComponent(out Projectile proj))
        {
            proj.Initialize(mb.transform, target, damage, defaultSpeed, hitRadius);
        }
    }
}
