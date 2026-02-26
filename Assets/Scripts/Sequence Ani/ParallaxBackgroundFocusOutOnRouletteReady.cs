using UnityEngine;
using R3;
using DG.Tweening;

[DisallowMultipleComponent]
public class ParallaxBackgroundFocusOutOnRouletteReady : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private ParallaxBackground targetParallaxBackground;
    [SerializeField] private bool includeInactiveChildren = true;
    [SerializeField] private SpriteRenderer[] excludeSpriteRenderers;

    [Header("Dim Settings - Ready (Ready Dim)")]
    [SerializeField, Range(0f, 1f)] private float readyDimMultiplier = 0.55f;
    [SerializeField, Min(0f)] private float readyDimDuration = 0.18f;
    [SerializeField] private Ease readyDimEase = Ease.OutQuad;

    [Header("Dim Settings - Spinning (Spinning Dim)")]
    [SerializeField, Range(0f, 1f)] private float spinningDimMultiplier = 0.35f;
    [SerializeField, Min(0f)] private float spinningDimDuration = 0.25f;
    [SerializeField] private Ease spinningDimEase = Ease.OutQuad;

    [Header("Dim Settings - Clear (Clear From Ready)")]
    [SerializeField, Min(0f)] private float clearFromReadyDuration = 0.22f;
    [SerializeField, Min(0f)] private float clearFromSpinningDelay = 0.06f;
    [SerializeField, Min(0f)] private float clearFromSpinningDuration = 0.35f;
    [SerializeField] private Ease clearEase = Ease.OutCubic;

    private SpriteRenderer[] _renderers;
    private Color[] _baseColors;

    private bool _isReady;
    private bool _isSpinning;

    private enum DimPhase
    {
        Clear = 0,
        Ready = 1,
        Spinning = 2
    }

    private DimPhase _phase;

    private float _currentMultiplier = 1f;
    private Tween _tween;
    private CompositeDisposable _disposables;

    private void Awake()
    {
        ResolveTarget();
        CacheRenderers();
    }

    private void OnEnable()
    {
        _disposables = new CompositeDisposable();
        GameEvents.OnRouletteSpinReady
            .Subscribe(OnRouletteSpinReadyChanged)
            .AddTo(_disposables);

        GameEvents.OnRouletteSpinning
            .Subscribe(OnRouletteSpinningChanged)
            .AddTo(_disposables);

        GameEvents.OnPlayerWeaponUnlocked
            .Subscribe(_ => ForceClearDim())
            .AddTo(_disposables);
    }

    private void OnDisable()
    {
        _disposables?.Dispose();
        _disposables = null;

        KillTween();
        ApplyMultiplier(1f);
    }

    private void ResolveTarget()
    {
        if (targetParallaxBackground != null) return;
        targetParallaxBackground = GetComponentInParent<ParallaxBackground>();
        if (targetParallaxBackground == null)
        {
            targetParallaxBackground = FindFirstObjectByType<ParallaxBackground>(FindObjectsInactive.Include);
        }
    }

    private void CacheRenderers()
    {
        if (targetParallaxBackground == null)
        {
            _renderers = System.Array.Empty<SpriteRenderer>();
            _baseColors = System.Array.Empty<Color>();
            return;
        }

        var all = targetParallaxBackground.GetComponentsInChildren<SpriteRenderer>(includeInactive: includeInactiveChildren);
        if (all == null || all.Length == 0)
        {
            _renderers = System.Array.Empty<SpriteRenderer>();
            _baseColors = System.Array.Empty<Color>();
            return;
        }

        int count = 0;
        for (int i = 0; i < all.Length; i++)
        {
            var r = all[i];
            if (r == null) continue;
            if (IsExcluded(r)) continue;
            count++;
        }

        _renderers = new SpriteRenderer[count];
        _baseColors = new Color[count];

        int w = 0;
        for (int i = 0; i < all.Length; i++)
        {
            var r = all[i];
            if (r == null) continue;
            if (IsExcluded(r)) continue;

            _renderers[w] = r;
            _baseColors[w] = r.color;
            w++;
        }
    }

    private bool IsExcluded(SpriteRenderer r)
    {
        if (excludeSpriteRenderers == null || excludeSpriteRenderers.Length == 0) return false;
        for (int i = 0; i < excludeSpriteRenderers.Length; i++)
        {
            if (ReferenceEquals(excludeSpriteRenderers[i], r)) return true;
        }
        return false;
    }

    private void OnRouletteSpinReadyChanged(bool ready)
    {
        _isReady = ready;
        TweenToState();
    }

    private void OnRouletteSpinningChanged(bool spinning)
    {
        _isSpinning = spinning;
        TweenToState();
    }

    private void ForceClearDim()
    {
        _isReady = false;
        _isSpinning = false;
        _phase = DimPhase.Clear;
        TweenTo(1f, clearFromReadyDuration, clearEase, 0f);
    }

    private void TweenToState()
    {
        DimPhase next = _isSpinning ? DimPhase.Spinning : (_isReady ? DimPhase.Ready : DimPhase.Clear);
        DimPhase prev = _phase;
        _phase = next;

        if (next == DimPhase.Spinning)
        {
            TweenTo(spinningDimMultiplier, spinningDimDuration, spinningDimEase, 0f);
            return;
        }

        if (next == DimPhase.Ready)
        {
            TweenTo(readyDimMultiplier, readyDimDuration, readyDimEase, 0f);
            return;
        }

        if (prev == DimPhase.Spinning)
        {
            TweenTo(1f, clearFromSpinningDuration, clearEase, clearFromSpinningDelay);
        }
        else
        {
            TweenTo(1f, clearFromReadyDuration, clearEase, 0f);
        }
    }

    private void TweenTo(float targetMultiplier, float duration, Ease ease, float delaySeconds)
    {
        if (_renderers == null || _renderers.Length == 0) return;

        KillTween();

        float start = _currentMultiplier;
        _tween = DOTween.To(() => start, v =>
            {
                start = v;
                _currentMultiplier = v;
                ApplyMultiplier(v);
            }, targetMultiplier, duration)
            .SetEase(ease)
            .SetDelay(Mathf.Max(0f, delaySeconds))
            .SetUpdate(true)
            .SetTarget(this)
            .OnComplete(() =>
            {
                if (_phase == DimPhase.Clear && Mathf.Approximately(_currentMultiplier, 1f))
                {
                    GameEvents.OnBackgroundDimCleared.OnNext(R3.Unit.Default);
                }
            });
    }

    private void ApplyMultiplier(float multiplier)
    {
        if (_renderers == null || _baseColors == null) return;

        float m = Mathf.Clamp01(multiplier);

        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;

            Color baseC = _baseColors[i];
            r.color = new Color(baseC.r * m, baseC.g * m, baseC.b * m, baseC.a);
        }
    }

    private void KillTween()
    {
        if (_tween != null && _tween.active)
        {
            _tween.Kill();
        }
        _tween = null;
    }
}

