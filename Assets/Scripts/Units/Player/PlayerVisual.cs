using UnityEngine;
using R3;
using System;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [Header("Attack VFX")]
    [SerializeField] private ParticleSystem[] attackParticles;
    [SerializeField] private bool restartParticlesOnAttack = true;

    [Header("TimeScale Pause VFX")]
    [SerializeField] private ParticleSystem[] pauseWhenTimeStoppedParticles;
    [SerializeField] private TrailRenderer[] pauseWhenTimeStoppedTrails;
    [SerializeField, Min(0f)] private float timeScaleZeroThreshold = 0.0001f;
    [SerializeField] private bool stopAndClearParticlesOnTimeStop = true;
    [SerializeField] private bool clearTrailsOnResume = true;

    [Header("TimeScale Cinematic Toggle")]
    [SerializeField] private GameObject activeOnTimeStoppedUntilRouletteClick;

    public SpriteRenderer SpriteRenderer => spriteRenderer;

    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int AttackState = Animator.StringToHash("PC Attack");
    private bool _timeStoppedApplied;
    private bool[] _prevParticlePlayingStates;
    private bool[] _prevTrailEnabledStates;
    private bool _rouletteClickConsumedInCurrentTimeStop;
    private IDisposable _rouletteSpinClickDisposable;

    private void Awake()
    {
        spriteRenderer ??= GetComponent<SpriteRenderer>();
        animator ??= GetComponentInParent<Animator>();
        animator ??= GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        SyncTimeScalePauseState(force: true);
        SyncTimeStoppedToggleObject(force: true);

        _rouletteSpinClickDisposable?.Dispose();
        _rouletteSpinClickDisposable = GameEvents.OnRouletteSpinButtonClicked.Subscribe(_ =>
        {
            if (Time.timeScale > timeScaleZeroThreshold) return;
            _rouletteClickConsumedInCurrentTimeStop = true;
            if (activeOnTimeStoppedUntilRouletteClick != null)
            {
                activeOnTimeStoppedUntilRouletteClick.SetActive(false);
            }
        });
    }

    private void OnDisable()
    {
        _rouletteSpinClickDisposable?.Dispose();
        _rouletteSpinClickDisposable = null;

        _rouletteClickConsumedInCurrentTimeStop = false;
        if (activeOnTimeStoppedUntilRouletteClick != null)
        {
            activeOnTimeStoppedUntilRouletteClick.SetActive(false);
        }
    }

    private void Update()
    {
        SyncTimeScalePauseState(force: false);
        SyncTimeStoppedToggleObject(force: false);
    }

    public void PlayAttack()
    {
        if (animator == null) return;
        PlayAttackVfx();
        if (IsInAttackState()) { animator.Play(AttackState, 0, 0f); return; }
        animator.ResetTrigger(AttackTrigger);
        animator.SetTrigger(AttackTrigger);
    }

    private void PlayAttackVfx()
    {
        if (attackParticles == null || attackParticles.Length == 0) return;
        for (int i = 0; i < attackParticles.Length; i++)
        {
            var ps = attackParticles[i];
            if (ps == null) continue;

            if (restartParticlesOnAttack)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
            else
            {
                if (!ps.isPlaying) ps.Play(true);
            }
        }
    }

    private bool IsInAttackState()
    {
        if (animator == null) return false;
        return animator.GetCurrentAnimatorStateInfo(0).shortNameHash == AttackState;
    }

    private void SyncTimeScalePauseState(bool force)
    {
        bool isTimeStoppedNow = Time.timeScale <= timeScaleZeroThreshold;
        if (!force && _timeStoppedApplied == isTimeStoppedNow) return;
        _timeStoppedApplied = isTimeStoppedNow;

        if (isTimeStoppedNow)
        {
            ApplyTimeStoppedVfx();
        }
        else
        {
            RestoreTimeStoppedVfx();
        }
    }

    private void ApplyTimeStoppedVfx()
    {
        if (pauseWhenTimeStoppedParticles != null)
        {
            _prevParticlePlayingStates ??= new bool[pauseWhenTimeStoppedParticles.Length];
            if (_prevParticlePlayingStates.Length != pauseWhenTimeStoppedParticles.Length)
            {
                _prevParticlePlayingStates = new bool[pauseWhenTimeStoppedParticles.Length];
            }

            for (int i = 0; i < pauseWhenTimeStoppedParticles.Length; i++)
            {
                var ps = pauseWhenTimeStoppedParticles[i];
                if (ps == null) continue;
                _prevParticlePlayingStates[i] = ps.isPlaying;

                if (stopAndClearParticlesOnTimeStop)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                else
                {
                    ps.Pause(true);
                }
            }
        }

        if (pauseWhenTimeStoppedTrails != null)
        {
            _prevTrailEnabledStates ??= new bool[pauseWhenTimeStoppedTrails.Length];
            if (_prevTrailEnabledStates.Length != pauseWhenTimeStoppedTrails.Length)
            {
                _prevTrailEnabledStates = new bool[pauseWhenTimeStoppedTrails.Length];
            }

            for (int i = 0; i < pauseWhenTimeStoppedTrails.Length; i++)
            {
                var tr = pauseWhenTimeStoppedTrails[i];
                if (tr == null) continue;
                _prevTrailEnabledStates[i] = tr.enabled;
                tr.enabled = false;
            }
        }
    }

    private void RestoreTimeStoppedVfx()
    {
        if (pauseWhenTimeStoppedParticles != null)
        {
            for (int i = 0; i < pauseWhenTimeStoppedParticles.Length; i++)
            {
                var ps = pauseWhenTimeStoppedParticles[i];
                if (ps == null) continue;
                if (_prevParticlePlayingStates != null && i < _prevParticlePlayingStates.Length && !_prevParticlePlayingStates[i]) continue;
                if (!ps.gameObject.activeInHierarchy) continue;
                ps.Play(true);
            }
        }

        if (pauseWhenTimeStoppedTrails != null)
        {
            for (int i = 0; i < pauseWhenTimeStoppedTrails.Length; i++)
            {
                var tr = pauseWhenTimeStoppedTrails[i];
                if (tr == null) continue;
                bool shouldEnable = _prevTrailEnabledStates == null || i >= _prevTrailEnabledStates.Length || _prevTrailEnabledStates[i];
                tr.enabled = shouldEnable;
                if (shouldEnable && clearTrailsOnResume) tr.Clear();
            }
        }
    }

    private void SyncTimeStoppedToggleObject(bool force)
    {
        if (activeOnTimeStoppedUntilRouletteClick == null) return;

        bool isTimeStoppedNow = Time.timeScale <= timeScaleZeroThreshold;
        if (!isTimeStoppedNow)
        {
            _rouletteClickConsumedInCurrentTimeStop = false;
            if (force || activeOnTimeStoppedUntilRouletteClick.activeSelf)
            {
                activeOnTimeStoppedUntilRouletteClick.SetActive(false);
            }
            return;
        }

        if (_rouletteClickConsumedInCurrentTimeStop)
        {
            if (force || activeOnTimeStoppedUntilRouletteClick.activeSelf)
            {
                activeOnTimeStoppedUntilRouletteClick.SetActive(false);
            }
            return;
        }

        if (force || !activeOnTimeStoppedUntilRouletteClick.activeSelf)
        {
            activeOnTimeStoppedUntilRouletteClick.SetActive(true);
        }
    }
}
