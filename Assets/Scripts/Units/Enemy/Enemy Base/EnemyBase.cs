using UnityEngine;
using R3;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

public abstract class EnemyBase : MonoBehaviour, IEnemy
{
    [Inject, SerializeField] private EnemyStat _stat;
    [Inject, SerializeField] private EnemyVisual _visual;
    [Inject, SerializeField] private EnemyHealthBarSR _healthBar;

    protected float currentHealth;
    protected EnemyState currentState;
    protected Transform myTransform;
    protected Transform targetTransform;

    protected CompositeDisposable poolDisposables;
    protected CancellationTokenSource poolCts;

    private float cachedMaxHealth = -1f;

    private bool _lockY;
    private float _lockedY;

    protected EnemyVisual Visual => _visual;

    public EnemyState CurrentState => currentState;
    public bool IsAlive => currentState != EnemyState.Dead;
    public Transform Target { get => targetTransform; set => targetTransform = value; }
    public EnemyStat Stats => _stat;

    public virtual void OnSpawn()
    {
        poolDisposables = new CompositeDisposable();
        poolCts = new CancellationTokenSource();

        myTransform.localPosition = Vector3.zero;
        myTransform.DOKill();
        _visual?.ResetForSpawn();

        currentState = EnemyState.Spawning;

        EnsureHealthBarCached();
        RefreshHealthBar();
    }

    public virtual void OnDespawn()
    {
        EnemyRegistry.Unregister(this);

        myTransform.DOKill();
        _visual?.KillTweens();

        poolDisposables?.Dispose();
        poolDisposables = null;

        poolCts?.Cancel();
        poolCts?.Dispose();
        poolCts = null;

        if (_healthBar != null) _healthBar.SetVisible(false);
    }

    public virtual void Initialize(Transform target)
    {
        targetTransform = target;
        currentHealth = _stat != null ? _stat.MaxHealth : 10f;
        cachedMaxHealth = _stat != null ? _stat.MaxHealth : 10f;
        RefreshHealthBar();

        EnemyRegistry.Register(this);
        SpawnSequenceAsync(poolCts.Token).Forget();
    }

    public void ConfigureVerticalLock(bool lockY, float lockedY)
    {
        _lockY = lockY;
        _lockedY = lockedY;

        if (_lockY)
        {
            var p = myTransform.position;
            p.y = _lockedY;
            myTransform.position = p;
        }
    }

    public virtual void TakeDamage(float damage)
    {
        if (currentState == EnemyState.Dead) return;

        currentHealth -= damage;
        RefreshHealthBar();

        _visual?.PlayHitFlash();
        _visual?.PlayHitPunchScale();

        if (currentHealth <= 0)
        {
            DieSequenceAsync(poolCts.Token).Forget();
        }
    }

    protected virtual async UniTaskVoid SpawnSequenceAsync(CancellationToken token)
    {
        if (_visual != null)
        {
            await DOTweenUniTaskUtil.AwaitTweenAsync(_visual.PlaySpawnScale(), token);
        }

        currentState = EnemyState.Chasing;
        BehaviorLoopAsync(token).Forget();
    }

    protected virtual async UniTaskVoid BehaviorLoopAsync(CancellationToken token)
    {
        while (currentState != EnemyState.Dead && !token.IsCancellationRequested)
        {
            if (targetTransform == null || _stat == null) break;

            if (_lockY)
            {
                var p = myTransform.position;
                if (!Mathf.Approximately(p.y, _lockedY))
                {
                    p.y = _lockedY;
                    myTransform.position = p;
                }
            }

            if (currentState == EnemyState.Chasing)
            {
                // 지상 적 기본형: y축 추적을 하지 않고, 수평(x) 기준으로만 전진/공격 판정
                float absDx = Mathf.Abs(targetTransform.position.x - myTransform.position.x);
                if (absDx <= _stat.AttackRange)
                {
                    currentState = EnemyState.Attacking;
                    await AttackSequenceAsync(token);
                }
                else
                {
                    Vector3 targetPos = targetTransform.position;
                    targetPos.y = _lockY ? _lockedY : myTransform.position.y;
                    targetPos.z = myTransform.position.z;

                    myTransform.position = Vector3.MoveTowards(
                        myTransform.position,
                        targetPos,
                        _stat.MoveSpeed * Time.deltaTime);

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            else
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
    }

    protected virtual async UniTask AttackSequenceAsync(CancellationToken token)
    {
        Vector3 directionToTarget = (targetTransform.position - myTransform.position);
        directionToTarget.y = 0f;
        directionToTarget.z = 0f;
        if (directionToTarget.sqrMagnitude < 0.0001f)
        {
            directionToTarget = Vector3.left;
        }
        else
        {
            directionToTarget.Normalize();
        }

        if (_lockY)
        {
            var p = myTransform.position;
            p.y = _lockedY;
            myTransform.position = p;
        }

        await DOTweenUniTaskUtil.AwaitTweenAsync(
            myTransform.DOMove(myTransform.position - directionToTarget * 0.3f, 0.2f).SetEase(Ease.OutQuad),
            token);

        await DOTweenUniTaskUtil.AwaitTweenAsync(
            myTransform.DOMove(myTransform.position + directionToTarget * 0.8f, 0.1f).SetEase(Ease.InFlash),
            token);

        if (currentState != EnemyState.Dead && _stat != null)
        {
            GameEvents.OnPlayerHit.OnNext(_stat.AttackDamage);
        }

        float cooldown = _stat != null ? _stat.AttackCooldown : 1.5f;
        await UniTask.Delay(TimeSpan.FromSeconds(cooldown), cancellationToken: token);

        if (currentState != EnemyState.Dead)
        {
            currentState = EnemyState.Chasing;
        }
    }

    protected virtual async UniTaskVoid DieSequenceAsync(CancellationToken token)
    {
        currentState = EnemyState.Dead;
        GameEvents.OnEnemyKilled.OnNext(this);
        if (_healthBar != null) _healthBar.SetVisible(false);

        var seq = _visual != null ? _visual.CreateDieSequence() : DOTween.Sequence();

        await DOTweenUniTaskUtil.AwaitTweenAsync(seq, token);

        PoolManager.Instance.Despawn(transform.root.gameObject);
    }

    protected virtual void Awake()
    {
        myTransform = transform;

        if (_stat == null) _stat = GetComponentInChildren<EnemyStat>(true);
        if (_visual == null) _visual = GetComponentInChildren<EnemyVisual>(true);

        EnsureHealthBarCached();
    }

    private void EnsureHealthBarCached()
    {
        if (_healthBar != null) return;
        _healthBar = GetComponentInChildren<EnemyHealthBarSR>(true);
    }

    private void RefreshHealthBar()
    {
        EnsureHealthBarCached();
        if (_healthBar == null) return;

        float max = _stat != null ? _stat.MaxHealth : cachedMaxHealth;
        if (max <= 0f) max = 10f;
        cachedMaxHealth = max;

        float progress = Mathf.Clamp01(currentHealth / max);
        _healthBar.SetVisible(true);
        _healthBar.SetProgress01(progress);
    }
}
