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
        if (proj == null)
        {
            // 프리팹 세팅 오류/교체(룰렛 등)로 Projectile 컴포넌트가 없을 수 있음.
            // 이 경우 스폰된 오브젝트를 방치하면 이후 Despawn 중복/미등록 경고로 이어질 수 있어 즉시 반환합니다.
            PoolManager.Instance.Despawn(go);
            return;
        }

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

        // OrbitingAmmo는 해당 모드일 때만 활성화(그 외에는 잔탄/오비팅 유지로 오해될 수 있음)
        bool shouldEnableOrbiting = fireMode == FireMode.OrbitingAmmo && projectilePrefab != null;
        if (orbitingAmmo.enabled != shouldEnableOrbiting)
        {
            orbitingAmmo.enabled = shouldEnableOrbiting; // disable 시 OnDisable -> DespawnAll()
        }
    }
}
