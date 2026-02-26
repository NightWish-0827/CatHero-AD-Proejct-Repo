using UnityEngine;
using System;
using Random = UnityEngine.Random;
using System.Collections.Generic;
public class Projectile : MonoBehaviour, IPoolable
{
    public enum MovementMode
    {
        HomingSpeed = 0,
        TimedCurve = 1
    }

    public enum ArcSideMode
    {
        Up = 0,
        Down = 1,
        RandomUpOrDown = 2
    }

    [Header("Movement")]
    [SerializeField] private MovementMode movementMode = MovementMode.HomingSpeed;
    [SerializeField] private float defaultFlightTime = 0.25f;
    [SerializeField] private AnimationCurve progressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Arc (TimedCurve)")]
    [SerializeField] private Vector3 arcPlaneNormal = new Vector3(0f, 0f, 1f);

    [SerializeField] private float arcAmplitude = 1.2f;

    [SerializeField] private ArcSideMode arcSideMode = ArcSideMode.RandomUpOrDown;

    [SerializeField] private float arcAngleDegrees = 0f;

    [SerializeField] private float arcAngleJitterDegrees = 25f;

    [SerializeField] private AnimationCurve arcProfile = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f)
    );

    [SerializeField] private bool lockTargetPositionOnFire = true;

    [SerializeField] private bool guaranteeHitOnEnd = true;

    [Header("Hold (Optional)")]
    [SerializeField] private bool followAnchorWhileHolding = true;

    private Transform _target;
    private IEnemy _enemy;
    private float _damage;
    private float _speed;
    private float _hitRadiusSqr;

    private bool _isHolding;
    private Transform _holdAnchor;
    private Vector3 _holdOffsetFromAnchor;

    private Vector3 _startPos;
    private Vector3 _lockedEndPos;
    private Vector3 _arcDir;
    private float _elapsed;
    private float _flightTime;
    private bool _hasCurveSetup;

    private bool _hasDealtDamage;
    private bool _hasFinished;
    private Action<Projectile> _onFinished;

    [Header("VFX (Optional)")]
    [SerializeField] private ProjectileLineTrail lineTrail;

    [Header("VFX Trail Particle (Optional)")]
    [SerializeField] private GameObject trailParticleRoot;
    [SerializeField] private bool restartTrailParticlesOnEnable = true;

    private bool _trailParticleActive;
    private ParticleSystem[] _trailParticleSystems;

    [Header("VFX Hit (Optional)")]
    [SerializeField] private GameObject hitVfxPrefab;
    [SerializeField] private Vector3 hitVfxPositionOffset = Vector3.zero;
    [SerializeField] private bool hitVfxUseProjectileRotation = false;
    [SerializeField] private int hitVfxPrewarmCount = 0;

    private bool _hitVfxSpawned;
    private static readonly HashSet<int> WarmedHitVfxPrefabs = new HashSet<int>();

    private void Awake()
    {
        lineTrail ??= GetComponent<ProjectileLineTrail>();
        lineTrail?.DisableTrail();

        if (trailParticleRoot != null)
        {
            _trailParticleSystems = trailParticleRoot.GetComponentsInChildren<ParticleSystem>(true);
        }
        SetTrailParticleActive(false);

        PrewarmHitVfxIfNeeded();
    }

    public void Initialize(Transform target, IEnemy enemy, float damage, float speed, float hitRadius)
        => Initialize(target, enemy, damage, speed, hitRadius, 0f);

    public void Initialize(Transform target, IEnemy enemy, float damage, float speed, float hitRadius, float flightTime)
    {
        ResetState();

        _target = target;
        _enemy = enemy;
        _damage = damage;
        _speed = speed;
        _hitRadiusSqr = hitRadius * hitRadius;

        _startPos = transform.position;
        _elapsed = 0f;
        _flightTime = flightTime > 0f ? flightTime : defaultFlightTime;
        _lockedEndPos = _target != null ? _target.position : _startPos;
        _hasCurveSetup = false;
    }

    public void OnSpawn()
    {
        ResetState();
    }

    public void OnDespawn()
    {
        ResetState();
    }

    public void Launch(Transform target, IEnemy enemy)
    {
        if (target == null || enemy == null)
        {
            Finish(withDamage: false);
            return;
        }

        _hasFinished = false;
        _hasDealtDamage = false;
        _hitVfxSpawned = false;

        StopHolding();

        _target = target;
        _enemy = enemy;

        _startPos = transform.position;
        _elapsed = 0f;
        _lockedEndPos = _target.position;
        _hasCurveSetup = false;

        lineTrail?.EnableTrail();
        SetTrailParticleActive(true);
    }

    public void Launch(Transform target, IEnemy enemy, float damage, float speed, float hitRadius, float flightTime)
    {
        _damage = damage;
        _speed = speed;
        _hitRadiusSqr = hitRadius * hitRadius;
        _flightTime = flightTime > 0f ? flightTime : defaultFlightTime;
        Launch(target, enemy);
    }

    public void SetOnFinished(Action<Projectile> onFinished)
    {
        _onFinished = onFinished;
    }

    public void StartHolding(Transform anchor, Vector3 offsetFromAnchor)
    {
        _isHolding = true;
        _holdAnchor = anchor;
        _holdOffsetFromAnchor = offsetFromAnchor;

        _target = null;
        _enemy = null;

        lineTrail?.DisableTrail();
        SetTrailParticleActive(false);
        _hitVfxSpawned = false;

        if (_holdAnchor != null)
        {
            transform.position = _holdAnchor.position + _holdOffsetFromAnchor;
        }
    }

    public void StopHolding()
    {
        _isHolding = false;
        _holdAnchor = null;
    }

    public void SetHoldOffset(Vector3 offsetFromAnchor)
    {
        _holdOffsetFromAnchor = offsetFromAnchor;
    }

    private void Update()
    {
        if (_isHolding)
        {
            if (followAnchorWhileHolding)
            {
                if (_holdAnchor == null)
                {
                    Finish(withDamage: false);
                    return;
                }

                Vector3 basePos = _holdAnchor.position + _holdOffsetFromAnchor;
                transform.position = basePos;
            }
            return;
        }

        if (_target == null || _enemy == null || !_enemy.IsAlive)
        {
            Finish(withDamage: false);
            return;
        }

        if (_hasFinished) return;

        if (movementMode == MovementMode.TimedCurve)
        {
            if (!_hasCurveSetup) SetupCurve();

            float duration = Mathf.Max(0.01f, _flightTime);
            _elapsed += Time.deltaTime;
            float t01 = Mathf.Clamp01(_elapsed / duration);

            float easedT = progressCurve != null ? Mathf.Clamp01(progressCurve.Evaluate(t01)) : t01;

            Vector3 endPos = lockTargetPositionOnFire ? _lockedEndPos : _target.position;
            Vector3 basePos = Vector3.Lerp(_startPos, endPos, easedT);

            float arcT = arcProfile != null ? arcProfile.Evaluate(easedT) : (4f * easedT * (1f - easedT));
            transform.position = basePos + (_arcDir * (arcAmplitude * arcT));

            if (_elapsed >= duration)
            {
                if (guaranteeHitOnEnd && _enemy != null && _enemy.IsAlive)
                {
                    DealDamageOnce();
                }
                Finish(withDamage: false);
                return;
            }
        }
        else
        {
            Vector3 dir = (_target.position - transform.position).normalized;
            transform.position += dir * (_speed * Time.deltaTime);
        }

        float sqrDist = (_target.position - transform.position).sqrMagnitude;
        if (sqrDist <= _hitRadiusSqr)
        {
            DealDamageOnce();
            Finish(withDamage: false);
        }
    }

    private void SetupCurve()
    {
        _hasCurveSetup = true;

        Vector3 endPos = lockTargetPositionOnFire ? _lockedEndPos : _target.position;
        Vector3 toEnd = endPos - _startPos;
        if (toEnd.sqrMagnitude < 0.0001f) toEnd = Vector3.right;

        Vector3 planeNormal = arcPlaneNormal.sqrMagnitude > 0.0001f ? arcPlaneNormal.normalized : Vector3.forward;
        Vector3 perp = Vector3.Cross(planeNormal, toEnd).normalized;
        if (perp.sqrMagnitude < 0.0001f) perp = Vector3.up;

        if (Vector3.Dot(perp, Vector3.up) < 0f) perp = -perp;

        float sign = arcSideMode switch
        {
            ArcSideMode.Up => 1f,
            ArcSideMode.Down => -1f,
            _ => (Random.value < 0.5f ? 1f : -1f)
        };

        float angle = arcAngleDegrees;
        if (arcAngleJitterDegrees > 0f)
        {
            angle += Random.Range(-arcAngleJitterDegrees, arcAngleJitterDegrees);
        }

        Vector3 dir = perp * sign;
        dir = Quaternion.AngleAxis(angle, planeNormal) * dir;
        if (dir.sqrMagnitude < 0.0001f) dir = perp * sign;

        _arcDir = dir.normalized;
    }

    private void ResetState()
    {
        _target = null;
        _enemy = null;
        _damage = 0f;
        _speed = 0f;
        _hitRadiusSqr = 0f;

        _isHolding = false;
        _holdAnchor = null;
        _holdOffsetFromAnchor = Vector3.zero;

        _startPos = Vector3.zero;
        _lockedEndPos = Vector3.zero;
        _arcDir = Vector3.zero;
        _elapsed = 0f;
        _flightTime = 0f;
        _hasCurveSetup = false;

        _hasDealtDamage = false;
        _hasFinished = false;
        _onFinished = null;

        lineTrail?.DisableTrail();
        SetTrailParticleActive(false);
        _hitVfxSpawned = false;
    }

    private void SetTrailParticleActive(bool active)
    {
        if (trailParticleRoot == null) return;
        if (_trailParticleActive == active && trailParticleRoot.activeSelf == active) return;

        _trailParticleActive = active;

        if (active)
        {
            if (!trailParticleRoot.activeSelf) trailParticleRoot.SetActive(true);

            if (restartTrailParticlesOnEnable)
            {
                _trailParticleSystems ??= trailParticleRoot.GetComponentsInChildren<ParticleSystem>(true);
                for (int i = 0; i < _trailParticleSystems.Length; i++)
                {
                    var ps = _trailParticleSystems[i];
                    if (ps == null) continue;
                    ps.Clear(true);
                    ps.Play(true);
                }
            }
        }
        else
        {
            if (trailParticleRoot.activeSelf) trailParticleRoot.SetActive(false);
        }
    }

    private void DealDamageOnce()
    {
        if (_hasDealtDamage) return;
        _hasDealtDamage = true;
        _enemy?.TakeDamage(_damage);
        SpawnHitVfxOnce();
    }

    private void SpawnHitVfxOnce()
    {
        if (_hitVfxSpawned) return;
        _hitVfxSpawned = true;

        if (hitVfxPrefab == null) return;

        Vector3 pos = transform.position + hitVfxPositionOffset;
        Quaternion rot = hitVfxUseProjectileRotation ? transform.rotation : Quaternion.identity;

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Spawn(hitVfxPrefab, pos, rot);
        }
        else
        {
            Instantiate(hitVfxPrefab, pos, rot);
        }
    }

    private void PrewarmHitVfxIfNeeded()
    {
        if (hitVfxPrewarmCount <= 0) return;
        if (hitVfxPrefab == null) return;
        if (PoolManager.Instance == null) return;

        int id = hitVfxPrefab.GetInstanceID();
        if (WarmedHitVfxPrefabs.Contains(id)) return;
        WarmedHitVfxPrefabs.Add(id);

        int count = Mathf.Clamp(hitVfxPrewarmCount, 1, 32);
        Vector3 pos = new Vector3(10000f, 10000f, 0f);
        for (int i = 0; i < count; i++)
        {
            var go = PoolManager.Instance.Spawn(hitVfxPrefab, pos, Quaternion.identity);
            PoolManager.Instance.Despawn(go);
        }
    }

    private void Finish(bool withDamage)
    {
        if (_hasFinished) return;
        _hasFinished = true;

        if (withDamage) DealDamageOnce();

        lineTrail?.DisableTrail();
        SetTrailParticleActive(false);

        if (_onFinished != null)
        {
            _onFinished.Invoke(this);
            return;
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Despawn(transform.root.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
