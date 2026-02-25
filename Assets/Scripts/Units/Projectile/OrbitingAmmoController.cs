using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

public class OrbitingAmmoController : MonoBehaviour
{
    [Header("Anchor")]
    [SerializeField] private Transform anchorOverride;

    [Header("Orbit")]
    [SerializeField] private int ammoCount = 4;
    [SerializeField] private float radius = 0.7f;
    [SerializeField] private float degreesPerSecond = 160f;
    [SerializeField] private float angleOffsetDegrees = 0f;

    [Header("Fire Spice")]
    [Tooltip("발사 직후 잠깐 공전 속도를 올려 리듬감을 줍니다.")]
    [SerializeField] private float fireOrbitSpeedMultiplier = 1.35f;
    [SerializeField] private float fireOrbitSpeedBoostSeconds = 0.12f;
    private float _fireBoostRemaining;

    [Header("Return")]
    [SerializeField] private float returnDuration = 0.25f;
    [SerializeField] private AnimationCurve returnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private enum SlotState { Orbiting, Fired, Returning }

    private class Slot
    {
        public int Index;
        public float AngleOffsetDeg;
        public Projectile Projectile;
        public SlotState State;
        public float ReturnElapsed;
        public Vector3 ReturnStartOffset;
    }

    private readonly List<Slot> _slots = new List<Slot>(8);
    private float _baseAngleDeg;
    private int _nextFireIndex;

    private GameObject _projectilePrefab;
    private float _speed;
    private float _hitRadius;
    private float _flightTime;

    private CancellationToken _destroyToken;

    private void OnEnable()
    {
        _destroyToken = this.GetCancellationTokenOnDestroy();
        InitializeAsync().Forget();
    }

    private void OnDisable()
    {
        DespawnAll();
    }

    public void SetConfig(GameObject projectilePrefab, float speed, float hitRadius, float flightTime)
    {
        _projectilePrefab = projectilePrefab;
        _speed = speed;
        _hitRadius = hitRadius;
        _flightTime = flightTime;
    }

    public void TryFire(Vector3 origin, IEnemy preferredTarget, float damage)
    {
        EnsureAmmo();
        if (_slots.Count == 0) return;

        Slot slot = FindNextOrbitingSlot();
        if (slot == null) return;

        var enemy = preferredTarget;
        if (enemy == null || !enemy.IsAlive)
        {
            enemy = EnemyRegistry.GetFrontMostInRange(origin, 999f, onlyAhead: true);
            if (enemy == null) enemy = EnemyRegistry.GetNearest(origin);
        }
        if (enemy == null || !enemy.IsAlive) return;

        var mb = enemy as MonoBehaviour;
        if (mb == null) return;

        slot.State = SlotState.Fired;
        slot.Projectile.StopHolding();
        slot.Projectile.Launch(mb.transform, enemy, damage, _speed, _hitRadius, _flightTime);

        if (fireOrbitSpeedBoostSeconds > 0f && fireOrbitSpeedMultiplier > 1f)
        {
            _fireBoostRemaining = fireOrbitSpeedBoostSeconds;
        }
    }

    private Slot FindNextOrbitingSlot()
    {
        int tries = _slots.Count;
        for (int t = 0; t < tries; t++)
        {
            int idx = (_nextFireIndex + t) % _slots.Count;
            var s = _slots[idx];
            if (s != null && s.Projectile != null && s.State == SlotState.Orbiting)
            {
                _nextFireIndex = (idx + 1) % _slots.Count;
                return s;
            }
        }
        return null;
    }

    private async UniTaskVoid InitializeAsync()
    {
        await UniTask.WaitUntil(
            () => _destroyToken.IsCancellationRequested || (PoolManager.Instance != null && _projectilePrefab != null),
            cancellationToken: _destroyToken
        );
        if (_destroyToken.IsCancellationRequested) return;

        EnsureAmmo();
    }

    private void Update()
    {
        if (PoolManager.Instance == null || _projectilePrefab == null) return;
        if (_slots.Count == 0) return;

        UpdateOrbit();
    }

    private void EnsureAmmo()
    {
        if (PoolManager.Instance == null || _projectilePrefab == null) return;

        int count = Mathf.Clamp(ammoCount, 1, 16);
        Transform anchor = anchorOverride != null ? anchorOverride : transform;
        if (anchor == null) return;

        bool needsRebuild = _slots.Count != count;
        if (!needsRebuild)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] == null || _slots[i].Projectile == null)
                {
                    needsRebuild = true;
                    break;
                }
            }
        }
        if (!needsRebuild) return;

        DespawnAll();
        _slots.Clear();
        _baseAngleDeg = 0f;
        _nextFireIndex = 0;

        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float slotAngleOffset = angleOffsetDegrees + (step * i);
            Vector3 offset = OffsetFromAngleDeg(_baseAngleDeg + slotAngleOffset, radius);
            Vector3 spawnPos = anchor.position + offset;

            var go = PoolManager.Instance.Spawn(_projectilePrefab, spawnPos, Quaternion.identity);
            var proj = go.GetComponent<Projectile>();
            if (proj == null)
            {
                PoolManager.Instance.Despawn(go);
                continue;
            }

            int capturedIndex = i;
            proj.SetOnFinished(p => OnProjectileFinished(capturedIndex, p));
            proj.StartHolding(anchor, offset);

            _slots.Add(new Slot
            {
                Index = i,
                AngleOffsetDeg = slotAngleOffset,
                Projectile = proj,
                State = SlotState.Orbiting,
                ReturnElapsed = 0f,
                ReturnStartOffset = offset
            });
        }
    }

    private void UpdateOrbit()
    {
        Transform anchor = anchorOverride != null ? anchorOverride : transform;
        if (anchor == null) return;

        float speedMul = 1f;
        if (_fireBoostRemaining > 0f)
        {
            _fireBoostRemaining = Mathf.Max(0f, _fireBoostRemaining - Time.deltaTime);
            float t01 = fireOrbitSpeedBoostSeconds > 0f ? (_fireBoostRemaining / fireOrbitSpeedBoostSeconds) : 0f;
            // 남은 시간이 많을수록(발사 직후) 더 빠르고, 0으로 갈수록 1로 복귀
            speedMul = Mathf.Lerp(1f, fireOrbitSpeedMultiplier, t01);
        }

        _baseAngleDeg += (degreesPerSecond * speedMul) * Time.deltaTime;
        if (_baseAngleDeg > 360f || _baseAngleDeg < -360f) _baseAngleDeg %= 360f;

        float dur = Mathf.Max(0.01f, returnDuration);

        for (int i = 0; i < _slots.Count; i++)
        {
            var s = _slots[i];
            if (s == null || s.Projectile == null) continue;

            Vector3 desiredOffset = OffsetFromAngleDeg(_baseAngleDeg + s.AngleOffsetDeg, radius);

            if (s.State == SlotState.Orbiting)
            {
                s.Projectile.SetHoldOffset(desiredOffset);
                continue;
            }

            if (s.State == SlotState.Returning)
            {
                s.ReturnElapsed += Time.deltaTime;
                float t01 = Mathf.Clamp01(s.ReturnElapsed / dur);
                float eased = Mathf.Clamp01(returnCurve.Evaluate(t01));
                Vector3 offset = Vector3.Lerp(s.ReturnStartOffset, desiredOffset, eased);
                s.Projectile.SetHoldOffset(offset);

                if (t01 >= 1f)
                {
                    s.State = SlotState.Orbiting;
                    s.ReturnElapsed = 0f;
                    s.ReturnStartOffset = offset;
                }
            }
        }
    }

    private void OnProjectileFinished(int slotIndex, Projectile projectile)
    {
        if (projectile == null) return;
        if (slotIndex < 0 || slotIndex >= _slots.Count) { PoolManager.Instance?.Despawn(projectile.gameObject); return; }

        Transform anchor = anchorOverride != null ? anchorOverride : transform;
        if (anchor == null)
        {
            PoolManager.Instance?.Despawn(projectile.gameObject);
            return;
        }

        var slot = _slots[slotIndex];
        if (slot == null || !ReferenceEquals(slot.Projectile, projectile)) return;

        Vector3 currentOffset = projectile.transform.position - anchor.position;
        projectile.StartHolding(anchor, currentOffset);

        slot.State = SlotState.Returning;
        slot.ReturnElapsed = 0f;
        slot.ReturnStartOffset = currentOffset;
    }

    private void DespawnAll()
    {
        if (PoolManager.Instance == null) return;
        for (int i = 0; i < _slots.Count; i++)
        {
            var s = _slots[i];
            if (s?.Projectile != null)
            {
                PoolManager.Instance.Despawn(s.Projectile.gameObject);
            }
        }
    }

    private static Vector3 OffsetFromAngleDeg(float angleDeg, float r)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * r;
    }
}

