using UnityEngine;
using R3;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>캣 히어로 스타일 적: 악몽이 고양이/주인을 향해 접근 → 공격 시 데미지.</summary>
public abstract class EnemyBase : MonoBehaviour, IEnemy
{
    protected abstract SpriteRenderer HitEffectSprite { get; }

    protected EnemyStatDB stats;
    protected float currentHealth;
    protected EnemyState currentState;
    protected Transform myTransform;
    protected Transform targetTransform;

    protected CompositeDisposable poolDisposables;
    protected CancellationTokenSource poolCts;

  
    public EnemyState CurrentState => currentState;
    public bool IsAlive => currentState != EnemyState.Dead;
    public Transform Target { get => targetTransform; set => targetTransform = value; }
    public EnemyStatDB Stats => stats;

    public virtual void OnSpawn()
    {
        poolDisposables = new CompositeDisposable();
        poolCts = new CancellationTokenSource();

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

    public virtual void Initialize(Transform target, EnemyStatDB statDB)
    {
        targetTransform = target;
        stats = statDB;
        currentHealth = stats.maxHealth;

        EnemyRegistry.Register(this);
        SpawnSequenceAsync(poolCts.Token).Forget();
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
            if (targetTransform == null) break;

            if (currentState == EnemyState.Chasing)
            {
                float sqrDistance = (targetTransform.position - myTransform.position).sqrMagnitude;

                if (sqrDistance <= stats.attackRange * stats.attackRange)
                {
                    currentState = EnemyState.Attacking;
                    await AttackSequenceAsync(token);
                }
                else
                {
                    myTransform.position = Vector3.MoveTowards(
                        myTransform.position,
                        targetTransform.position,
                        stats.moveSpeed * Time.deltaTime);

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            else
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
    }

    /// <summary>캣 히어로: 적이 고양이/주인에게 도달 시 선딜 → 돌진 공격 → 플레이어 데미지 → 쿨타임.</summary>
    protected virtual async UniTask AttackSequenceAsync(CancellationToken token)
    {
        Vector3 directionToTarget = (targetTransform.position - myTransform.position).normalized;

        // 1. 선딜 (살짝 뒤로 빠짐)
        await DOTweenUniTaskUtil.AwaitTweenAsync(
            myTransform.DOMove(myTransform.position - directionToTarget * 0.3f, 0.2f).SetEase(Ease.OutQuad),
            token);

        // 2. 돌진 타격 → 플레이어 데미지 (캣 히어로: 악몽이 고양이/주인을 공격)
        await DOTweenUniTaskUtil.AwaitTweenAsync(
            myTransform.DOMove(myTransform.position + directionToTarget * 0.8f, 0.1f).SetEase(Ease.InFlash),
            token);

        if (currentState != EnemyState.Dead)
        {
            GameEvents.OnPlayerHit.OnNext(stats.attackDamage);
        }

        // 3. 공격 쿨타임
        await UniTask.Delay(System.TimeSpan.FromSeconds(stats.attackCooldown), cancellationToken: token);

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

        PoolManager.Instance.Despawn(gameObject);
    }

    protected virtual void Awake()
    {
        myTransform = transform;
    }
}
// EnemyBase