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

    protected float currentHealth;
    protected EnemyState currentState;
    protected Transform myTransform;
    protected Transform targetTransform;

    protected CompositeDisposable poolDisposables;
    protected CancellationTokenSource poolCts;

    private SpriteRenderer HitEffectSprite => _visual != null ? _visual.SpriteRenderer : null;

    private bool _lockY;
    private float _lockedY;

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
        if (HitEffectSprite != null)
        {
            HitEffectSprite.DOKill();
            HitEffectSprite.color = Color.white;
        }

        currentState = EnemyState.Spawning;
    }

    public virtual void OnDespawn()
    {
        EnemyRegistry.Unregister(this);

        myTransform.DOKill();
        if (HitEffectSprite != null) HitEffectSprite.DOKill();

        poolDisposables?.Dispose();
        poolDisposables = null;

        poolCts?.Cancel();
        poolCts?.Dispose();
        poolCts = null;
    }

    public virtual void Initialize(Transform target)
    {
        targetTransform = target;
        currentHealth = _stat != null ? _stat.MaxHealth : 10f;

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

        if (HitEffectSprite != null)
        {
            DOTween.To(() => HitEffectSprite.color, x => HitEffectSprite.color = x, Color.red, 0.1f)
                .SetTarget(HitEffectSprite)
                .OnComplete(() => HitEffectSprite.color = Color.white);
        }
        myTransform.DOPunchScale(Vector3.one * -0.2f, 0.15f, 10, 1);

        if (currentHealth <= 0)
        {
            DieSequenceAsync(poolCts.Token).Forget();
        }
    }

    protected virtual async UniTaskVoid SpawnSequenceAsync(CancellationToken token)
    {
        myTransform.localScale = Vector3.zero;

        await DOTweenUniTaskUtil.AwaitTweenAsync(
            myTransform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack),
            token);

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

        var seq = DOTween.Sequence();
        seq.Append(myTransform.DOScaleY(0f, 0.2f).SetEase(Ease.InBounce));
        if (HitEffectSprite != null)
        {
            seq.Join(DOTween.ToAlpha(() => HitEffectSprite.color, x => HitEffectSprite.color = x, 0f, 0.2f).SetTarget(HitEffectSprite));
        }

        await DOTweenUniTaskUtil.AwaitTweenAsync(seq, token);

        PoolManager.Instance.Despawn(transform.root.gameObject);
    }

    protected virtual void Awake()
    {
        myTransform = transform;
    }
}
