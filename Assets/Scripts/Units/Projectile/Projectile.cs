using UnityEngine;

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

    [Tooltip("TimedCurve 모드에서만 사용. Launcher가 비행 시간을 넘기지 않으면 이 값을 사용합니다.")]
    [SerializeField] private float defaultFlightTime = 0.25f;

    [Tooltip("0~1 진행률에 적용되는 속도 커브(타격감/가속감 조절)")]
    [SerializeField] private AnimationCurve progressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Arc (TimedCurve)")]
    [Tooltip("2D(XY)라면 기본값(Vector3.forward) 유지 권장. 곡사 방향(수직/좌우)을 정의하는 평면 노멀입니다.")]
    [SerializeField] private Vector3 arcPlaneNormal = new Vector3(0f, 0f, 1f);

    [Tooltip("곡사 최대 오프셋(월드 유닛). 0이면 직선과 동일.")]
    [SerializeField] private float arcAmplitude = 1.2f;

    [Tooltip("곡사 방향(위/아래/랜덤)")]
    [SerializeField] private ArcSideMode arcSideMode = ArcSideMode.RandomUpOrDown;

    [Tooltip("기본 곡사 각도(도). 0이면 기본 '위/아래' 방향으로 휩니다.")]
    [SerializeField] private float arcAngleDegrees = 0f;

    [Tooltip("발사마다 arcAngleDegrees에 더해지는 랜덤 편차(도).")]
    [SerializeField] private float arcAngleJitterDegrees = 25f;

    [Tooltip("0~1 진행률에 따른 곡사 높이 프로파일(끝은 0이 되어야 타겟에 정확히 도착)")]
    [SerializeField] private AnimationCurve arcProfile = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f)
    );

    [Tooltip("타겟 위치를 발사 시점에 고정합니다(곡사 느낌 강화). 끄면 타겟을 따라가며 곡사합니다.")]
    [SerializeField] private bool lockTargetPositionOnFire = true;

    [Tooltip("TimedCurve 종료 시점에 타겟이 살아있으면 거리와 무관하게 데미지를 적용합니다(연출 우선).")]
    [SerializeField] private bool guaranteeHitOnEnd = true;

    private Transform _target;
    private IEnemy _enemy;
    private float _damage;
    private float _speed;
    private float _hitRadiusSqr;

    private Vector3 _startPos;
    private Vector3 _lockedEndPos;
    private Vector3 _arcDir;
    private float _elapsed;
    private float _flightTime;
    private bool _hasCurveSetup;

    public void Initialize(Transform target, IEnemy enemy, float damage, float speed, float hitRadius)
        => Initialize(target, enemy, damage, speed, hitRadius, 0f);

    public void Initialize(Transform target, IEnemy enemy, float damage, float speed, float hitRadius, float flightTime)
    {
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

    public void OnSpawn() { }

    public void OnDespawn() { }

    private void Update()
    {
        if (_target == null || _enemy == null || !_enemy.IsAlive)
        {
            ReturnToPool();
            return;
        }

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
                    _enemy.TakeDamage(_damage);
                }
                ReturnToPool();
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
            _enemy.TakeDamage(_damage);
            ReturnToPool();
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

        // "Up" 기준을 월드 +Y로 맞춰두면, 좌/우 방향에 따라 곡사 방향이 뒤집히는 문제를 줄일 수 있습니다.
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

    private void ReturnToPool()
    {
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Despawn(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
