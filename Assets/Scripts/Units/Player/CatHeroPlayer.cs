using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;


[SceneReferral]
public class CatHeroPlayer : MonoBehaviour
{
    private enum EncounterRouletteKind
    {
        First = 0,
        Second = 1,
        Third = 2,
    }

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

    [Header("Third Cluster Roulette (Final Item)")]
    [SerializeField] private int thirdClusterRouletteFixedIndex = 2;
    [SerializeField] private int thirdClusterRouletteAngleRoll = 0;

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
    private bool _thirdClusterRouletteDone;

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
        GameEvents.OnThirdDrawFinalCinematicImpact.Subscribe(_ => OnThirdDrawFinalCinematicImpact()).AddTo(_disposables);
        GameEvents.OnThirdDrawFinalCinematicFinished.Subscribe(_ => OnThirdDrawFinalCinematicFinished()).AddTo(_disposables);

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

        var targetInRange = EnemyRegistry.GetFrontMostInRange(transform.position, _stat.AttackRange, onlyAhead: true);
        if (targetInRange != null)
        {
            _isEncounterLocked = true;
            _movement.IsActive = false;

            if (!_firstEncounterRouletteDone && !_isFirstEncounterRoulettePlaying)
            {
                StartEncounterRouletteAsync(EncounterRouletteKind.First, firstEncounterRouletteFixedIndex, firstEncounterRouletteAngleRoll).Forget();
            }
            else if (_firstEncounterRouletteDone
                     && !_secondClusterRouletteDone
                     && !_isFirstEncounterRoulettePlaying
                     && !_weaponUnlocked
                     && _weaponUnlockedWaveIndex > 0
                     && _currentWaveIndex == (_weaponUnlockedWaveIndex + 1))
            {
                StartEncounterRouletteAsync(EncounterRouletteKind.Second, secondClusterRouletteFixedIndex, secondClusterRouletteAngleRoll).Forget();
            }
            else if (_firstEncounterRouletteDone
                     && _secondClusterRouletteDone
                     && !_thirdClusterRouletteDone
                     && !_isFirstEncounterRoulettePlaying
                     && !_weaponUnlocked
                     && _weaponUnlockedWaveIndex > 0
                     && _currentWaveIndex == (_weaponUnlockedWaveIndex + 1))
            {
                StartEncounterRouletteAsync(EncounterRouletteKind.Third, thirdClusterRouletteFixedIndex, thirdClusterRouletteAngleRoll).Forget();
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
        if (_weaponUnlocked && waveIndex == _weaponUnlockedWaveIndex && !_thirdClusterRouletteDone)
        {
            LockWeaponAfterClusterClear();
        }
    }

    private async UniTaskVoid StartEncounterRouletteAsync(EncounterRouletteKind kind, int fixedIndex, int angleRoll)
    {
        _isFirstEncounterRoulettePlaying = true;
        bool isSecondDraw = kind == EncounterRouletteKind.Second;
        bool isThirdDraw = kind == EncounterRouletteKind.Third;
        bool keepTimePausedForCinematic = isSecondDraw || isThirdDraw;
        bool keepPlayerLockedForCinematic = isSecondDraw || isThirdDraw;
        bool spinCompleted = false;

        Roulette roulette = firstEncounterRoulette;
        if (roulette == null)
        {
            roulette = FindFirstObjectByType<Roulette>(FindObjectsInactive.Include);
        }

        if (roulette == null)
        {           
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

            await UniTask.Yield(PlayerLoopTiming.Update);

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
                Debug.LogWarning("[CatHeroPlayer] RouletteSpinPanel not found. Falling back to auto spin.");
                await roulette.SpinFixedAsync(fixedIndex, angleRoll);
            }

            if (kind == EncounterRouletteKind.First)
            {
                _firstEncounterRouletteDone = true;
            }
            else if (kind == EncounterRouletteKind.Second)
            {
                _secondClusterRouletteDone = true;
            }
            else
            {
                _thirdClusterRouletteDone = true;
            }

            UnlockWeapon();
            spinCompleted = true;
        }
        finally
        {
            if (panelUsed != null)
            {
                panelUsed.Hide();
            }

            if (!keepTimePausedForCinematic || !spinCompleted)
            {
                RestoreTimeScaleIfPausedForRoulette();
            }

            if (!keepPlayerLockedForCinematic || !spinCompleted)
            {
                _isFirstEncounterRoulettePlaying = false;
            }
        }

        if (isSecondDraw && spinCompleted)
        {
            GameEvents.OnSecondDrawHalfCinematicRequested.OnNext(R3.Unit.Default);
        }
        else if (isThirdDraw && spinCompleted)
        {
            GameEvents.OnThirdDrawFinalCinematicRequested.OnNext(R3.Unit.Default);
        }
    }

    private void OnSecondDrawHalfCinematicFinished()
    {
        _isFirstEncounterRoulettePlaying = false;
    }

    private void OnSecondDrawHalfCinematicImpact()
    {
        RestoreTimeScaleIfPausedForRoulette();
    }

    private void OnThirdDrawFinalCinematicFinished()
    {   
        _isFirstEncounterRoulettePlaying = false;
    }

    private void OnThirdDrawFinalCinematicImpact()
    {
        RestoreTimeScaleIfPausedForRoulette();
    }

    private async UniTaskVoid AutoAttackLoopAsync(CancellationToken token)
    {
        while (IsAlive && !token.IsCancellationRequested)
        {
            if (_stat == null) { await UniTask.Yield(token); continue; }
            if (_projectileLauncher == null || _projectileLauncher.ProjectilePrefab == null)
            {
                await UniTask.Yield(token);
                continue;
            }

            var target = EnemyRegistry.GetFrontMostInRange(transform.position, _stat.AttackRange, onlyAhead: true);
            if (target == null)
            {
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
