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

    [Header("Second Cluster Roulette (Weapon Re-Unlock)")]
    [SerializeField] private int secondClusterRouletteFixedIndex = 1;
    [SerializeField] private int secondClusterRouletteAngleRoll = 0;

    private float _currentHealth;
    private CompositeDisposable _disposables;
    private CancellationTokenSource _attackCts;
    private bool _isEncounterLocked;

    private bool _firstEncounterRouletteDone;
    private bool _isFirstEncounterRoulettePlaying;
    private float _roulettePrevTimeScale = 1f;
    private bool _roulettePausedTimeScale;
    private GameObject _lockedProjectilePrefab;

    private bool _weaponUnlocked;
    private int _weaponUnlockedWaveIndex = -1;
    private int _currentWaveIndex;
    private bool _secondClusterRouletteDone;

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
        GameEvents.OnWaveStarted.Subscribe(w => _currentWaveIndex = w).AddTo(_disposables);
        GameEvents.OnWaveCleared.Subscribe(OnWaveCleared).AddTo(_disposables);
        GameEvents.OnSecondDrawHalfCinematicImpact.Subscribe(_ => OnSecondDrawHalfCinematicImpact()).AddTo(_disposables);
        GameEvents.OnSecondDrawHalfCinematicFinished.Subscribe(_ => OnSecondDrawHalfCinematicFinished()).AddTo(_disposables);

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
                StartEncounterRouletteAsync(firstEncounterRouletteFixedIndex, firstEncounterRouletteAngleRoll, markFirstDone: true).Forget();
            }
            else if (_firstEncounterRouletteDone
                     && !_secondClusterRouletteDone
                     && !_isFirstEncounterRoulettePlaying
                     && !_weaponUnlocked
                     && _weaponUnlockedWaveIndex > 0
                     && _currentWaveIndex == (_weaponUnlockedWaveIndex + 1))
            {
                // 1번 무기 획득 후 1개 클러스터(웨이브) 클리어 시 무기 반납 → 바로 다음 클러스터에서 재획득(룰렛) 트리거
                StartEncounterRouletteAsync(secondClusterRouletteFixedIndex, secondClusterRouletteAngleRoll, markFirstDone: false).Forget();
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

    private void LockWeaponAfterClusterClear()
    {
        if (_projectileLauncher == null) return;
        if (_lockedProjectilePrefab == null) return;
        _projectileLauncher.SetProjectilePrefab(null);
        _weaponUnlocked = false;
    }

    private void UnlockWeapon()
    {
        if (_projectileLauncher == null) return;
        if (_lockedProjectilePrefab == null) return;
        _projectileLauncher.SetProjectilePrefab(_lockedProjectilePrefab);
        GameEvents.OnPlayerWeaponUnlocked.OnNext(R3.Unit.Default);

        _weaponUnlocked = true;
        if (_currentWaveIndex > 0) _weaponUnlockedWaveIndex = _currentWaveIndex;
    }

    private void RestoreTimeScaleIfPausedForRoulette()
    {
        if (!_roulettePausedTimeScale) return;
        _roulettePausedTimeScale = false;
        Time.timeScale = _roulettePrevTimeScale;
    }

    private void OnWaveCleared(int waveIndex)
    {
        // 무기가 활성화된 상태에서 "해당 클러스터"를 클리어하면 바로 반납(잠금)
        if (_weaponUnlocked && waveIndex == _weaponUnlockedWaveIndex)
        {
            LockWeaponAfterClusterClear();
        }
    }

    private async UniTaskVoid StartEncounterRouletteAsync(int fixedIndex, int angleRoll, bool markFirstDone)
    {
        _isFirstEncounterRoulettePlaying = true;
        bool isSecondDraw = !markFirstDone;
        bool spinCompleted = false;

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

        RouletteSpinPanel panelUsed = null;

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
            panelUsed = panel;

            if (panel != null)
            {
                panel.Show();
                await panel.WaitForClickAndSpinFixedAsync(fixedIndex, angleRoll);
            }
            else
            {
                // 패널(버튼)이 없으면 안전 폴백(개발 중 데드락 방지)
                Debug.LogWarning("[CatHeroPlayer] RouletteSpinPanel not found. Falling back to auto spin.");
                await roulette.SpinFixedAsync(fixedIndex, angleRoll);
            }

            if (markFirstDone)
            {
                _firstEncounterRouletteDone = true;
            }
            else
            {
                _secondClusterRouletteDone = true;
            }

            // 룰렛 종료 = 공격권/무기 획득
            UnlockWeapon();
            spinCompleted = true;
        }
        finally
        {
            if (panelUsed != null)
            {
                panelUsed.Hide();
            }

            // 룰렛 루트 비활성화로 패널 페이드가 끊기는 것을 방지하기 위해,
            // 여기서는 rouletteGo를 비활성화하지 않습니다.

            // 2번째 뽑기(하프 시네마틱)는 "디밍 복구 후에도 시간 정지 유지"가 필요하므로,
            // 여기서는 spin이 실패했거나 1번째 뽑기일 때만 즉시 복구한다.
            if (!isSecondDraw || !spinCompleted)
            {
                RestoreTimeScaleIfPausedForRoulette();
                _isFirstEncounterRoulettePlaying = false;
            }
        }

        if (isSecondDraw && spinCompleted)
        {
            // 요구사항: 룰렛 결과 이후 디밍 복구는 되지만, 시간은 계속 정지 상태로 유지.
            // 이후의 하프 시네마틱 연출(틴트/홀드/적 일괄 처치)은 별도 시스템(SecondDrawHalfCinematic)이 담당.
            GameEvents.OnSecondDrawHalfCinematicRequested.OnNext(R3.Unit.Default);
        }
    }

    private void OnSecondDrawHalfCinematicFinished()
    {
        // 하프 시네마틱이 끝나면 플레이어 제어 상태를 풀어준다.
        _isFirstEncounterRoulettePlaying = false;
    }

    private void OnSecondDrawHalfCinematicImpact()
    {
        // 임팩트 시점에 timeScale을 재개한다.
        RestoreTimeScaleIfPausedForRoulette();
    }

    private async UniTaskVoid AutoAttackLoopAsync(CancellationToken token)
    {
        while (IsAlive && !token.IsCancellationRequested)
        {
            if (_stat == null) { await UniTask.Yield(token); continue; }
            if (_projectileLauncher == null || _projectileLauncher.ProjectilePrefab == null)
            {
                // 무기 반납(잠금) 상태에서는 공격 애니메이션/발사 모두 스킵
                await UniTask.Yield(token);
                continue;
            }

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
