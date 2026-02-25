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

    [Header("First Encounter Roulette (Weapon Unlock)")]
    [SerializeField] private Roulette firstEncounterRoulette;
    [SerializeField] private RouletteSpinPanel firstEncounterRouletteSpinPanel;
    [SerializeField] private int firstEncounterRouletteFixedIndex = 0;
    [SerializeField] private int firstEncounterRouletteAngleRoll = 0;

    private float _currentHealth;
    private CompositeDisposable _disposables;
    private CancellationTokenSource _attackCts;
    private bool _isEncounterLocked;

    private bool _firstEncounterRouletteDone;
    private bool _isFirstEncounterRoulettePlaying;
    private float _roulettePrevTimeScale = 1f;
    private bool _roulettePausedTimeScale;
    private GameObject _lockedProjectilePrefab;

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

        LockWeaponUntilFirstEncounterRoulette();
        AutoAttackLoopAsync(_attackCts.Token).Forget();
    }

    private void Update()
    {
        if (!IsAlive) return;
        if (_stat == null || _movement == null) return;

        if (_isFirstEncounterRoulettePlaying)
        {
            _movement.IsActive = false;
            return;
        }

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

            if (!_firstEncounterRouletteDone && !_isFirstEncounterRoulettePlaying)
            {
                StartFirstEncounterRouletteAsync().Forget();
            }
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

        RestoreTimeScaleIfPausedForRoulette();
    }

    private void LockWeaponUntilFirstEncounterRoulette()
    {
        if (_projectileLauncher == null) return;
        if (_firstEncounterRouletteDone) return;

        // 시작 시 무기를 '잠금' 처리: 첫 조우 룰렛 완료 후 지급(Unlock) 연출
        if (_lockedProjectilePrefab == null)
        {
            _lockedProjectilePrefab = _projectileLauncher.ProjectilePrefab;
        }
        _projectileLauncher.SetProjectilePrefab(null);
    }

    private void UnlockWeapon()
    {
        if (_projectileLauncher == null) return;
        if (_lockedProjectilePrefab == null) return;
        _projectileLauncher.SetProjectilePrefab(_lockedProjectilePrefab);
        GameEvents.OnPlayerWeaponUnlocked.OnNext(R3.Unit.Default);
    }

    private void RestoreTimeScaleIfPausedForRoulette()
    {
        if (!_roulettePausedTimeScale) return;
        _roulettePausedTimeScale = false;
        Time.timeScale = _roulettePrevTimeScale;
    }

    private async UniTaskVoid StartFirstEncounterRouletteAsync()
    {
        _isFirstEncounterRoulettePlaying = true;

        Roulette roulette = firstEncounterRoulette;
        if (roulette == null)
        {
            roulette = FindFirstObjectByType<Roulette>(FindObjectsInactive.Include);
        }

        if (roulette == null)
        {
            // 룰렛이 없으면 안전하게 즉시 Unlock 후 진행
            _firstEncounterRouletteDone = true;
            UnlockWeapon();
            _isFirstEncounterRoulettePlaying = false;
            return;
        }

        GameObject rouletteGo = roulette.gameObject;
        bool prevActive = rouletteGo.activeSelf;

        try
        {
            _roulettePrevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _roulettePausedTimeScale = true;

            rouletteGo.SetActive(true);

            // 활성화 직후 1프레임 대기(Awake/레이아웃 안정화)
            await UniTask.Yield(PlayerLoopTiming.Update);

            // 룰렛은 자동으로 돌지 않고, 버튼 클릭 후에만 스핀되도록 한다.
            var panel = firstEncounterRouletteSpinPanel != null
                ? firstEncounterRouletteSpinPanel
                : rouletteGo.GetComponentInChildren<RouletteSpinPanel>(true);

            if (panel != null)
            {
                panel.Show();
                await panel.WaitForClickAndSpinFixedAsync(firstEncounterRouletteFixedIndex, firstEncounterRouletteAngleRoll);
            }
            else
            {
                // 패널(버튼)이 없으면 안전 폴백(개발 중 데드락 방지)
                Debug.LogWarning("[CatHeroPlayer] RouletteSpinPanel not found. Falling back to auto spin.");
                await roulette.SpinFixedAsync(firstEncounterRouletteFixedIndex, firstEncounterRouletteAngleRoll);
            }

            _firstEncounterRouletteDone = true;

            // 룰렛 종료 = 공격권/무기 획득
            UnlockWeapon();
        }
        finally
        {
            var panel = firstEncounterRouletteSpinPanel != null
                ? firstEncounterRouletteSpinPanel
                : (rouletteGo != null ? rouletteGo.GetComponentInChildren<RouletteSpinPanel>(true) : null);
            if (panel != null)
            {
                panel.Hide();
            }

            // 룰렛만 띄우는 연출이므로, 원래 비활성 오브젝트였다면 복구
            if (!prevActive && rouletteGo != null)
            {
                rouletteGo.SetActive(false);
            }

            RestoreTimeScaleIfPausedForRoulette();
            _isFirstEncounterRoulettePlaying = false;
        }

        // NOTE: 재개 직후 수동 발사는 자동 공격 루프와 타이밍이 겹쳐
        // "획득하자마자 2번 발사"처럼 보일 수 있어 제거합니다.
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
