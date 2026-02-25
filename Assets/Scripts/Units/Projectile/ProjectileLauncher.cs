using UnityEngine;

[RequireComponent(typeof(OrbitingAmmoController))]
public class ProjectileLauncher : MonoBehaviour
{
    public enum FireMode
    {
        Direct = 0,
        // (Legacy) PreloadBurst = 1, removed
        OrbitingAmmo = 2
    }

    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float defaultSpeed = 8f;
    [SerializeField] private bool useFlightTime = true;
    [SerializeField] private float defaultFlightTime = 0.25f;
    [SerializeField] private float hitRadius = 0.3f;

    [Header("Fire Mode")]
    [SerializeField] private FireMode fireMode = FireMode.Direct;

    [SerializeField] private OrbitingAmmoController orbitingAmmo;

    public GameObject ProjectilePrefab => projectilePrefab;

    public void SetProjectilePrefab(GameObject prefab)
    {
        projectilePrefab = prefab;
        SyncOrbitingAmmoConfig();
    }

    private void Awake()
    {
        orbitingAmmo ??= GetComponent<OrbitingAmmoController>();
        SyncOrbitingAmmoConfig();
    }

    private void OnEnable()
    {
        SyncOrbitingAmmoConfig();
    }

    public void Fire(Vector3 origin, IEnemy target, float damage)
    {
        if (projectilePrefab == null || PoolManager.Instance == null) return;

        if (fireMode == FireMode.OrbitingAmmo)
        {
            SyncOrbitingAmmoConfig();
            orbitingAmmo?.TryFire(origin, target, damage);
            return;
        }

        if (target == null) return;
        FireDirect(origin, target, damage);
    }

    private void FireDirect(Vector3 origin, IEnemy target, float damage)
    {
        var mb = target as MonoBehaviour;
        if (mb == null) return;

        var go = PoolManager.Instance.Spawn(projectilePrefab, origin, Quaternion.identity);
        var proj = go.GetComponent<Projectile>();
        if (proj == null) return;

        if (useFlightTime)
        {
            proj.Initialize(mb.transform, target, damage, defaultSpeed, hitRadius, defaultFlightTime);
        }
        else
        {
            proj.Initialize(mb.transform, target, damage, defaultSpeed, hitRadius);
        }
    }


    private void SyncOrbitingAmmoConfig()
    {
        if (orbitingAmmo == null) return;
        float flightTime = useFlightTime ? defaultFlightTime : 0f;
        orbitingAmmo.SetConfig(projectilePrefab, defaultSpeed, hitRadius, flightTime);
    }
}
