using UnityEngine;
using DG.Tweening;
using R3;

[DisallowMultipleComponent]
public class TopCenterPanelRevealOnCinematicTimeStop : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("TimeScale Condition")]
    [SerializeField, Min(0f)] private float timeScaleZeroThreshold = 0.0001f;

    [Header("Positions (Anchored)")]
    [SerializeField] private Vector2 shownAnchoredPos = new Vector2(0f, 200f);
    [SerializeField] private Vector2 hiddenAnchoredPos = new Vector2(0f, -200f);

    [Header("Slide (Unscaled)")]
    [SerializeField, Min(0f)] private float slideDuration = 0.22f;
    [SerializeField] private Ease slideInEase = Ease.OutCubic;
    [SerializeField] private Ease slideOutEase = Ease.InCubic;

    private Tween _tween;
    private bool _timeStoppedApplied;
    private bool _rouletteClickConsumedInCurrentTimeStop;
    private CompositeDisposable _disposables;

    private void Awake()
    {
        panelRoot ??= transform as RectTransform;
        canvasGroup ??= GetComponent<CanvasGroup>();

        ApplyHiddenImmediate(resetClickConsumed: true);
    }

    private void OnEnable()
    {
        _disposables = new CompositeDisposable();

        GameEvents.OnRouletteSpinButtonClicked
            .Subscribe(_ =>
            {
                if (Time.timeScale > timeScaleZeroThreshold) return;
                _rouletteClickConsumedInCurrentTimeStop = true;
                PlayHide();
            })
            .AddTo(_disposables);

        ApplyHiddenImmediate(resetClickConsumed: false);
        Sync(force: true);
    }

    private void OnDisable()
    {
        _disposables?.Dispose();
        _disposables = null;

        KillTween();
        ApplyHiddenImmediate(resetClickConsumed: true);
    }

    private void Update()
    {
        Sync(force: false);
    }

    private void Sync(bool force)
    {
        bool isTimeStoppedNow = Time.timeScale <= timeScaleZeroThreshold;
        if (!force && _timeStoppedApplied == isTimeStoppedNow) return;
        _timeStoppedApplied = isTimeStoppedNow;

        if (!isTimeStoppedNow)
        {
            _rouletteClickConsumedInCurrentTimeStop = false;
            ApplyHiddenImmediate(resetClickConsumed: false);
            return;
        }

        if (_rouletteClickConsumedInCurrentTimeStop)
        {
            PlayHide();
            return;
        }

        PlayShow();
    }

    private void PlayShow()
    {
        if (panelRoot == null) return;

        KillTween();

        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        panelRoot.anchoredPosition = hiddenAnchoredPos;
        _tween = panelRoot
            .DOAnchorPos(shownAnchoredPos, slideDuration)
            .SetEase(slideInEase)
            .SetUpdate(true)
            .SetTarget(this);
    }

    private void PlayHide()
    {
        if (panelRoot == null) return;

        KillTween();

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        _tween = panelRoot
            .DOAnchorPos(hiddenAnchoredPos, slideDuration)
            .SetEase(slideOutEase)
            .SetUpdate(true)
            .SetTarget(this);
    }

    private void ApplyHiddenImmediate(bool resetClickConsumed)
    {
        if (resetClickConsumed) _rouletteClickConsumedInCurrentTimeStop = false;
        _timeStoppedApplied = false;

        if (panelRoot == null) return;

        panelRoot.anchoredPosition = hiddenAnchoredPos;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void KillTween()
    {
        if (_tween != null && _tween.active) _tween.Kill();
        _tween = null;
    }
}

