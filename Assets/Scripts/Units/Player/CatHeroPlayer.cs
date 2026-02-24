using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
/// <summary>캣 히어로: 고양이/주인. Root가 배분한 의존성만 [Inject]로 사용.</summary>
public class CatHeroPlayer : MonoBehaviour
{
    [Inject, SerializeField] private PlayerStat _stat;
    [Inject, SerializeField] private PlayerMovement _movement;
    [Inject, SerializeField] private ProjectileLauncher _projectileLauncher;
    [Inject, SerializeField] private PlayerVisual _visual;

    private float _currentHealth;
    private CompositeDisposable _disposables;
    private CancellationTokenSource _attackCts;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _stat != null ? _stat.MaxHealth : 100f;
    public bool IsAlive => _currentHealth > 0f;
    public Transform Transform => transform;

    private void Awake()
    {
        _currentHealth = _stat != null ? _stat.MaxHealth : 100f;
        _disposables = new CompositeDisposable();
        _attackCts = new CancellationTokenSource();

        GameEvents.OnPlayerHit.Subscribe(OnHit).AddTo(_disposables);
        GameEvents.OnPlayerDeath.Subscribe(_ => StopAll()).AddTo(_disposables);

        AutoAttackLoopAsync(_attackCts.Token).Forget();
    }

    private void OnDestroy()
    {
        StopAll();
        _disposables?.Dispose();
    }

    private void StopAll()
    {
        _attackCts?.Cancel();
        _attackCts?.Dispose();
        _attackCts = null;
        if (_movement != null) _movement.IsActive = false;
    }

    private async UniTaskVoid AutoAttackLoopAsync(CancellationToken token)
    {
        while (IsAlive && !token.IsCancellationRequested)
        {
            if (_stat == null) { await UniTask.Yield(token); continue; }

            var nearest = EnemyRegistry.GetNearest(transform.position);
            if (nearest != null && _projectileLauncher != null)
            {
                var mb = nearest as MonoBehaviour;
                if (mb != null)
                {
                    float sqrDist = (mb.transform.position - transform.position).sqrMagnitude;
                    if (sqrDist <= _stat.AttackRange * _stat.AttackRange)
                    {
                        Vector3 origin = _visual != null ? _visual.transform.position : transform.position;
                        _projectileLauncher.Fire(origin, nearest, _stat.BaseAttackDamage);
                    }
                }
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_stat.AttackInterval), cancellationToken: token);
        }
    }

    private void OnHit(float damage)
    {
        if (!IsAlive) return;

        _currentHealth = Mathf.Max(0f, _currentHealth - damage);

        if (_currentHealth <= 0f) GameEvents.OnPlayerDeath.OnNext(R3.Unit.Default);
    }
}
