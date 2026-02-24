using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float defaultSpeed = 8f;
    [SerializeField] private float hitRadius = 0.3f;

    public void Fire(Vector3 origin, IEnemy target, float damage)
    {
        if (projectilePrefab == null || target == null || PoolManager.Instance == null) return;

        var mb = target as MonoBehaviour;
        if (mb == null) return;

        var go = PoolManager.Instance.Spawn(projectilePrefab, origin, Quaternion.identity);
        var proj = go.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(mb.transform, target, damage, defaultSpeed, hitRadius);
        }
    }
}
