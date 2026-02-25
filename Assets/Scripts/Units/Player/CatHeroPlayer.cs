using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

[SceneReferral]
public class CatHeroPlayer : MonoBehaviour
{
    [Inject, SerializeField] private PlayerStat _stat;
    [Inject, SerializeField] private PlayerMovement _movement;
    [Inject, SerializeField] private ProjectileLauncher _projectileLauncher;
    [Inject, SerializeField] private PlayerVisual _visual;

    private float _currentHealth;
    private CompositeDisposable _disposables;
    private CancellationTokenSource _attackCts;
    private bool _isEncounterLocked;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _stat != null ? _stat.MaxHealth : 100f;
    public bool IsAlive => _currentHealth > 0f;
    public Transform Transform => transform;

    private void Awake()
    {
        _currentHealth = _stat != null ? _stat.MaxHealth : 100f;
        _disposables = new CompositeDisposable();
        _attackCts = new CancellationTokenSource();
        _isEncounterLocked = false;

        GameEvents.OnPlayerHit.Subscribe(OnHit).AddTo(_disposables);
        GameEvents.OnPlayerDeath.Subscribe(_ => StopAll()).AddTo(_disposables);

        AutoAttackLoopAsync(_attackCts.Token).Forget();
    }

    private void Update()
    {
        if (!IsAlive) return;
        if (_stat == null || _movement == null) return;

        // Forge Master 방식: "사거리 진입" 순간 교전(사이클) 고정 → 적 전멸(클리어) 전까지 플레이어 정지 유지
        if (_isEncounterLocked)
        {
            if (EnemyRegistry.Enemies.Count == 0)
            {
                _isEncounterLocked = false;
                _movement.IsActive = true;
            }
            else
            {
                _movement.IsActive = false;
            }
            return;
        }

        // 아직 교전이 아니면, 전방(+X) 적이 유효 공격 범위에 들어오는 순간 정지하며 교전을 시작한다.
        var targetInRange = EnemyRegistry.GetFrontMostInRange(transform.position, _stat.AttackRange, onlyAhead: true);
        if (targetInRange != null)
        {
            _isEncounterLocked = true;
            _movement.IsActive = false;
        }
        else
        {
            _movement.IsActive = true;
        }
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
        _isEncounterLocked = false;
        if (_movement != null) _movement.IsActive = false;
    }

    private async UniTaskVoid AutoAttackLoopAsync(CancellationToken token)
    {
        while (IsAlive && !token.IsCancellationRequested)
        {
            if (_stat == null) { await UniTask.Yield(token); continue; }

            // 좌→우 진행: "앞줄(플레이어에 가장 가까운 전방)" 적부터 제거
            var target = EnemyRegistry.GetFrontMostInRange(transform.position, _stat.AttackRange, onlyAhead: true);
            if (target == null)
            {
                // 전방에 없으면(예: 뒤로 지나간 적) 기존 규칙(가장 가까운 적)으로 폴백
                var nearest = EnemyRegistry.GetNearest(transform.position);
                if (nearest is MonoBehaviour mb)
                {
                    float rangeSqr = _stat.AttackRange * _stat.AttackRange;
                    if ((mb.transform.position - transform.position).sqrMagnitude <= rangeSqr)
                    {
                        target = nearest;
                    }
                }
            }
            if (target != null && _projectileLauncher != null)
            {
                Vector3 origin = _visual != null ? _visual.transform.position : transform.position;
                _visual?.PlayAttack();
                _projectileLauncher.Fire(origin, target, _stat.BaseAttackDamage);
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
